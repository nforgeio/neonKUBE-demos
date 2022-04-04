//-----------------------------------------------------------------------------
// FILE:	    Generator.cs
// CONTRIBUTOR: Marcus Bowyer
// COPYRIGHT:   Copyright (c) 2005-2022 by neonFORGE LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Neon.Common;
using Neon.Net;
using Neon.Service;
using Neon.Time;

namespace LoadGenerator
{
    public partial class Service : NeonService
    {
        private int requestsPerSecond = 10;
        private JsonClient helloWorldClient;
        private List<string> endpoints;

        /// <summary>
        /// Main method to generate load to specified URLs
        /// </summary>
        /// <returns></returns>
        public async Task RequestAsync()
        {
            var captureTimer = new RecurringTimer("Interval:00:00:01");
            captureTimer.Set();

            var configReloadTimer = new RecurringTimer("Interval:00:01:00");
            configReloadTimer.Set();

            while (true)
            {
                if (configReloadTimer.HasFired())
                {
                    await ReloadConfigAsync();
                }

                await captureTimer.WaitAsync(TimeSpan.FromMilliseconds(10));

                var tasks = new List<Task>();
                for (int i = 0; i < requestsPerSecond; i++)
                {
                    tasks.Add(helloWorldClient.GetAsync(endpoints.SelectRandom(1).First()));
                }

                await Task.WhenAll(tasks);
            }
        }

        /// <summary>
        /// Loads in configuration.
        /// </summary>
        /// <returns></returns>
        private async Task ReloadConfigAsync()
        {
            if (int.TryParse(GetEnvironmentVariable("REQUESTS_PER_SECOND"), out var rps))
            {
                requestsPerSecond = rps;
            }

            using (var sr = new StreamReader("/etc/load-generator/config.yaml"))
            {
                var config = NeonHelper.YamlDeserialize<dynamic>(await sr.ReadToEndAsync());

                // set base address
                helloWorldClient = new JsonClient()
                {
                    BaseAddress = new Uri(config["baseAddress"])
                };

                // set urls to be hit
                endpoints = new List<string>();
                
                foreach(var endpoint in config["urls"])
                {
                    endpoints.Add((string)(endpoint));
                }
            }
        }
    }
}
