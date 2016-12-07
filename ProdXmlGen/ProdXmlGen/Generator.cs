using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;

namespace ProdXmlGen
{
    public class Generator
    {
        public static void GenerateXml(string platform, string input, string targetFolder)
        {
            if (Directory.Exists(targetFolder))
                Directory.Delete(targetFolder);
            Directory.CreateDirectory(targetFolder);

            XDocument xDoc = XDocument.Load(@"Template\ProductCenter.xml");
            XDocument itemDoc = XDocument.Load(@"Template\item.xml");

            string[] repos = Directory.GetDirectories(input, "*", SearchOption.TopDirectoryOnly);

            foreach (string repo in repos)
            {
                string repoName = Path.GetFileNameWithoutExtension(repo);
                string title = string.Empty;
                string screenshotUrl = string.Empty;
                string description = string.Empty;
                string zipUrl = string.Format(@"https://github.com/ThinkGeo/{0}/archive/master.zip", repoName);
                int start = 0, end = 0;

                string readMeFile = Directory.GetFiles(repo, "*.md", SearchOption.AllDirectories).FirstOrDefault();
                if (string.IsNullOrEmpty(readMeFile))
                {
                    continue;
                }
                string[] readMeLines = File.ReadAllLines(readMeFile);
                for (int i = 0; i < readMeLines.Count(); i++)
                {
                    if (readMeLines[i].Contains("#") && readMeLines[i].Contains("Sample-ForWebApi"))
                    {
                        title = readMeLines[i].Replace('#', ' ').Trim();
                    }

                    if (readMeLines[i].Contains("###") && readMeLines[i].Contains("Description"))
                    {
                        start = i;
                    }

                    if (readMeLines[i].Contains("![Screenshot]"))
                    {
                        string temp = readMeLines[i].Trim().Substring(12);
                        temp = temp.Replace('(', ' ');
                        screenshotUrl = temp.Replace(')', ' ');
                        end = i;
                    }
                }

                for (int i = start + 1; i < end; i++)
                {
                    description = description + readMeLines[i];
                }

                if (title.Contains("Sample Template for "))
                {
                    XElement titleElement = itemDoc.Root.Descendants("title").First();
                    titleElement.Value = title;
                    XElement herfElement = itemDoc.Root.Descendants("hyperlinks").Descendants("link").Descendants("herf").First();
                    herfElement.Value = zipUrl;
                    XElement awasomePictureElement = itemDoc.Root.Descendants("awasomePicture").Descendants("orginal").First();
                    awasomePictureElement.Value = screenshotUrl;
                    XElement descriptionElement = itemDoc.Root.Descendants("description").First();
                    descriptionElement.Value = description;

                    XElement items = xDoc.Root.Descendants("part").Descendants("subparts").Descendants("subpart").Descendants("items").First();
                    items.Add(itemDoc.Root);
                }
                else
                {
                    XElement titleElement = itemDoc.Root.Descendants("title").First();
                    titleElement.Value = title;
                    XElement herfElement = itemDoc.Root.Descendants("hyperlinks").Descendants("link").Descendants("herf").First();
                    herfElement.Value = zipUrl;
                    XElement awasomePictureElement = itemDoc.Root.Descendants("awasomePicture").Descendants("orginal").First();
                    awasomePictureElement.Value = screenshotUrl;
                    XElement descriptionElement = itemDoc.Root.Descendants("description").First();
                    descriptionElement.Value = description;
                    //Todo seconde part node.
                    XElement items = xDoc.Root.Descendants("part").Descendants("subparts").Descendants("subpart").Descendants("items").First();
                    items.Add(itemDoc.Root);
                }
            }
            xDoc.Save(Path.Combine(targetFolder, $"ProductCenter-{platform}.xml"));
        }

        private static string[] GetReadMeContent(string readMeUrl)
        {
            WebClient client = new WebClient();
            client.DownloadFile(new Uri(readMeUrl), "temp.md");
            string[] contents = File.ReadAllLines("temp.md");
            File.Delete("temp.md");
            return contents;
        }
    }
}
