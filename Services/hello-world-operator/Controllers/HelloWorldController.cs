using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Neon.Diagnostics;
using Neon.Kube;
using Neon.Kube.Operator.Controller;
using Neon.Kube.Operator.Finalizer;
using Neon.Kube.Operator.ResourceManager;
using Neon.Kube.Resources.Grafana;
using Neon.Kube.Resources.Istio;
using Neon.Kube.Resources.Prometheus;
using Neon.Tasks;

using HelloWorldOperator.Entities;

using k8s;
using k8s.Models;
using Neon.Kube.Operator;
using Neon.Kube.Operator.Attributes;
using Octokit;
using System.Xml.Linq;
using OpenTelemetry.Resources;
using Neon.Kube.Operator.Rbac;
using Neon.Kube.Kube;
using System.Linq;
using Neon.Kube.Resources.Cluster;

namespace HelloWorldOperator.Controllers
{
    [RbacRule<V1HelloWorldDemo>(Verbs = RbacVerb.All, Scope = Neon.Kube.Resources.EntityScope.Cluster)]
    [RbacRule<V1Deployment>(Verbs = RbacVerb.All, Scope = Neon.Kube.Resources.EntityScope.Cluster)]
    [RbacRule<V1PersistentVolumeClaim>(Verbs = RbacVerb.All, Scope = Neon.Kube.Resources.EntityScope.Cluster)]
    [RbacRule<V1ConfigMap>(Verbs = RbacVerb.All, Scope = Neon.Kube.Resources.EntityScope.Cluster)]
    [RbacRule<V1Service>(Verbs = RbacVerb.All, Scope = Neon.Kube.Resources.EntityScope.Cluster)]
    [RbacRule<V1VirtualService>(Verbs = RbacVerb.All, Scope = Neon.Kube.Resources.EntityScope.Cluster)]
    [RbacRule<V1NeonSsoClient>(Verbs = RbacVerb.All, Scope = Neon.Kube.Resources.EntityScope.Cluster)]
    [RbacRule<V1NeonDashboard>(Verbs = RbacVerb.All, Scope = Neon.Kube.Resources.EntityScope.Cluster)]
    [RbacRule<V1AuthorizationPolicy>(Verbs = RbacVerb.All, Scope = Neon.Kube.Resources.EntityScope.Cluster)]
    [RbacRule<V1ServiceMonitor>(Verbs = RbacVerb.All, Scope = Neon.Kube.Resources.EntityScope.Cluster)]
    [RbacRule<V1GrafanaDashboard>(Verbs = RbacVerb.All, Scope = Neon.Kube.Resources.EntityScope.Cluster)]
    [DependentResource<V1Deployment>()]
    [DependentResource<V1Service>()]
    [DependentResource<V1PersistentVolumeClaim>()]
    [DependentResource<V1VirtualService>]
    [DependentResource<V1ServiceMonitor>]
    [DependentResource<V1GrafanaDashboard>()]
    public class HelloWorldController : IResourceController<V1HelloWorldDemo>
    {
        private IKubernetes                         k8s;
        private IFinalizerManager<V1HelloWorldDemo> finalizerManager;
        private ILogger<HelloWorldController>       logger;
        private ClusterInfo                         clusterInfo;
        public HelloWorldController(
            IKubernetes k8s,
            IFinalizerManager<V1HelloWorldDemo> finalizerManager,
            ILogger<HelloWorldController> logger)
        {
            this.k8s              = k8s;
            this.finalizerManager = finalizerManager;
            this.logger           = logger;
        }

        /// <inheritdoc/>
        public static async Task StartAsync(IServiceProvider serviceProvider)
        {
            await SyncContext.Clear;
        }

