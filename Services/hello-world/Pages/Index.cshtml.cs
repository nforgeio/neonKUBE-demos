using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

using Neon.Common;

namespace HelloWorld.Pages
{
    public class IndexModel : PageModel
    {

        public string PodNamespace { get; }
        public string PodName { get; }
        public string NodeName { get; }

        public string User { get; set; }

        private readonly ILogger<IndexModel> logger;
        private readonly Service helloWorldService;

        public IndexModel(
            Service helloWorldService,
            ILogger<IndexModel> logger)
        {
            this.logger            = logger;
            this.helloWorldService = helloWorldService;
            this.PodNamespace      = helloWorldService.GetEnvironmentVariable("POD_NAMESPACE", "pod-namespace");
            this.PodName           = helloWorldService.GetEnvironmentVariable("POD_NAME", "pod-name");
            this.NodeName          = helloWorldService.GetEnvironmentVariable("NODE_NAME", "node-name");
        }

        public void OnGet()
        {
            this.User = NeonHelper.JsonSerialize(HttpContext.User.Identity.Name);
        }
    }
}