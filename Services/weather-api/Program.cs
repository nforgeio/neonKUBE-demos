//-----------------------------------------------------------------------------
// FILE:	    Program.cs
// CONTRIBUTOR: Marcus Bowyer
// COPYRIGHT:   Copyright (c) 2005-2022 by neonFORGE LLC.  All rights reserved.

using System;
using System.Threading.Tasks;

using Neon.Common;
using Neon.Service;

using Prometheus.DotNetRuntime;

namespace WeatherApi
{
    /// <summary>
    /// Holds the global program state.
    /// </summary>
    public static partial class Program
    {
        /// <summary>
        /// Returns the program's service implementation.
        /// </summary>
        public static Service Service { get; private set; }

        /// <summary>
        /// The program entrypoint.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        public static async Task Main(string[] args)
        {
            try
            {
                Service = new Service("weather-api");

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