        public async Task<ResourceControllerResult> ReconcileAsync(V1HelloWorldDemo resource)
        {
            logger.LogInformation($"RECONCILING: {resource.Name()}");

            clusterInfo = TypedConfigMap<ClusterInfo>.From(await k8s.CoreV1.ReadNamespacedConfigMapAsync(KubeConfigMapName.ClusterInfo, KubeNamespace.NeonStatus)).Data;

            if (resource.Spec.StorageType == V1HelloWorldDemo.StorageType.Nfs)
            {
                await UpsertNfsVolumeAsync(resource);
            }

            var tasks = new List<Task>
            {
                UpsertHelloWorldDeploymentAsync(resource),
                UpsertLoadGeneratorDeploymentAsync(resource),
                UpsertServiceAsync(resource),
                UpsertVirtualServiceAsync(resource),
                UpsertHelloWorldServiceMonitorAsync(resource),
                UpsertKubeStateMetricsServiceMonitorAsync(resource),
                UpsertGrafanaDashboardAsync(resource),
                ConfigureSsoAsync(resource),
                ConfigureNeonDashboardAsync(resource)
            };

            await Task.WhenAll(tasks);

            logger.LogInformation($"RECONCILED: {resource.Name()}");

            return ResourceControllerResult.Ok();
        }
        public async Task OnPromotionAsync()
        {
            await SyncContext.Clear;

            logger.LogInformation($"PROMOTED");
        }

        public async Task DeletedAsync(V1HelloWorldDemo resource)
        {
            await SyncContext.Clear;

            logger.LogInformation($"DELETED: {resource.Name()}");
        }

        private async Task ConfigureSsoAsync(V1HelloWorldDemo resource)
        {
            if (resource.Spec.SsoEnabled)
            {
                await ConfigureSsoEnabledAsync(resource);
            }
            else
            {
                await ConfigureSsoDisabledAsync(resource);
            }
        }

        private async Task ConfigureSsoEnabledAsync(V1HelloWorldDemo resource)
        {
            var authPolicyList = await k8s.CustomObjects.ListNamespacedCustomObjectAsync<V1AuthorizationPolicy>(
                 namespaceParameter: resource.Namespace(),
                 fieldSelector:      $"metadata.name={resource.Name()}");

            V1AuthorizationPolicy authPolicy;
            if (authPolicyList.Items.Count > 0)
            {
                logger.LogInformationEx(() => $"AuthorizationPolicy for {resource.Name()}/hello-world exists, updating existing AuthorizationPolicy.");

                authPolicy = authPolicyList.Items[0];
            }
            else
            {
                logger.LogInformationEx(() => $"AuthorizationPolicy for {resource.Name()}/hello-world doesn't exist, creating new AuthorizationPolicy.");

                authPolicy = new V1AuthorizationPolicy().Initialize();
                authPolicy.Metadata.Name = resource.Name();
                authPolicy.Metadata.SetNamespace(resource.Namespace());
                authPolicy.Metadata.EnsureLabels().Add("app", resource.Name());
                authPolicy.Metadata.EnsureLabels().Add("operator", Program.Name);
                authPolicy.AddOwnerReference(resource.MakeOwnerReference());
            }

            authPolicy.Spec = new V1AuthorizationPolicySpec();
            authPolicy.Spec.Selector = new WorkloadSelector()
            {
                MatchLabels = new Dictionary<string, string>() { 
                    { "app", resource.Name() },
                    { "component", "hello-world" }
                }
            };
            authPolicy.Spec.Action = AuthorizationPolicyAction.Custom;
            authPolicy.Spec.Provider = new ExtensionProvider()
            {
                Name = "neon-sso-service"
            };
            authPolicy.Spec.Rules = new List<AuthorizationPolicyRule>() { new AuthorizationPolicyRule() };

            await k8s.CustomObjects.UpsertNamespacedCustomObjectAsync(
                body:               authPolicy,
                name:               authPolicy.Name(),
                namespaceParameter: resource.Namespace());

            var ssoClient = await k8s.CustomObjects.ReadClusterCustomObjectAsync<V1NeonSsoClient>("neon-sso");

            if (!ssoClient.Spec.RedirectUris.Contains($"https://hello-world.{clusterInfo.Domain}/oauth2/callback"))
            {
                ssoClient.Spec.RedirectUris.Add($"https://hello-world.{clusterInfo.Domain}/oauth2/callback");

                await k8s.CustomObjects.UpsertClusterCustomObjectAsync(
                    body: ssoClient,
                    name: ssoClient.Name());
            }
        }

