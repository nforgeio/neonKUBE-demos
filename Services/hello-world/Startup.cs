using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Neon.Web;

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
            services.AddHttpClient()
                    .AddSingleton(HelloWorldService.Log)
                    .AddControllers()
                    .AddNeon();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (HelloWorldService.InDevelopment)
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseRouting();
            app.UseHttpMetrics(options =>
            {
                options.AddRouteParameter("page");
            });
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapMetrics();
                endpoints.MapControllers();
            });
        }
    }
}
