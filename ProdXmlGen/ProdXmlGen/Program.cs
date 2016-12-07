using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProdXmlGen
{
    class Program
    {
        public static List<string> AllPlatforms = new List<string> { "Android", "iOS", "Mvc", "WebApi", "WebForms", "WinForms", "Wpf" };
        static void Main(string[] args)
        {
            //Generator.GenerateXml("all", @"C:\Users\mikeyu\Desktop\New folder", "output");
            if (args.Length == 2)
            {
                string[] args1 = args[0].Split(',');
                string outputFolder = args[1];

                List<string> tempPlatforms = new List<string>();
                if (args1.Length == 1 && args1[0].Equals("all"))
                {
                    tempPlatforms = AllPlatforms;
                }
                else
                {
                    foreach (string platform in args1)
                        tempPlatforms.Add(platform);
                }
                Clone(tempPlatforms);
                Console.WriteLine("Complete clone!");
                GenerateXml(tempPlatforms, outputFolder);
                Console.WriteLine("Complete !");
            }
        }

        private static void GenerateXml(List<string> platforms, string outputFolder)
        {
            if (Directory.Exists(outputFolder))
                Directory.Delete(outputFolder, true);
            Directory.CreateDirectory(outputFolder);

            foreach (string platform in platforms)
            {
                Generator.GenerateXml(platform, platform, outputFolder);
            }
        }

        private static void Clone(List<string> platforms)
        {
            var task = GitHubCommand.GetAllRepositories();
            task.Wait();
            if (Directory.Exists("Repos"))
                Directory.Delete("Repos", true);
            Directory.CreateDirectory("Repos");

            foreach (string platform in platforms)
            {
                CloneAllRepositoriesOfOnePlatform(platform, Path.Combine("Repos", platform), task.Result);
            }
        }

        public static void CloneAllRepositoriesOfOnePlatform(string platform, string outputDirectory, IReadOnlyList<Repository> repos)
        {
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, true);
            Directory.CreateDirectory(outputDirectory);
            foreach (Repository repo in repos)
            {
                string sshUrl = repo.SshUrl;
                string gitName = sshUrl.Split('/').Last();
                string repoName = gitName.Remove(gitName.Length - 4, 4);
                if (sshUrl.Contains(string.Format("-For{0}", platform)))
                {
                    string result = GitHubCommand.Clone(sshUrl, outputDirectory, repoName);
                    Console.WriteLine(result);
                    break;
                }
            }
        }
    }
}
