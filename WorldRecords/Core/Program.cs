using Newtonsoft.Json;
using Octokit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WorldRecords.Entities;
using WorldRecords.Util;

namespace WorldRecords.Core
{
    class Program
    {
        private static Credentials creds;
        private static GitHubClient client;

        private static List<Game> games;
        private static List<Record> records;

        static void Main(string[] args)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            if (!Directory.Exists("files"))
            {
                Directory.CreateDirectory("files");
            }

            if (!File.Exists("files/userpass.txt"))
            {
                File.WriteAllText("files/userpass.txt", "");
            }

            var credsFile = File.ReadAllLines("files/userpass.txt");
            if (credsFile.Length > 1)
            {
                creds = new Credentials(credsFile[0], credsFile[1]);
            }
            else
            {
                Console.WriteLine("Please fill out `userpass.txt` with the following:\nGitHub username on line 1\nGitHub password/access token on line 2");
                Console.ReadLine();
                return;
            }

            if (!File.Exists("files/records.json"))
            {
                File.WriteAllText("files/records.json", "[]");
            }
            records = JsonConvert.DeserializeObject<List<Record>>(File.ReadAllText("files/records.json"));

            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            client = new GitHubClient(new ProductHeaderValue("WorldRecords"));
            client.Credentials = creds;

