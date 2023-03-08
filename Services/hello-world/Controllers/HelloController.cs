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
    public class HelloController : NeonControllerBase
    {
        public static string DataDirPath = "/var/helloworld";
        public static string TextPath    = $"{DataDirPath}/some.txt";

        private Service     helloWorldService;
        private ILogger     logger;
        private string      podName;

        private static readonly Counter requestCounter = Metrics.CreateCounter(
            name:          "helloworld_requests_total",
            help:          "Received requests.",
            configuration: new CounterConfiguration()
            {
                SuppressInitialValue = false,
                LabelNames           = new string[] { "route" },
                ExemplarBehavior     = new ExemplarBehavior()
                {
                    DefaultExemplarProvider = (metric, value) => Exemplar.FromTraceContext(),
                    NewExemplarMinInterval = TimeSpan.FromMinutes(1)
                }
            });

        private static readonly Counter errorCounter = Metrics.CreateCounter(
            name:          "helloworld_errors_total",
            help:          "Number of errors.",
            configuration: new CounterConfiguration()
            {
                SuppressInitialValue = false,
                LabelNames           = new string[] { "route", "code" },
                ExemplarBehavior = new()
                {
                    DefaultExemplarProvider = (metric, value) => Exemplar.FromTraceContext(),
                    NewExemplarMinInterval = TimeSpan.FromMinutes(1)
                }
            });

        private static readonly Histogram requestDuration = Metrics.CreateHistogram(
            name: "helloworld_request_duration_seconds",
            help: "Request duration in seconds.",
            configuration: new HistogramConfiguration()
            {
                SuppressInitialValue = false,
                LabelNames = new string[] { "route" },
                ExemplarBehavior = new()
                {
                    DefaultExemplarProvider = (metric, value) => value < 0.25 ? Exemplar.None : Exemplar.FromTraceContext(),
                    NewExemplarMinInterval = TimeSpan.FromMinutes(1)
                }
            });

        public HelloController(Service helloWorldService, ILogger logger)
        {
            this.helloWorldService = helloWorldService;
            this.logger            = logger;
            this.podName           = Dns.GetHostName();
        }

        [HttpGet("")]
        public async Task<ActionResult> HomePage(
            [FromQuery] bool disableErrors = false,
            [FromQuery] bool disableWait   = false)
        {
            await SyncContext.Clear;

            using var activity = TelemetryHub.ActivitySource?.StartActivity();
            using var timer    = requestDuration.WithLabels(new string[] { "hello" }).NewTimer();

            var traceId = Activity.Current?.Id;
            requestCounter.WithLabels(new string[] { "hello" }).Inc();

            logger.LogDebug($"HELLO request received");

            if (!disableWait)
            {
                // introduce some random latency
                await Task.Delay(NeonHelper.PseudoRandomTimespan(TimeSpan.FromMilliseconds(125)));
            }

            if (!disableErrors)
            {
                // cause some errors
                if (NeonHelper.PseudoRandomInt(100) <= 10)
                {
                    var error = new Exception();
                    logger?.LogErrorEx(error, "There was a bug in the code.");

                    if (NeonHelper.PseudoRandomInt(3) < 1)
                    {
                        errorCounter.WithLabels(new string[] { "hello", "400" }).Inc();
                        return BadRequest();
                    }
                    else
                    {
                        errorCounter.WithLabels(new string[] { "hello", "500" }).Inc();
                        throw error;
                    }

                    
                }
            }

            var currentText = await System.IO.File.ReadAllTextAsync(TextPath);

            return Content(
$@"<!DOCTYPE html>
<html>
<body style=""background-color:white;"">
    <a href='/'>[Home]</a>
    <h3>
    Hello, World!<br/></br>
    From pod: {podName}
    </h3>

    <form action=""kill"" method=""post"">
        <input type=""submit"" name=""killpod"" value=""Kill pod"" />
    </form>

    <p>Current text: {currentText.Trim()}</p>
    <form action=""savetext"" method=""post"">
        <label for=""sometext"">Change text:</label>
        <input type=""text"" id=""sometext"" name=""sometext"" value="""">
        <input type=""submit"" value=""Change Text"">
    </form>
</body>
</html>",
contentType:     "text/html",
contentEncoding: Encoding.UTF8);
        }

        [HttpPost("savetext")]
        public async Task<ActionResult> SaveText()
        {
            await SyncContext.Clear;

            using var activity = TelemetryHub.ActivitySource?.StartActivity();
            using var timer = requestDuration.WithLabels(new string[] { "savetext" }).NewTimer();

            var text = Request.Form["sometext"];
            logger.LogInformation($"Saving text: [{text}]");

            await System.IO.File.WriteAllTextAsync(TextPath, text);

            return Redirect("/?disableErrors=true&disableWait=true");
        }

        [HttpPost("kill")]
        public async Task<ActionResult> KillPage()
        {
            await SyncContext.Clear;

            using var activity = TelemetryHub.ActivitySource?.StartActivity();
            using var timer    = requestDuration.WithLabels(new string[] { "kill" }).NewTimer();

            requestCounter.WithLabels(new string[] { "kill" }).Inc();

            logger.LogInformation($"Killing pod: [{podName}]");

            await helloWorldService.SetStatusAsync(NeonServiceStatus.Unhealthy);

            return Content(
$@"<!DOCTYPE html>
<html>
<body>
    <a href='/'>[Home]</a>
    <h3>Goodbye, World! [{podName}]</h3>
</body>
</html>",
contentType:     "text/html",
contentEncoding: Encoding.UTF8);
        }
    }
}