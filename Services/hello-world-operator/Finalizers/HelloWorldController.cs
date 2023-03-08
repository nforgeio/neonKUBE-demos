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
using System.Linq;
using Neon.Kube.Resources.Cluster;
using Neon.Kube.Operator.Util;
using Neon.Kube.Resources.Minio;
using Neon.Kube.Kube;

namespace HelloWorldOperator.Finalizers
{
    public class HelloWorldFinalizer : IResourceFinalizer<V1HelloWorldDemo>
    {
        private IKubernetes                  k8s;
        private ILogger<HelloWorldFinalizer> logger;
        public HelloWorldFinalizer(
            IKubernetes k8s,
            ILogger<HelloWorldFinalizer> logger)
        {
            this.k8s              = k8s;
            this.logger           = logger;
        }

        public async Task FinalizeAsync(V1HelloWorldDemo resource)
        {
            logger.LogInformation($"FINALIZING: {resource.Name()}");

            var clusterInfo = TypedConfigMap<ClusterInfo>.From(await k8s.CoreV1.ReadNamespacedConfigMapAsync(KubeConfigMapName.ClusterInfo, KubeNamespace.NeonStatus)).Data;

            var serviceMonitor = await k8s.CustomObjects.ReadNamespacedCustomObjectAsync<V1ServiceMonitor>(
                namespaceParameter: KubeNamespace.NeonMonitor,
                name:               "kube-state-metrics");

            serviceMonitor.Spec.Endpoints.First().Interval = "1m";

            await k8s.CustomObjects.UpsertNamespacedCustomObjectAsync(
                body:               serviceMonitor, 
                name:               serviceMonitor.Name(),
                namespaceParameter: serviceMonitor.Namespace());

            var ssoClient = await k8s.CustomObjects.ReadClusterCustomObjectAsync<V1NeonSsoClient>("neon-sso");

            if (ssoClient.Spec.RedirectUris.Contains($"https://hello-world.{clusterInfo.Domain}/oauth2/callback"))
            {
                ssoClient.Spec.RedirectUris.Remove($"https://hello-world.{clusterInfo.Domain}/oauth2/callback");

                await k8s.CustomObjects.UpsertClusterCustomObjectAsync(
                    body: ssoClient,
                    name: ssoClient.Name());
            }

            try
            {
                await k8s.CustomObjects.DeleteClusterCustomObjectAsync<V1NeonDashboard>(resource.Name());
            }
            catch
            {
                // doesn't exist
            }

            logger.LogInformation($"FINALIZED: {resource.Name()}");
        }
    }
}
