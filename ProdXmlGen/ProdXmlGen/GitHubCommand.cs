using Octokit;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ProdXmlGen
{
    public static class GitHubCommand
    {
        private static string currentName = string.Empty;
        private static string currentPassWorld = string.Empty;
        private static GitHubClient client = new GitHubClient(new ProductHeaderValue("Test"));
        private static string gitCmdExeFilePath = "\"C:\\Program Files (x86)\\SmartGit\\git\\cmd\\git.exe\"";

        public static async Task CreateRepository(string repositoryName)
        {
            try
            {
                Repository rep = await client.Repository.Create("ThinkGeo", new NewRepository(repositoryName));
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public static async Task<IReadOnlyList<Repository>> GetAllRepositories()
        {
            try
            {
                IReadOnlyList<Repository> repositories = await client.Repository.GetAllForOrg("ThinkGeo");
                return repositories;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw e;
            }
        }

        public static string Clone(string sshUrl, string dtargetFolder, string repoName)
        {
            if (!Directory.Exists(dtargetFolder))
                Directory.CreateDirectory(dtargetFolder);
            string command = File.ReadAllText(@"..\..\Command\Clone.bat");
            command = command.Replace("args0", gitCmdExeFilePath);
            command = command.Replace("args1", dtargetFolder);
            command = command.Replace("args2", sshUrl);
            command = command.Replace("args3", repoName);
            if (!Directory.Exists("Temp\\Clone"))
                Directory.CreateDirectory("Temp\\Clone");
            string tempBat = "Temp\\Clone\\" + repoName + ".bat";
            File.WriteAllText(tempBat, command);

            Process process = new Process();
            process.StartInfo = new ProcessStartInfo(tempBat);
            process.StartInfo.UseShellExecute = false;
            process.Start();
            process.WaitForExit();
            return sshUrl;
        }
  }
}
