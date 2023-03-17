using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Neon.Diagnostics;

using Prometheus;

namespace HelloWorld
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Service HelloWorldService;

        public Startup(IConfiguration configuration, Service service)
        {
            Configuration          = configuration;
            this.HelloWorldService = service;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddHttpClient()
                .AddSingleton(TelemetryHub.LoggerFactory)
                .AddResponseCompression()
                .AddRazorPages().Services
                .AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (HelloWorldService.InDevelopment)
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseResponseCompression();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseHttpMetrics();
            app.UseMetricServer(options =>
            {
                options.EnableOpenMetrics = true;
            });
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
                endpoints.MapMetrics();
            });
        }
    }
}
