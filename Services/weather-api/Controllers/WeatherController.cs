//-----------------------------------------------------------------------------
// FILE:	    WeatherController.cs
// CONTRIBUTOR: Marcus Bowyer
// COPYRIGHT:   Copyright (c) 2005-2022 by neonFORGE LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

using Neon.Common;
using Neon.Cryptography;
using Neon.Diagnostics;
using Neon.Service;
using Neon.Web;

using Prometheus;

namespace WeatherApi.Controllers
{
    [ApiController]
    public class WeatherController : NeonControllerBase
    {
        private Service     weatherApiService;
        private INeonLogger logger;

        private string[] weatherOptions = { "rainy", "sunny", "cloudy", "really rainy" };

        private static readonly Counter requestCounter = Metrics.CreateCounter(
            $"{Program.Service.MetricsPrefix}_request_count", 
            "Received weather requests.",
            new CounterConfiguration()
                {
                    SuppressInitialValue = false
                });

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="WeatherApiService"></param>
        public WeatherController(
            Service     weatherApiService,
            INeonLogger logger)
        {
            this.weatherApiService = weatherApiService;
            this.logger            = logger;
        }

        /// <summary>
        /// Method to get weather for a specific zipcode.
        /// </summary>
        /// <returns>The current weather as a string</returns>
        [Route("")]
        public async Task<ActionResult<string>> GetWeatherByZipAzync([FromQuery] string zipCode)
        {
            requestCounter.Inc();

            if (string.IsNullOrEmpty(zipCode))
            {
                logger.LogError($"ZipCode is empty");

                return BadRequest("ZipCode is empty");
            }

            var weather = weatherOptions.SelectRandom(1).FirstOrDefault();

            var result = new StringBuilder();
            
            result.Append($"Weather in [{zipCode}]: ");
            result.Append(weather);

            logger.LogInfo($"Weather in zip: [{zipCode}] is [{weather}]");

            return Ok(await Task.FromResult(result.ToString()));
        }
    }
}