            while (true)
            {
                using (WebClient wc = new WebClient())
                {
                    FConsole.WriteLine("Getting games from VRSpeed.run.");
                    while (true)
                    {
                        try
                        {
                            var str = await wc.DownloadStringTaskAsync("https://vrspeed.run/vrsrassets/other/games.json");
                            games = JsonConvert.DeserializeObject<List<Game>>(str);
                            break;
                        }
                        catch (Exception e)
                        {
                            FConsole.WriteLine($"&cERROR: &fError when trying to get games from VRSpeed.run. Retrying in 7.5 seconds.\n&7 - \"{e.Message}\"");
                            await Task.Delay(7500);
                        }
                    }

                    for (var i = 0; i < games.Count; i++)
                    {
                        var game = games[i];
                        var req = "";

                        FConsole.WriteLine($"Checking game: {game.name} ({i + 1}/{games.Count})");

                        try
                        {
                            req = await wc.DownloadStringTaskAsync($"https://www.speedrun.com/api/v1/games/{game.id}?embed=categories,categories.variables&" + DateTimeOffset.Now.ToUnixTimeSeconds());
                        }
                        catch (Exception e)
                        {
                            FConsole.WriteLine($"&cERROR: &fError when trying to access game \"{game.id}\". Retrying in 7.5 seconds.\n&7 - \"{e.Message}\"");
                            i--;
                        }

                        if (req != "")
                        {
                            dynamic data = ((dynamic)JsonConvert.DeserializeObject(req)).data;

                            for (var k = 0; k < data.categories.data.Count; k++)
                            {
                                var category = data.categories.data[k];

                                if (category.type != "per-game") continue;

                                List<dynamic> subcats = new List<dynamic>();

                                foreach (var variable in category.variables.data)
                                {
                                    if ((bool)variable["is-subcategory"])
                                        subcats.Add(variable);
                                }

                                if (subcats.Count > 0)
                                {
                                    var combos = new List<List<string>>();

                                    foreach (var variable in subcats)
                                    {
                                        var list = new List<string>();
                                        foreach (var __value in variable.values.values)
                                        {
                                            string _value = JsonConvert.SerializeObject(__value);
                                            var id = _value.Split(new[] { '"' }, 2)[1].Split('"')[0];

                                            if (!game.ignoredVariables.Any(v => v.id == (string)variable.id && v.value == id))
                                            {
                                                var query = $"&var-{variable.id}={id}";
                                                list.Add(query);
                                            }
                                        }
                                        if (list.Count > 0) combos.Add(list);
                                    }

                                    var allCombinations = Func.CartesianProduct(combos);

                                    for (var m = 0; m < allCombinations.Count(); m++)
                                    {
                                        var vars = string.Join("", allCombinations.ElementAt(m));
                                        req = "";

                                        try
                                        {
                                            req = await wc.DownloadStringTaskAsync($"https://www.speedrun.com/api/v1/leaderboards/" +
                                            $"{data.id}/category/{category.id}?embed=variables&top=1{vars}&{DateTimeOffset.Now.ToUnixTimeSeconds()}");
                                        }
                                        catch (Exception e)
                                        {
                                            FConsole.WriteLine($"&cERROR: &fError when trying to access category \"{category.id} ({vars})\" of game \"{game.id}\". Retrying in 7.5 seconds.\n&7 - \"{e.Message}\"");
                                            m--;
                                        }

                                        if (req != "")
                                        {
                                            var run = ((dynamic)JsonConvert.DeserializeObject(req)).data;
                                            if (run.runs.Count > 0)
                                            {
                                                await CheckForNewWR(run.runs[0].run, vars);
                                            }
                                            else
                                            {
                                                var record = new Record("", (string)run.game, (string)run.category, vars);
                                                if (!records.Any(r => r.IsSameCategory(record)))
                                                {
                                                    records.Add(record);
                                                    File.WriteAllText("files/records.json", JsonConvert.SerializeObject(records, Formatting.Indented));
                                                }
                                            }
                                        }

                                        await Task.Delay(7500);
                                    }
                                }
                                else
                                {
                                    req = "";

                                    try
                                    {
                                        req = await wc.DownloadStringTaskAsync($"https://www.speedrun.com/api/v1/leaderboards/" +
                                        $"{data.id}/category/{category.id}?embed=variables&top=1&{DateTimeOffset.Now.ToUnixTimeSeconds()}");
                                    }
                                    catch (Exception e)
                                    {
                                        FConsole.WriteLine($"&cERROR: &fError when trying to access category \"{category.id}\" of game \"{game.id}\". Retrying in 7.5 seconds.\n&7 - \"{e.Message}\"");
                                        k--;
                                    }

                                    if (req != "")
                                    {
                                        var run = ((dynamic)JsonConvert.DeserializeObject(req)).data;
                                        if (run.runs.Count > 0)
                                        {
                                            await CheckForNewWR(run.runs[0].run);
                                        }
                                        else
                                        {
                                            var record = new Record("", (string)run.game, (string)run.category, "");
                                            if (!records.Any(r => r.IsSameCategory(record))) //new category with no run - save with empty id
                                            {
                                                records.Add(record);
                                                File.WriteAllText("files/records.json", JsonConvert.SerializeObject(records, Formatting.Indented));
                                            }
                                            else
                                            {
                                                records.RemoveAll(r => r.IsSameCategory(record));
                                                records.Add(record);
                                                File.WriteAllText("files/records.json", JsonConvert.SerializeObject(records, Formatting.Indented));
                                            }
                                        }
                                    }
                                }

                                await Task.Delay(7500);
                            }
                        }

                        await Task.Delay(7500);
                    }
                }
            }
        }

        static async Task CheckForNewWR(dynamic run, string subcats = "")
        {
            var record = new Record((string)run.id, (string)run.game, (string)run.category, subcats);

            if (records.Any(r => r.IsSameCategory(record)))
            {
                var existing = records.First(r => r.IsSameCategory(record));

                if (!record.Equals(existing))
                {
                    records.Remove(existing);
                    records.Add(record);

                    File.WriteAllText("files/records.json", JsonConvert.SerializeObject(records, Formatting.Indented));

                    FConsole.WriteLine($"- New record found (ID: \"{record.id}\")");
                    await PostWR(record.id);
                }
            }
            else //new game/category; dont post record
            {
                records.Add(record);
                File.WriteAllText("files/records.json", JsonConvert.SerializeObject(records, Formatting.Indented));
            }
        }

        static async Task PostWR(string id)
        {
            var time = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
            var release = new NewRelease(time.ToString());
            release.Name = id;

            await client.Repository.Release.Create("VRSRBot", "test", release); //test repo
        }
    }
}
