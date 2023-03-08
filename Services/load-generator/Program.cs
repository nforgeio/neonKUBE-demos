using Neon.Common;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace LoadGenerator
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = Sdk.CreateTracerProviderBuilder()
                  .SetResourceBuilder(ResourceBuilder.CreateDefault()
                  .AddService("load-generator", serviceVersion: "1.0.0"))
                  .AddHttpClientInstrumentation()
                  .AddOtlpExporter(
                      options =>
                      {
                          options.ExportProcessorType = ExportProcessorType.Batch;
                          options.BatchExportProcessorOptions = new BatchExportProcessorOptions<Activity>();
                          options.Endpoint = new Uri(NeonHelper.NeonKubeOtelCollectorUri);
                          options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                      })
                  .Build();

            var client = new HttpClient();

            for (int i = 0; i < 10; i++)
            {
                _ = LoadGen(client);
            }

            await Task.Delay(TimeSpan.FromDays(1));
        }

        private static async Task LoadGen(HttpClient client)
        {
            while (true)
            {
                try
                {
                    await client.GetAsync("http://hello-world");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"ERROR: {e.GetType().FullName}: {e.Message}");
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
}
