using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Neon.Common;
using Neon.Service;

namespace HelloWorld
{
    public class Service : NeonService
    {
        private IWebHost webHost;

        public Service(string name)
             : base(name, version: "0.0.1", metricsPrefix: "helloworld")
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
                port = 0;
            }

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

            Log.LogInfo($"Listening on {IPAddress.Any}:{port}");

            // Indicate that the service is ready for business.

            await SetStatusAsync(NeonServiceStatus.Running);

            // Wait for the process terminator to signal that the service is stopping.

            await Terminator.StopEvent.WaitAsync();

            // Return the exit code specified by the configuration.

            return await Task.FromResult(0);
        }
    }
}