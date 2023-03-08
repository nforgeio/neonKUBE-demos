using System.Reflection;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neon.Common;
using Neon.IO;
using Neon.Kube;
using Neon.Kube.Operator;

namespace HelloWorldOperator
{
    public static partial class Program
    {
        public static Service Service { get; private set; }
        public static string Name = "hello-world-operator";
        public static IStaticDirectory Resources { get; private set; }

        public static async Task Main(string[] args)
        {
            KubeHelper.InitializeJson();
            Resources = Assembly.GetExecutingAssembly().GetResourceFileSystem("HelloWorldOperator.Resources");

            var k8s = KubernetesOperatorHost
               .CreateDefaultBuilder(args)
               .ConfigureOperator(configure =>
               {
                   configure.AssemblyScanningEnabled = true;
                   configure.Name = Name;
                   configure.DeployedNamespace = "default";
               })
               .ConfigureNeonKube()
               .UseStartup<OperatorStartup>()
               .Build();

            await k8s.RunAsync();

        }
    }
}