        private async Task ConfigureSsoDisabledAsync(V1HelloWorldDemo resource)
        { 
            var authPolicyList = await k8s.CustomObjects.ListNamespacedCustomObjectAsync<V1AuthorizationPolicy>(
                 namespaceParameter: resource.Namespace(),
                 fieldSelector:      $"metadata.name={resource.Name()}");

            foreach (var authPolicy in authPolicyList)
            {
                await k8s.CustomObjects.DeleteNamespacedCustomObjectAsync<V1AuthorizationPolicy>(
                    name:               authPolicy.Name(),
                    namespaceParameter: resource.Namespace());
            }

            var ssoClient = await k8s.CustomObjects.ReadClusterCustomObjectAsync<V1NeonSsoClient>("neon-sso");

            if (ssoClient.Spec.RedirectUris.Contains($"https://hello-world.{clusterInfo.Domain}/oauth2/callback"))
            {
                ssoClient.Spec.RedirectUris.Remove($"https://hello-world.{clusterInfo.Domain}/oauth2/callback");

                await k8s.CustomObjects.UpsertClusterCustomObjectAsync(
                    body: ssoClient,
                    name: ssoClient.Name());
            }
        }

        private async Task UpsertNfsVolumeAsync(V1HelloWorldDemo resource)
        {
            var pvcList = await k8s.CoreV1.ListNamespacedPersistentVolumeClaimAsync(
                namespaceParameter: resource.Namespace(),
                fieldSelector:      $"metadata.name={resource.Name()}");

            V1PersistentVolumeClaim pvc;

            if (pvcList.Items.Count > 0)
            {
                logger.LogInformationEx(() => $"PersistentVolumeClaim for {resource.Name()}/hello-world exists.");
            }
            else
            {
                logger.LogInformationEx(() => $"PersistentVolumeClaim for {resource.Name()}/hello-world doesn't exist, creating new PersistentVolumeClaim.");
                pvc = new V1PersistentVolumeClaim().Initialize();
                pvc.Metadata.Name = resource.Name();
                pvc.Metadata.SetNamespace(resource.Namespace());
                pvc.Metadata.EnsureLabels().Add("app", resource.Name());
                pvc.Metadata.EnsureLabels().Add("operator", Program.Name);
                pvc.AddOwnerReference(resource.MakeOwnerReference());

                var resourceReq = new Dictionary<string, ResourceQuantity>();
                resourceReq.Add("storage", new ResourceQuantity("10Mi"));

                pvc.Spec = new V1PersistentVolumeClaimSpec()
                {
                    AccessModes      = new List<string>() { "ReadWriteMany" },
                    Resources        = new V1ResourceRequirements(requests: resourceReq),
                    StorageClassName = "openebs-nfs"
                };

                await k8s.CoreV1.CreateNamespacedPersistentVolumeClaimAsync(pvc, pvc.Namespace());
            }
        }

