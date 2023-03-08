using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Neon.Common;
using Neon.Service;
using Neon.Kube;
using OpenTelemetry.Trace;
using OpenTelemetry;
using System.Diagnostics;
using HelloWorld.Controllers;
using Neon.Kube.PortForward;
using Neon.Diagnostics;
using k8s;
using k8s.Models;
using System.Linq;
using Neon.Net;

namespace HelloWorld
{
    public class Service : NeonService
    {
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

            int port = 80;

            if (NeonHelper.IsDevWorkstation)
            {
                port = 11010;
            }

            EnsureTextFile();

            // Pause for a random amount of time between 5-10 seconds to give
            // the demononstrator a chance to go back to the [hello-world]
            // dashboard so folks can see replicas come online and the random
            // part should make this more interesting (i.e. the pods don't
            // at start at once.

            var randomDelay = NeonHelper.PseudoRandomTimespan(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10));

            await Task.Delay(randomDelay);

            // Start the web service.

            webHost = new WebHostBuilder()
                .ConfigureAppConfiguration(
                    (hostingcontext, config) =>
                    {
                        config.Sources.Clear();
                    })
                .UseStartup<Startup>()
                .UseKestrel(options => options.Listen(IPAddress.Any, port))
                .ConfigureServices(services => services.AddSingleton(typeof(Service), this))
                .UseStaticWebAssets()
                .Build();

            _ = webHost.RunAsync();

            Logger.LogInformation($"Listening on {IPAddress.Any}:{port}");

            // Indicate that the service is ready for business.

            await SetStatusAsync(NeonServiceStatus.Running);

            // Wait for the process terminator to signal that the service is stopping.

            await Terminator.StopEvent.WaitAsync();

            // Return the exit code specified by the configuration.

            return await Task.FromResult(0);
        }

        private void EnsureTextFile()
        {
            if (!Directory.Exists(HelloController.DataDirPath))
            {
                Directory.CreateDirectory(HelloController.DataDirPath);
            }

            if (!File.Exists(HelloController.TextPath))
            {
                File.Create(HelloController.TextPath).Close();
            }
        } 

        /// <inheritdoc/>
        protected override bool OnTracerConfig(TracerProviderBuilder builder)
        {
            builder.AddHttpClientInstrumentation(x =>
            {
                x.RecordException = true;
            })
                    .AddAspNetCoreInstrumentation(o =>
                    {
                        o.EnrichWithHttpRequest = (activity, httpRequest) =>
                        {
                            activity.SetTag("requestProtocol", httpRequest.Protocol);
                        };
                        o.EnrichWithHttpResponse = (activity, httpResponse) =>
                        {
                            activity.SetTag("responseLength", httpResponse.ContentLength);
                        };
                        o.EnrichWithException = (activity, exception) =>
                        {
                            activity.SetTag("exceptionType", exception.GetType().ToString());
                        };
                    })
                    .AddOtlpExporter(
                    options =>
                    {
                        options.ExportProcessorType = ExportProcessorType.Batch;
                        options.BatchExportProcessorOptions = new BatchExportProcessorOptions<Activity>();
                        options.Endpoint = new Uri(NeonHelper.NeonKubeOtelCollectorUri);
                        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                    });

            return true;
        }
    }
}