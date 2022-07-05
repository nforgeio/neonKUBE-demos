//-----------------------------------------------------------------------------
// FILE:	    Program.cs
// CONTRIBUTOR: Marcus Bowyer
// COPYRIGHT:   Copyright (c) 2005-2022 by neonFORGE LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace LoadGenerator
{
    public static class Program
    {
        private static int        concurrentRequests = 10;
        public static async Task Main(string[] args)
        {
            var client = new HttpClient();

            while (true)
            {
                var tasks = new List<Task>();
                for (int i = 0; i < concurrentRequests; i++)
                {
                    tasks.Add(client.GetAsync("http://hello-world"));
                }

                await Task.WhenAll(tasks);

                await Task.Delay(1000);
            }
        }
    }
}