        private async Task UpsertHelloWorldDeploymentAsync(V1HelloWorldDemo resource)
        {
            var deploymentList = await k8s.AppsV1.ListNamespacedDeploymentAsync(
                namespaceParameter: resource.Namespace(),
                fieldSelector: $"metadata.name={resource.Name()}");

            V1Deployment deployment;
            bool exists = false;
            if (deploymentList.Items.Count > 0)
            {
                logger.LogInformationEx(() => $"Deployment for {resource.Name()}/hello-world exists, updating existing Deployment.");
                exists = true;
                deployment = deploymentList.Items[0];
            }
            else
            {
                logger.LogInformationEx(() => $"Deployment for {resource.Name()}/hello-world doesn't exist, creating new Deployment.");

                deployment = new V1Deployment().Initialize();
                deployment.Metadata.Name = resource.Name();
                deployment.Metadata.SetNamespace(resource.Namespace());
                deployment.Metadata.EnsureLabels().Add("app", resource.Name());
                deployment.Metadata.EnsureLabels().Add("operator", Program.Name);
                deployment.Metadata.EnsureLabels().Add("component", "hello-world");
                deployment.AddOwnerReference(resource.MakeOwnerReference());
            }

            deployment.Spec = new V1DeploymentSpec()
            {
                Replicas = resource.Spec.HelloWorldReplicas,
                Selector = new V1LabelSelector()
                {
                    MatchLabels = new Dictionary<string, string>()
                    {
                        { "app", resource.Name() }
                    }
                },
                Template = new V1PodTemplateSpec()
                {
                    Metadata = new V1ObjectMeta()
                    {
                        Labels = new Dictionary<string, string>()
                        {
                            { "app", resource.Name() },
                            { "component", "hello-world" }
                        }

                    },
                    Spec = new V1PodSpec()
                    {
                        Containers = new List<V1Container>()
                            {
                                new V1Container()
                                {
                                    Name = "hello-world",
                                    Image = "registry.neon.local/library/hello-world:latest",
                                    ImagePullPolicy = "Always",
                                    Ports = new List<V1ContainerPort>()
                                    {
                                        new V1ContainerPort(containerPort: 80, name: "http-web", protocol: "TCP"),
                                        new V1ContainerPort(containerPort: 9762, name: "http-metrics", protocol: "TCP")
                                    },
                                    Env = new List<V1EnvVar>() { new V1EnvVar(name: "LOG_LEVEL", value: resource.Spec.LogLevel)},
                                    LivenessProbe = new V1Probe()
                                    {
                                        Exec = new V1ExecAction()
                                        {
                                            Command = new List<string>(){ "/health-check" }
                                        },
                                        InitialDelaySeconds = 15,
                                        TimeoutSeconds = 1,
                                        PeriodSeconds = 5,
                                        SuccessThreshold = 1,
                                        FailureThreshold = 1,
                                    },
                                    ReadinessProbe = new V1Probe()
                                    {
                                        Exec = new V1ExecAction()
                                        {
                                            Command = new List<string>(){ "/ready-check" }
                                        },
                                        InitialDelaySeconds = 15,
                                        TimeoutSeconds = 1,
                                        PeriodSeconds = 5,
                                        SuccessThreshold = 1,
                                        FailureThreshold = 1,
                                    },
                                    StartupProbe = new V1Probe()
                                    {
                                        Exec = new V1ExecAction()
                                        {
                                            Command = new List<string>(){ "/health-check" }
                                        },
                                        InitialDelaySeconds = 15,
                                        TimeoutSeconds = 1,
                                        PeriodSeconds = 5,
                                        SuccessThreshold = 1,
                                        FailureThreshold = 30,
                                    }
                                }
                            }
                    }
                }
            };

            switch (resource.Spec.StorageType)
            {
                case V1HelloWorldDemo.StorageType.Nfs:

                    deployment.Spec.Template.Spec.Volumes = new List<V1Volume>()
                    {
                        new V1Volume()
                        {
                            Name = "data",
                            PersistentVolumeClaim = new V1PersistentVolumeClaimVolumeSource(claimName: resource.Name())
                        }
                    };

                    deployment.Spec.Template.Spec.Containers.First().VolumeMounts = new List<V1VolumeMount>()
                    {
                        new V1VolumeMount()
                        {
                            Name = "data",
                            MountPath = "/var/helloworld"
                        }
                    };

                    break;

                case V1HelloWorldDemo.StorageType.Ephemeral:
                default:
                    break;
            }

            if (exists)
            {
                await k8s.AppsV1.ReplaceNamespacedDeploymentAsync(deployment, deployment.Name(), deployment.Namespace());
            }
            else
            {
                await k8s.AppsV1.CreateNamespacedDeploymentAsync(deployment, deployment.Namespace());
            }
        }

        private async Task UpsertLoadGeneratorDeploymentAsync(V1HelloWorldDemo resource)
        {
            var name = $"{resource.Name()}-load-generator";
            var deploymentList = await k8s.AppsV1.ListNamespacedDeploymentAsync(
                namespaceParameter: resource.Namespace(),
                fieldSelector: $"metadata.name={name}");

            V1Deployment deployment;
            bool exists = false;
            if (deploymentList.Items.Count > 0)
            {
                logger.LogInformationEx(() => $"Deployment for {resource.Name()}/load-generator exists, updating existing Deployment.");

                exists = true;
                deployment = deploymentList.Items[0];
            }
            else
            {
                logger.LogInformationEx(() => $"Deployment for {resource.Name()}/load-generator doesn't exist, creating new Deployment.");

                deployment = new V1Deployment().Initialize();
                deployment.Metadata.Name = name;
                deployment.Metadata.SetNamespace(resource.Namespace());
                deployment.Metadata.EnsureLabels().Add("app", resource.Name());
                deployment.Metadata.EnsureLabels().Add("operator", Program.Name);
                deployment.AddOwnerReference(resource.MakeOwnerReference());
            }

            deployment.Spec = new V1DeploymentSpec()
            {
                Replicas = resource.Spec.LoadGeneratorReplicas,
                Selector = new V1LabelSelector()
                {
                    MatchLabels = new Dictionary<string, string>()
                    {
                        { "app", name }
                    }
                },
                Template = new V1PodTemplateSpec()
                {
                    Metadata = new V1ObjectMeta()
                    {
                        Labels = new Dictionary<string, string>()
                            {
                                { "app", name }
                            }
                    },
                    Spec = new V1PodSpec()
                    {
                        Containers = new List<V1Container>()
                            {
                                new V1Container()
                                {
                                    Name = "load-generator",
                                    Image = "registry.neon.local/library/load-generator:latest",
                                    ImagePullPolicy = "Always",
                                }
                            }
                    }
                }
            };

            if (exists)
            {
                await k8s.AppsV1.ReplaceNamespacedDeploymentAsync(deployment, deployment.Name(), deployment.Namespace());
            }
            else
            {
                await k8s.AppsV1.CreateNamespacedDeploymentAsync(deployment, deployment.Namespace());
            }
        }

