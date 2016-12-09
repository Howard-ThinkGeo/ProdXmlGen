using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ProdXmlGen
{
    class Program
    {
        public static List<string> AllPlatforms = new List<string> { "Android", "iOS", "Mvc", "WebApi", "WebForms", "WinForms", "Wpf" };

        static void Main(string[] args)
        {
            List<string> tempPlatforms = new List<string>();
            string outputFolder = string.Empty;

            switch (args.Length)
            {
                case 0:
                    tempPlatforms = AllPlatforms;
                    outputFolder = "output";
                    break;
                case 1:
                    if (GetPlatforms(args[0]) != null)
                    {
                        tempPlatforms = GetPlatforms(args[0]).ToList();
                        break;
                    }
                    else if (!string.IsNullOrEmpty(GetOutputPath(args[0])))
                    {
                        outputFolder = GetOutputPath(args[0]);
                        break;
                    }
                    else
                    {
                        ShowHelp();
                        return;
                    }
                case 2:
                    if (GetPlatforms(args[0]) != null && !string.IsNullOrEmpty(GetOutputPath(args[1])))
                    {
                        tempPlatforms = GetPlatforms(args[0]).ToList();
                        outputFolder = GetOutputPath(args[1]);
                        break;
                    }
                    else if (GetPlatforms(args[1]) != null && !string.IsNullOrEmpty(GetOutputPath(args[0])))
                    {
                        tempPlatforms = GetPlatforms(args[1]).ToList();
                        outputFolder = GetOutputPath(args[0]);
                        break;
                    }
                    ShowHelp();
                    return;
                default:
                    ShowHelp();
                    return;
            }

            GenerateXml(tempPlatforms, outputFolder);
            Console.WriteLine("Complete !");
            Console.ReadLine();
        }

        public static IReadOnlyList<Repository> GetAllRepositories(string userName, string passWorld)
        {
            try
            {
                GitHubClient client = new GitHubClient(new ProductHeaderValue("Test"));
                client.Credentials = new Credentials(userName, passWorld);

                Task<IReadOnlyList<Repository>> repositories = client.Repository.GetAllForOrg("ThinkGeo");
                repositories.Wait();
                return repositories.Result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw e;
            }
        }

        private static string[] GetPlatforms(string arg)
        {
            if (string.IsNullOrEmpty(Regex.Match(arg, "^-p=").Value)) return null;
            string[] platforms = arg.Remove(0, 3).Split(',').Distinct().ToArray();
            foreach (string platform in platforms)
            {
                if (!AllPlatforms.Contains(platform))
                {
                    Console.WriteLine("platforms Only exist Android,iOS,Mvc,WebApi,WebForms,WinForms,Wpf");
                    return null;
                }
            }
            return platforms;
        }

        private static string GetOutputPath(string arg)
        {
            if (string.IsNullOrEmpty(Regex.Match(arg, "^-o=").Value)) return null;
            string output = arg.Remove(0, 3);
            if (string.IsNullOrEmpty(Regex.Match(output, @"^[^ \t]+[ \t]+(.*)$").Value))
            {
                Console.WriteLine("The pattern of path is erorr");
                return null;
            }
            return output;
        }

        private static void ShowHelp()
        {
            Console.WriteLine(@"usage: [-h] [-p=value] [-o=value]
    -h             Show help information
    -p= value       platforms: Android,iOS,Mvc,WebApi,WebForms,WinForms,Wpf

                   Pattern: XX, XX,...
                   Defailt value: all
    -o= value       Output path.");
        }

        private static void GenerateXml(List<string> platforms, string outputFolder)
        {
            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            var repos = GetAllRepositories("mikeyu@thinkgeo.com", "github12348765");
            int index = 0;
            foreach (string platform in platforms)
            {
                XDocument xDoc = XDocument.Load(@"Template\ProductCenter.xml");

                foreach (Repository repo in repos)
                {
                    string sshUrl = repo.SshUrl;
                    if (!sshUrl.Contains(string.Format("-For{0}", platform))) continue;

                    string readMeContent = string.Empty;
                    string gitName = sshUrl.Split('/').Last();
                    string repoName = gitName.Remove(gitName.Length - 4, 4);

                    string url = string.Format("https://raw.githubusercontent.com/ThinkGeo/{0}/master/README.md", repoName);
                    try
                    {
                        WebRequest request = HttpWebRequest.Create(url);
                        WebResponse response = request.GetResponse();
                        StreamReader reader = new StreamReader(response.GetResponseStream());
                        readMeContent = reader.ReadToEnd();

                        XDocument itemDoc = XDocument.Load(@"Template\item.xml");
                        XElement titleElement = itemDoc.Root.Descendants("title").First();
                        titleElement.Value = Regex.Match(readMeContent, "^# [a-zA-z\\s]*[a-zA-z\\n]*").Value.Replace("#", "").Trim();
                        XElement zipElement = itemDoc.Root.Descendants("hyperlinks").Descendants("link").Descendants("herf").First();
                        zipElement.Value = string.Format(@"https://github.com/ThinkGeo/{0}/archive/master.zip", repoName).Trim();
                        XElement awasomePictureElement = itemDoc.Root.Descendants("awasomePicture").Descendants("orginal").First();
                        awasomePictureElement.Value = $"https://raw.githubusercontent.com/ThinkGeo/{repoName}/master/ScreenShot.png";
                        XElement descriptionElement = itemDoc.Root.Descendants("description").First();
                        descriptionElement.Value = Regex.Match(readMeContent, @"### Description[\s*a-zA-z\S*\n]*!\[").Value.Replace("### Description", "").Replace("![", "").Trim();

                        if (titleElement.Value.Contains("Sample Template for "))
                        {
                            XElement projectTemplatesElement = xDoc.Root.Descendants("part").Where(e => (e.Attribute("type").Value.Equals("MapSuiteProductCenter.ProductProjectTemplatesPart"))).Descendants("subparts").Descendants("subpart").Descendants("items").First();
                            projectTemplatesElement.Add(itemDoc.Root);
                        }
                        else
                        {
                            XElement sourceCodeLinkeElement = new XElement("link");
                            sourceCodeLinkeElement.Add(new XElement("herf", string.Format(@"https://github.com/ThinkGeo/{0}", repoName).Trim()));
                            sourceCodeLinkeElement.Add(new XElement("text", "View Source Code"));
                            itemDoc.Root.Descendants("hyperlinks").First().Add(sourceCodeLinkeElement);
                            XElement codeSampleElement = xDoc.Root.Descendants("part").Where(e => (e.Attribute("type").Value.Equals("MapSuiteProductCenter.ProductCodeSamplePart"))).Descendants("subparts").Descendants("subpart").Descendants("items").First();
                            codeSampleElement.Add(itemDoc.Root);
                        }

                        index++;
                        Console.WriteLine($"Done {index}/{repos.Count}: {repoName}");
                    }
                    catch (Exception err)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{err.Message} : {url}");
                        Console.ResetColor();
                    }
                }
                xDoc.Save(Path.Combine(outputFolder, $"ProductCenter-{platform}.xml"));
            }
        }
    }
}
