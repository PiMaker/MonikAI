// File: Updater.cs
// Created: 20.02.2018
// 
// See <summary> tags for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;

namespace MonikAI
{
    public class Updater
    {
        private static readonly string StatePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MonikAI");

        private readonly List<Task> downloadTasks = new List<Task>();

        private bool updateProgram;

        public async Task Init()
        {
            var dirExisted = Directory.Exists(Updater.StatePath);
            Directory.CreateDirectory(Updater.StatePath);

            if (!MonikaiSettings.Default.AutoUpdate && dirExisted)
            {
                return;
            }

            // Perform config exchange
            var client = new WebClient();
            var onlineConfigRaw = await client.DownloadStringTaskAsync("https://pimaker.github.io/MonikAI/update.json");
            UpdateConfig onlineConfig;

            try
            {
                onlineConfig = JsonConvert.DeserializeObject<UpdateConfig>(onlineConfigRaw);
            }
            catch (Exception e)
            {
                MessageBox.Show("An error has occured while updating MonikAI: " + e.Message, "Warning");
                return;
            }

            // General update
            if (string.IsNullOrWhiteSpace(MonikaiSettings.Default.LastUpdateConfig))
            {
                MonikaiSettings.Default.LastUpdateConfig = onlineConfigRaw;
            }

            var localConfig =
                JsonConvert.DeserializeObject<UpdateConfig>(MonikaiSettings.Default.LastUpdateConfig);

            if (localConfig.ProgramVersion < onlineConfig.ProgramVersion)
            {
                // Program update
                this.downloadTasks.Add(Task.Run(async () =>
                {
                    this.updateProgram = true;
                    var path = Path.Combine(Updater.StatePath, "MonikAI.exe");
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                    await client.DownloadFileTaskAsync(onlineConfig.ProgramURL, path);
                }));
            }

            if (localConfig.ResponsesVersion < onlineConfig.ResponsesVersion || !dirExisted || MonikaiSettings.Default.FirstLaunch)
            {
                // CSV update
                this.downloadTasks.Add(this.DownloadCSV(onlineConfig));
            }

            MonikaiSettings.Default.LastUpdateConfig = onlineConfigRaw;
            MonikaiSettings.Default.Save();
        }

        private async Task DownloadCSV(UpdateConfig config)
        {
            var client = new WebClient();
            foreach (var responseURL in config.ResponseURLs)
            {
                var path = Path.Combine(Updater.StatePath, GetFileNameFromUrl(responseURL));
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                await client.DownloadFileTaskAsync(responseURL, path);
            }
        }

        static readonly Uri someBaseUri = new Uri("http://canbeanything");

        // From: https://stackoverflow.com/a/40361205
        static string GetFileNameFromUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                uri = new Uri(Updater.someBaseUri, url);

            return Path.GetFileName(uri.LocalPath);
        }

        public async Task PerformUpdate(MainWindow window)
        {
            if (this.updateProgram && MonikaiSettings.Default.AutoUpdate)
            {
                var closed = false;

                var lastExp = new Expression("Wait a second, I will install it!", "j");
                lastExp.Executed += (sender, args) =>
                {
                    Task.WaitAll(this.downloadTasks.ToArray());

                    Process.Start(Path.Combine(Updater.StatePath, "MonikAI.exe"),
                        "/update " + Assembly.GetExecutingAssembly().Location);
                    closed = true;
                    Environment.Exit(0);
                };
                window.Say(new[]
                    {new Expression("Hey, I see there is an update available for my window.", "b"), lastExp});

                while (!closed)
                {
                    await Task.Delay(100);
                }
            }
            else
            {
                Task.WaitAll(this.downloadTasks.ToArray());
            }
            
            // Validate downloads have actually occured
            foreach (var file in Directory.GetFiles(Updater.StatePath))
            {
                if (file.EndsWith(".csv") || new FileInfo(file).Length > 0)
                {
                    return;
                }
            }

            // Invalid state detected, no responses available
            MessageBox.Show("An error has occured loading MonikAI data. Are you connected to the internet? If you disable Auto-Update in the settings you don't need to be connected to the Internet when MonikAI launches. NOTE: If this is your first time launching MonikAI, you need to be connected to the internet regardless!", "Error");
            Environment.Exit(1);
        }

        public void PerformUpdatePost()
        {
            var args = Environment.GetCommandLineArgs();

            if (args.Length <= 1)
            {
                return;
            }

            if (args.Length > 1 && args[1] == "/postupdate" &&
                File.Exists(Path.Combine(Updater.StatePath, "MonikAI.exe")))
            {
                File.Delete(Path.Combine(Updater.StatePath, "MonikAI.exe"));
                return;
            }

            if (args.Length > 1 && args[1] != "/update")
            {
                return;
            }

            var updatePath = Environment.GetCommandLineArgs()[2];
            var thisPath = Assembly.GetExecutingAssembly().Location;

            if (thisPath == updatePath)
            {
                MessageBox.Show(
                    "Really? You use your MonikAI from the AppData directory? Please don't, this breaks my entire update routine...",
                    "Error");
                Environment.Exit(1);
            }

            if (File.Exists(updatePath))
            {
                File.Delete(updatePath);
            }

            File.Copy(thisPath, updatePath);

            Process.Start(updatePath, "/postupdate");
            Environment.Exit(0);
        }
    }
}