        private async Task UpsertServiceAsync(V1HelloWorldDemo resource)
        {
            var serviceList = await k8s.CoreV1.ListNamespacedServiceAsync(
                namespaceParameter: resource.Namespace(),
                fieldSelector: $"metadata.name={resource.Name()}");

            V1Service service;
            bool exists = false;
            if (serviceList.Items.Count > 0)
            {
                logger.LogInformationEx(() => $"Service for {resource.Name()}/hello-world exists, updating existing Service.");

                exists = true;
                service = serviceList.Items[0];
            }
            else
            {
                logger.LogInformationEx(() => $"Service for {resource.Name()}/hello-world doesn't exist, creating new Service.");

                service = new V1Service().Initialize();
                service.Metadata.Name = resource.Name();
                service.Metadata.SetNamespace(resource.Namespace());
                service.Metadata.EnsureLabels().Add("app", resource.Name());
                service.Metadata.EnsureLabels().Add("operator", Program.Name);
                service.AddOwnerReference(resource.MakeOwnerReference());
            }

            service.Spec = new V1ServiceSpec()
            {
                Selector = new Dictionary<string, string>()
                {
                    { "app", resource.Name() }
                },
                Ports = new List<V1ServicePort>()
                {
                    new V1ServicePort(port: 80, targetPort: 80, name: "http-web", protocol: "TCP"),
                    new V1ServicePort(port: 9762, targetPort: 9762, name: "http-metrics", protocol: "TCP")
                }
            };

            if (exists)
            {
                await k8s.CoreV1.ReplaceNamespacedServiceAsync(service, service.Name(), service.Namespace());
            }
            else
            {
                await k8s.CoreV1.CreateNamespacedServiceAsync(service, service.Namespace());
            }
        }

        private async Task UpsertVirtualServiceAsync(V1HelloWorldDemo resource)
        {
            var virtualServiceList = await k8s.CustomObjects.ListNamespacedCustomObjectAsync<V1VirtualService>(
                namespaceParameter: resource.Namespace(),
                fieldSelector: $"metadata.name={resource.Name()}");

            V1VirtualService virtualService;
            if (virtualServiceList.Items.Count > 0)
            {
                logger.LogInformationEx(() => $"VirtualService for {resource.Name()}/hello-world exists, updating existing VirtualService.");

                virtualService = virtualServiceList.Items[0];
            }
            else
            {
                logger.LogInformationEx(() => $"VirtualService for {resource.Name()}/hello-world doesn't exist, creating new VirtualService.");

                virtualService = new V1VirtualService().Initialize();
                virtualService.Metadata.Name = resource.Name();
                virtualService.Metadata.SetNamespace(resource.Namespace());
                virtualService.Metadata.EnsureLabels().Add("app", resource.Name());
                virtualService.Metadata.EnsureLabels().Add("operator", Program.Name);
                virtualService.AddOwnerReference(resource.MakeOwnerReference());
            }

            virtualService.Spec = new V1VirtualServiceSpec()
            {
                Gateways = new List<string>() { "neon-ingress/neoncluster-gateway" },
                Hosts    = new List<string>() { $"hello-world.{clusterInfo.Domain}" },
                Http     = new List<HTTPRoute>()

            };

            if (resource.Spec.SsoEnabled)
            {
                virtualService.Spec.Http.Add(new HTTPRoute()
                {
                    Match = new List<HTTPMatchRequest>()
                    {
                        new HTTPMatchRequest()
                        {
                            Uri = new StringMatch()
                            {
                                Prefix = "/oauth2"
                            }
                        }
                    },
                    Route = new List<HTTPRouteDestination>()
                    {
                        new HTTPRouteDestination()
                        {
                            Destination = new Destination()
                            {
                                Host = "neon-sso-oauth2-proxy.neon-system.svc.cluster.local",
                                Port = new PortSelector()
                                {
                                    Number = 4180
                                }
                            }
                        }
                    }
                });
            }

            virtualService.Spec.Http.Add(
                new HTTPRoute()
                {
                    Match = new List<HTTPMatchRequest>()
                    {
                        new HTTPMatchRequest()
                        {
                            Uri = new StringMatch()
                            {
                                Prefix = "/"
                            }
                        }
                    },
                    Route = new List<HTTPRouteDestination>()
                    {
                        new HTTPRouteDestination()
                        {
                            Destination = new Destination()
                            {
                                Host = $"{resource.Name()}.{resource.Namespace()}.svc.cluster.local",
                                Port = new PortSelector()
                                {
                                    Number = 80
                                }
                            }
                        }
                    }
                });

            await k8s.CustomObjects.UpsertNamespacedCustomObjectAsync(
                body:               virtualService, 
                name:               virtualService.Name(),
                namespaceParameter: resource.Namespace());
        }

