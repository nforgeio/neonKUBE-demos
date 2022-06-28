﻿//-----------------------------------------------------------------------------
// FILE:	    WeatherController.cs
// CONTRIBUTOR: Marcus Bowyer
// COPYRIGHT:   Copyright (c) 2005-2022 by neonFORGE LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

using Neon.Collections;
using Neon.Common;
using Neon.Cryptography;
using Neon.Diagnostics;
using Neon.Net;
using Neon.Service;
using Neon.Tasks;
using Neon.Web;

using Prometheus;

namespace HelloWorld.Controllers
{
    [ApiController]
    public class HelloController : NeonControllerBase
    {
        private Service     helloWorldService;
        private JsonClient  weatherClient;
        private INeonLogger logger;

        private static readonly Counter requestCounter = Metrics.CreateCounter(
            $"{Program.Service.MetricsPrefix}_request_count",
            "Received requests.",
            configuration: new CounterConfiguration()
            {
                SuppressInitialValue = false,
                LabelNames = new string[] { "route" }
            });

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="helloWorldService"></param>
        /// <param name="weatherClient"></param>
        public HelloController(
            Service     helloWorldService,
            JsonClient  weatherClient,
            INeonLogger logger)
        {
            this.helloWorldService = helloWorldService;
            this.weatherClient     = weatherClient;
            this.logger            = logger;
        }

        /// <summary>
        /// Method to get weather for a specific zipcode.
        /// </summary>
        /// <returns>The current weather as a string</returns>
        [HttpGet("")]
        public async Task<ActionResult> HelloAsync()
        {
            await SyncContext.Clear;

            requestCounter.WithLabels(new string[] { "hello" }).Inc();

            logger.LogInfo($"Hello, World! From [{Dns.GetHostName()}]");

            return Content($@"<!DOCTYPE html>
<html>
<body>

<h3>Hello, World! [{Dns.GetHostName()}]</h3>

<form action=""kill"" method=""post"">
    <input type=""submit"" name=""killpod"" value=""Kill pod"" />
</form>
</body>
</html>", "text/html", Encoding.UTF8);
        }

        /// <summary>
        /// Method to get weather for a specific zipcode.
        /// </summary>
        /// <returns>The current weather as a string</returns>
        [HttpGet("weather")]
        public async Task<ActionResult<string>> WeatherAsync([FromQuery] string zipCode = null)
        {
            await SyncContext.Clear;

            requestCounter.WithLabels(new string[] { "weather" }).Inc();

            logger.LogInfo($"Processing weather request for zip: [{zipCode}]");

            var args = new ArgDictionary();
            args.Add("zipCode", zipCode);

            return await weatherClient.GetAsync<string>("http://weather-api/", args);

        }

        /// <summary>
        /// Sets current pod status to unhealthy.
        /// </summary>
        [HttpPost("kill")]
        public async Task<ActionResult<string>> KillAsync()
        {
            await SyncContext.Clear;

            requestCounter.WithLabels(new string[] { "kill" }).Inc();

            logger.LogInfo($"Killing pod: [{Dns.GetHostName()}]");

            await helloWorldService.SetStatusAsync(NeonServiceStatus.Unhealthy);

            return Redirect("/");
        }
    }
}