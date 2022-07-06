using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace LoadGenerator
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var client = new HttpClient();

            while (true)
            {
                var tasks = new List<Task>();

                for (int i = 0; i < 10; i++)
                {
                    tasks.Add(client.GetAsync("http://hello-world"));
                }

                await Task.WhenAll(tasks);
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
}