        private async Task UpsertHelloWorldServiceMonitorAsync(V1HelloWorldDemo resource)
        {
            var serviceMonitorList = await k8s.CustomObjects.ListNamespacedCustomObjectAsync<V1ServiceMonitor>(
                namespaceParameter: resource.Namespace(),
                fieldSelector: $"metadata.name={resource.Name()}");

            V1ServiceMonitor serviceMonitor;
            if (serviceMonitorList.Items.Count > 0)
            {
                logger.LogInformationEx(() => $"ServiceMonitor for {resource.Name()}/hello-world exists, updating existing ServiceMonitor.");

                serviceMonitor = serviceMonitorList.Items[0];
            }
            else
            {
                logger.LogInformationEx(() => $"ServiceMonitor for {resource.Name()}/hello-world doesn't exist, creating new ServiceMonitor.");

                serviceMonitor = new V1ServiceMonitor().Initialize();
                serviceMonitor.Metadata.Name = resource.Name();
                serviceMonitor.Metadata.SetNamespace(resource.Namespace());
                serviceMonitor.AddOwnerReference(resource.MakeOwnerReference());
            }

            serviceMonitor.Spec = new V1ServiceMonitorSpec()
            {
                Endpoints = new List<Endpoint>
                {
                    new Endpoint()
                    {
                        Interval = "5s",
                        Path = "/metrics",
                        ScrapeTimeout = "2s",
                        TargetPort = 9762
                    }
                },
                JobLabel = resource.Name(),
                Selector = new V1LabelSelector()
                {
                    MatchLabels = new Dictionary<string, string>
                    {
                        { "app", resource.Name() }
                    }
                }
            };

            await k8s.CustomObjects.UpsertNamespacedCustomObjectAsync(
                body:               serviceMonitor, 
                name:               serviceMonitor.Name(),
                namespaceParameter: resource.Namespace());
        }

        private async Task UpsertKubeStateMetricsServiceMonitorAsync(V1HelloWorldDemo resource)
        {
            var name = $"kube-state-metrics";
            var serviceMonitorList = await k8s.CustomObjects.ListNamespacedCustomObjectAsync<V1ServiceMonitor>(
                namespaceParameter: KubeNamespace.NeonMonitor,
                fieldSelector: $"metadata.name={name}");

            V1ServiceMonitor serviceMonitor;
            if (serviceMonitorList.Items.Count > 0)
            {
                logger.LogInformationEx(() => $"ServiceMonitor for {resource.Name()}/kube-state-metrics exists, updating existing ServiceMonitor.");

                serviceMonitor = serviceMonitorList.Items[0];
            }
            else
            {
                logger.LogInformationEx(() => $"ServiceMonitor for {resource.Name()}/kube-state-metrics doesn't exist, creating new ServiceMonitor.");

                serviceMonitor = new V1ServiceMonitor().Initialize();
                serviceMonitor.Metadata.Name = name;
                serviceMonitor.Metadata.SetNamespace(KubeNamespace.NeonMonitor);
                serviceMonitor.Metadata.EnsureLabels().Add("app", resource.Name());
                serviceMonitor.Metadata.EnsureLabels().Add("operator", Program.Name);
            }

            serviceMonitor.Spec = new V1ServiceMonitorSpec()
            {
                Endpoints = new List<Endpoint>
                {
                    new Endpoint()
                    {
                        Interval = "5s",
                        HonorLabels = true,
                        TargetPort = 8080
                    }
                },
                Selector = new V1LabelSelector()
                {
                    MatchLabels = new Dictionary<string, string>
                    {
                        { "app.kubernetes.io/instance", "kube-state-metrics" },
                        { "app.kubernetes.io/name", "kube-state-metrics" }
                    }
                }
            };

            await k8s.CustomObjects.UpsertNamespacedCustomObjectAsync(
                body:               serviceMonitor,
                name:               serviceMonitor.Name(),
                namespaceParameter: KubeNamespace.NeonMonitor);
        }

