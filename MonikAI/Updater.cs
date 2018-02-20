// File: Updater.cs
// Created: 20.02.2018
// 
// See <summary> tags for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Octokit;

namespace MonikAI
{
    public class Updater
    {
        private static readonly string StatePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MonikAI");

        private readonly List<Task> downloadTasks = new List<Task>();

        private readonly IGitHubClient github = new GitHubClient(new ProductHeaderValue("MonikAI"));

        private bool updateProgram;

        public async Task Init()
        {
            if (!MonikaiSettings.Default.AutoUpdate)
            {
                if (MonikaiSettings.Default.FirstLaunch)
                {
                    this.downloadTasks.Add(this.DownloadCSV());
                }

                return;
            }

            Directory.CreateDirectory(Updater.StatePath);

            // Retrieve GitHub releases
            var latestRelease = await this.github.Repository.Release.GetLatest("PiMaker", "MonikAI");

            if (MonikaiSettings.Default.GithubReleaseId == -1)
            {
                MonikaiSettings.Default.GithubReleaseId = latestRelease.Id;
                MonikaiSettings.Default.Save();
            }

            if (MonikaiSettings.Default.GithubReleaseId != latestRelease.Id)
            {
                // Application Update detected
                this.updateProgram = true;
                this.downloadTasks.Add(Task.Run(async () =>
                {
                    var client = new WebClient();
                    await client.DownloadFileTaskAsync(latestRelease.Assets.First().BrowserDownloadUrl,
                        Path.Combine(Updater.StatePath, "MonikAI.exe"));
                }));
            }

            // Retrieve CSV releases
            await this.DownloadCSV();
        }

        private async Task DownloadCSV()
        {
            var masterSha = (await this.github.Repository.Commit.Get("PiMaker", "MonikAI", "master")).Sha;
            if (masterSha != MonikaiSettings.Default.GithubMasterSHA)
            {
                this.downloadTasks.Add(Task.Run(async () =>
                {
                    var contents =
                        await this.github.Repository.Content.GetAllContentsByRef("PiMaker", "MonikAI", "/CSV",
                            "master");
                    foreach (var content in contents)
                    {
                        File.WriteAllText(Path.Combine(Updater.StatePath, content.Name), content.Content);
                    }

                    MonikaiSettings.Default.GithubMasterSHA = masterSha;
                    MonikaiSettings.Default.Save();
                }));
            }
        }

        public void PerformUpdate(MainWindow window)
        {
            Task.WaitAll(this.downloadTasks.ToArray());

            if (this.updateProgram && MonikaiSettings.Default.AutoUpdate)
            {
                var lastExp = new Expression("Wait a second, I will install it!", "j");
                lastExp.Executed += (sender, args) =>
                {
                    Process.Start(Path.Combine(Updater.StatePath, "MonikAI.exe"),
                        "/update " + Assembly.GetExecutingAssembly().Location);
                    Environment.Exit(0);
                };
                window.Say(new[]
                    {new Expression("Hey, I see there is an update available for my window.", "b"), lastExp});
            }
        }

        public void PerformUpdatePost()
        {
            if (Environment.GetCommandLineArgs()[1] == "/postupdate" &&
                File.Exists(Path.Combine(Updater.StatePath, "MonikAI.exe")))
            {
                File.Delete(Path.Combine(Updater.StatePath, "MonikAI.exe"));
            }

            if (Environment.GetCommandLineArgs()[1] != "/update")
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
