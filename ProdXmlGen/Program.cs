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
        public static Dictionary<string, string> arguments = new Dictionary<string, string>();

        static void Main(string[] args)
        {
            #region test case
            //exe -h
            //exe
            //exe -p=Mvc,Wpf
            //exe -o=output2
            //exe -p=Mvc,Wpf -o=output2
            #endregion

            arguments.Add("h", null);
            arguments.Add("p", "Android,iOS,Mvc,WebApi,WebForms,WinForms,Wpf");
            arguments.Add("o", "output");

            foreach (string s in args)
            {
                if (s.Trim().Equals("-h"))
                {
                    arguments["h"] = "Not null";
                    break;
                }
                arguments[s.Trim().Split('=')[0][1].ToString()] = s.Trim().Split('=')[1];
            }

            string platforms = string.Empty;
            string outputFolder = string.Empty;

            if (arguments["h"] != null)
            {
                Console.WriteLine(@"Usage: exePath [-options] [value]");
                Console.WriteLine("where options include:");
                Console.WriteLine(
                    string.Format("{0,-10} {1}", "-h", "print this help message")
                    + Environment.NewLine +
                    string.Format("{0,-10} {1}", "-p", "set platforms which are comma separated")
                    + Environment.NewLine +
                    string.Format("{0,-10} {1}", "-o", "set output folder")
                    );
            }
            else
            {
                platforms = arguments["p"];
                outputFolder = arguments["o"];
            }

            GenerateXml(platforms.Split(',').ToList(), outputFolder);
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
                    if (!sshUrl.Contains(string.Format("-For{0}", platform)))
                        continue;

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
