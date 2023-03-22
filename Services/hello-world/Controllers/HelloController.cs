using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Neon.Collections;
using Neon.Common;
using Neon.Cryptography;
using Neon.Diagnostics;
using Neon.Net;
using Neon.Service;
using Neon.Tasks;
using Neon.Web;
using OpenTelemetry.Trace;
using Prometheus;

namespace HelloWorld.Controllers
{
    [ApiController]
    public class HelloController : ControllerBase
    {
        public static string DataDirPath = "/var/helloworld";
        public static string TextPath    = $"{DataDirPath}/some.txt";

        private Service                  helloWorldService;
        private ILogger<HelloController> logger;
        private string                   podNamespace;
        private string                   podName;

        public HelloController(
            Service helloWorldService, 
            ILogger<HelloController> logger)
        {
            this.helloWorldService = helloWorldService;
            this.logger            = logger;
            this.podNamespace      = helloWorldService.GetEnvironmentVariable("POD_NAMESPACE", "pod-namespace");
            this.podName           = helloWorldService.GetEnvironmentVariable("POD_NAME", "pod-name");
        }

        [HttpGet("api")]
        public async Task<ActionResult> Api()
        {
            await SyncContext.Clear;

            logger.LogDebug($"HELLO request received");

            await Task.Delay(NeonHelper.PseudoRandomTimespan(TimeSpan.FromMilliseconds(125)));

            // cause some errors
            if (NeonHelper.PseudoRandomInt(100) <= 10)
            {
                logger?.LogErrorEx(() => "There was a bug in the code.");

                if (NeonHelper.PseudoRandomInt(3) < 1)
                {
                    return BadRequest();
                }
                else
                {
                    return StatusCode(500);
                }
            }

            return Ok();
        }

        [HttpPost("savetext")]
        public async Task<ActionResult> SaveText()
        {
            await SyncContext.Clear;

            var text = Request.Form["sometext"];
            logger.LogInformation($"Saving text: [{text}]");

            await System.IO.File.WriteAllTextAsync(TextPath, text);

            return Redirect("/storage");
        }

        [HttpPost("kill")]
        public async Task<ActionResult> KillPage()
        {
            await SyncContext.Clear;

            logger.LogInformation($"Killing pod: [{podNamespace}/{podName}]");

            await helloWorldService.SetStatusAsync(NeonServiceStatus.Unhealthy);

            return Redirect("/goodbye");
        }
    }
}