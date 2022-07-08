using System;
using System.Threading.Tasks;

using Neon.Common;
using Neon.Service;

using Prometheus.DotNetRuntime;

namespace HelloWorld
{
    public static partial class Program
    {
        public static Service Service { get; private set; }

        public static async Task Main(string[] args)
        {
Console.WriteLine($"MAIN: 0");
            try
            {
                Service = new Service("hello-world");
Console.WriteLine($"MAIN: 1");

                Service.MetricsOptions.Mode = MetricsMode.Scrape;
                Service.MetricsOptions.Path = "metrics/";
                Service.MetricsOptions.Port = 9762;
                Service.MetricsOptions.GetCollector =
                    () =>
                    {
                        return DotNetRuntimeStatsBuilder
                            .Default()
                            .StartCollecting();
                    };

Console.WriteLine($"MAIN: 2");
                Environment.Exit(await Service.RunAsync());
            }
            catch (Exception e)
            {
                // We really shouldn't see exceptions here but let's log something
                // just in case.  Note that logging may not be initialized yet so
                // we'll just output a string.

                Console.Error.WriteLine(NeonHelper.ExceptionError(e));
                Environment.Exit(-1);
            }
        }
    }
}
