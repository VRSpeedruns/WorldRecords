using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WorldRecords.Util;

namespace WorldRecords.Core
{
    class Heartbeat
    {
        private static GitHubClient client;

        public static void Start(Credentials creds)
        {
            client = new GitHubClient(new ProductHeaderValue("WorldRecords-Heartbeat"));
            client.Credentials = creds;

            Thread thread = new Thread(Loop);
            thread.Start();
        }

        static void Loop()
        {
            AsyncLoop().GetAwaiter().GetResult();
        }

        static async Task AsyncLoop()
        {
            while (true)
            {
                var time = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
                var release = new ReleaseUpdate();
                release.Body = time;
                var thisHeartbeat = false;

                FConsole.WriteLine($"&3Sending heartbeat.");
                try
                {
                    await client.Repository.Release.Edit("VRSRBot", "Heartbeats", 49565623, release); // 49565623 = WR's release
                    thisHeartbeat = true;
                    FConsole.WriteLine($"&3Heartbeat sent.");
                }
                catch
                {
                    FConsole.WriteLine($"&cHeartbeat not sent.");
                }
                
                if (thisHeartbeat) File.WriteAllText("files/heartbeat.txt", time);

                await Task.Delay(300000); // 5 minutes
            }
        }
    }
}
