using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace HelloWorld.Pages
{
    public class StorageModel : PageModel
    {
        public static string DataDirPath = "/var/helloworld";
        public static string TextPath = $"{DataDirPath}/some.txt";
        public string CurrentText { get; }
        public string StorageType { get; }
        public string PodName { get; }

        private readonly ILogger<StorageModel> logger;
        private readonly Service helloWorldService;

        public StorageModel(
            Service helloWorldService,
            ILogger<StorageModel> logger)
        {
            this.logger            = logger;
            this.helloWorldService = helloWorldService;
            this.CurrentText       = System.IO.File.ReadAllText(TextPath);
            this.StorageType       = helloWorldService.GetEnvironmentVariable("STORAGE_TYPE", "ephemeral");
            this.PodName           = helloWorldService.GetEnvironmentVariable("POD_NAME", "pod-name");
        }

        public void OnGet()
        {

        }
    }
}