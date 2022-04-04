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
using Neon.Service;
using Neon.Web;

using Prometheus;

namespace WeatherApi.Controllers
{
    [ApiController]
    public class WeatherController : NeonControllerBase
    {
        private Service WeatherApiService;

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
            Service WeatherApiService
            )
        {
            this.WeatherApiService = WeatherApiService;
        }

        /// <summary>
        /// Method to get weather for a specific zipcode.
        /// </summary>
        /// <returns>The current weather as a string</returns>
        [Route("")]
        public async Task<string> GetWeatherByZipAzync([FromQuery] string zipCode)
        {
            requestCounter.Inc();

            var result = new StringBuilder();
            
            if (!string.IsNullOrEmpty(zipCode))
            {
                result.Append($"Weather in [{zipCode}]: ");
            }

            result.Append(weatherOptions.SelectRandom(1).FirstOrDefault());

            return await Task.FromResult(result.ToString());
        }
    }
}