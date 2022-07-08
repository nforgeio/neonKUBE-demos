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
            try
            {
                Service = new Service("hello-world");

                Service.MetricsOptions.Mode         = MetricsMode.Scrape;
                Service.MetricsOptions.GetCollector =
                    () =>
                    {
                        return DotNetRuntimeStatsBuilder
                            .Default()
                            .StartCollecting();
                    };

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
