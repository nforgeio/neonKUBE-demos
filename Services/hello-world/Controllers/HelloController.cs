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
        private INeonLogger logger;
        private string      podName;

        private static readonly Counter requestCounter = Metrics.CreateCounter(
            name:          "helloworld_request_count",
            help:          "Received requests.",
            configuration: new CounterConfiguration()
            {
                SuppressInitialValue = false,
                LabelNames           = new string[] { "route" }
            });

        public HelloController(Service helloWorldService, INeonLogger logger)
        {
            this.helloWorldService = helloWorldService;
            this.logger            = logger;
            this.podName           = Dns.GetHostName();
        }

        [HttpGet("")]
        public async Task<ActionResult> HomePage()
        {
            await SyncContext.Clear;

            requestCounter.WithLabels(new string[] { "hello" }).Inc();

            logger.LogDebug($"HELLO request received");

            return Content(
$@"<!DOCTYPE html>
<html>
<body>
    <a href='/'>[Home]</a>
    <h3>
    Hello, World!<br/></br>
    From pod: {podName}
    </h3>

    <form action=""kill"" method=""post"">
        <input type=""submit"" name=""killpod"" value=""Kill pod"" />
    </form>
</body>
</html>",
contentType:     "text/html",
contentEncoding: Encoding.UTF8);
        }

        [HttpPost("kill")]
        public async Task<ActionResult> KillPage()
        {
            await SyncContext.Clear;

            logger.LogInfo($"Killing pod: [{podName}]");
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