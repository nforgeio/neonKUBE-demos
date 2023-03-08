using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Neon.Common;
using Neon.Service;
using k8s;
using k8s.Models;
using Neon.Kube;
using Neon.Tasks;
using Neon.Diagnostics;
using Neon.Kube.Resources.CertManager;
using Neon.Kube.Kube;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace HelloWorldOperator
{
    public class Service : NeonService
    {
        public IKubernetes K8s;

        public ClusterInfo ClusterInfo;

        public static string OperatorName = "hello-world-operator";

        private static readonly ILogger logger = TelemetryHub.CreateLogger<Service>();
        private readonly JsonSerializerOptions serializeOptions;

        private IWebHost webHost;

        public Service(string name)
             : base(name, version: "0.0.1")
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            // Dispose web host if it's still running.

            if (webHost != null)
            {
                webHost.Dispose();
                webHost = null;
            }
        }

        protected async override Task<int> OnRunAsync()
        {
            await SetStatusAsync(NeonServiceStatus.Starting);

            await WatchClusterInfoAsync();

            webHost = new WebHostBuilder()
                .ConfigureAppConfiguration(
                    (hostingcontext, config) =>
                    {
                        config.Sources.Clear();
                    })
                .UseStartup<OperatorStartup>()
                .UseKestrel()
                .ConfigureServices(services => services.AddSingleton(typeof(Service), this))
                .UseStaticWebAssets()
                .Build();

            _ = webHost.RunAsync();


            await SetStatusAsync(NeonServiceStatus.Running);

            // Wait for the process terminator to signal that the service is stopping.

            await Terminator.StopEvent.WaitAsync();

            // Return the exit code specified by the configuration.

            return await Task.FromResult(0);
        }

        private async Task WatchClusterInfoAsync()
        {
            await SyncContext.Clear;

            _ = K8s.WatchAsync<V1ConfigMap>(async (@event) =>
            {
                await SyncContext.Clear;

                ClusterInfo = TypedConfigMap<ClusterInfo>.From(@event.Value).Data;

                logger.LogInformationEx("Updated cluster info");
            },
            KubeNamespace.NeonStatus,
            fieldSelector: $"metadata.name={KubeConfigMapName.ClusterInfo}");
        }
    }
}