        private async Task UpsertGrafanaDashboardAsync(V1HelloWorldDemo resource)
        {
            var grafanaDashboardList = await k8s.CustomObjects.ListNamespacedCustomObjectAsync<V1GrafanaDashboard>(
                namespaceParameter: resource.Namespace(),
                fieldSelector: $"metadata.name={resource.Name()}");

            V1GrafanaDashboard dashboard;
            if (grafanaDashboardList.Items.Count > 0)
            {
                logger.LogInformationEx(() => $"GrafanaDashboard for {resource.Name()} exists, updating existing GrafanaDashboard.");

                dashboard = grafanaDashboardList.Items[0];
            }
            else
            {
                logger.LogInformationEx(() => $"GrafanaDashboard for {resource.Name()} doesn't exist, creating new GrafanaDashboard.");

                dashboard = new V1GrafanaDashboard().Initialize();
                dashboard.Metadata.Name = resource.Name();
                dashboard.Metadata.SetNamespace(resource.Namespace());
                dashboard.AddOwnerReference(resource.MakeOwnerReference());
            }


            dashboard.Metadata.EnsureLabels()["app"] = "grafana";
            dashboard.Metadata.EnsureLabels()["operator"] = Program.Name;

            dashboard.Spec = new V1GrafanaDashboardSpec()
            {
                Datasources = new List<V1GrafanaDatasource>()
                {
                    new V1GrafanaDatasource()
                    {
                        DatasourceName = "Mimir",
                        InputName = "DS_MIMIR"
                    },
                    new V1GrafanaDatasource()
                    {
                        DatasourceName = "Loki",
                        InputName = "DS_LOKI"
                    }
                },
                Json = Program.Resources.GetFile("/dashboard.json").ReadAllText()
            };

            await k8s.CustomObjects.UpsertNamespacedCustomObjectAsync(
                body: dashboard,
                name: dashboard.Name(),
                namespaceParameter: resource.Namespace());
        }

        private async Task ConfigureNeonDashboardAsync(V1HelloWorldDemo resource)
        {
            if (resource.Spec.NeonDashboardEnabled)
            {
                var neonDashboardList = await k8s.CustomObjects.ListClusterCustomObjectAsync<V1NeonDashboard>(
                    fieldSelector: $"metadata.name={resource.Name()}");

                V1NeonDashboard dashboard;
                if (neonDashboardList.Items.Count > 0)
                {
                    logger.LogInformationEx(() => $"NeonDashboard for {resource.Name()} exists, updating existing NeonDashboard.");

                    dashboard = neonDashboardList.Items[0];
                }
                else
                {
                    logger.LogInformationEx(() => $"NeonDashboard for {resource.Name()} doesn't exist, creating new NeonDashboard.");

                    dashboard = new V1NeonDashboard().Initialize();
                    dashboard.Metadata.Name = resource.Name();
                    dashboard.Metadata.SetNamespace(resource.Namespace());
                    dashboard.AddOwnerReference(resource.MakeOwnerReference());
                }

                dashboard.Metadata.EnsureLabels()["app"] = "neon";
                dashboard.Metadata.EnsureLabels()["operator"] = Program.Name;

                dashboard.Spec = new V1NeonDashboard.NeonDashboardSpec
                {
                    DisplayName = resource.Name(),
                    Enabled     = true,
                    Url         = $"https://hello-world.{clusterInfo.Domain}"
                };

                await k8s.CustomObjects.UpsertClusterCustomObjectAsync(
                    body: dashboard,
                    name: dashboard.Name());
            }
            else
            {
                try
                {
                    await k8s.CustomObjects.DeleteClusterCustomObjectAsync<V1NeonDashboard>(resource.Name());
                }
                catch
                {
                    // doesn't exist
                }
            }
        }
    }
}
