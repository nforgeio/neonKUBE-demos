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

            for (int i = 0; i < 10; i++)
            {
                _ = LoadGen(client);
            }

            await Task.Delay(TimeSpan.FromDays(1));
        }

        private static async Task LoadGen(HttpClient client)
        {
            while (true)
            {
                try
                {
                    await client.GetAsync("http://hello-world");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"ERROR: {e.GetType().FullName}: {e.Message}");
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
}
