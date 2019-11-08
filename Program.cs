using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using HtmlAgilityPack;
using McMaster.Extensions.CommandLineUtils;

namespace topographer
{
    class Program
    {
        static void Main(string[] args)
        {
            // Define cli options parser
            var app = new CommandLineApplication();
            app.HelpOption("-h|--help");
            var optionUrl = app.Option<string>("-u|--url <URL>", "What we will parse? Full Url.", CommandOptionType.SingleValue);
            var optionSavePath = app.Option<string>("-s|--save-to <PATH>", "Where we will save sitemap-file? Path to folder.", CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                // Exit if url or path is not present
                if (!optionUrl.HasValue() || !optionSavePath.HasValue())
                {
                    Console.WriteLine(app.GetHelpText());
                    Environment.Exit(1);
                }

                Config.savePath = optionSavePath.Value() + Path.DirectorySeparatorChar + "sitemap.xml";
                Config.targetHost = optionUrl.Value();

                GenerateSitemap();
                SaveXmlFile();
            });

            app.Execute(args);
        }

        public static void GenerateSitemap()
        {
            var indexUrlUri = new Uri(Config.targetHost);
            var web = new HtmlWeb();
            var db = LinkContext.GetInstance();

            db.Links.Add((new Link {Url = Config.targetHost}));
            db.SaveChanges();

            while (db.Links.Count(link => !link.IsParsed) > 0)
            {
                Parallel.ForEach(db.Links.Where(link => !link.IsParsed), (currentLink) =>
                {
                    Console.WriteLine("Parsing {0}", currentLink.Url);
                    var doc = web.Load(currentLink.Url); // Load url from order

                    // Grab and parse all links from loaded page
                    Parallel.ForEach(doc.DocumentNode.SelectNodes("//a"), (freshLink) =>
                    {
                        string freshLinkHref = freshLink.GetAttributeValue("href", ""); // Extract link from tag
                        var freshLinkUri = new Uri(indexUrlUri, freshLinkHref); // Convert relative links to absolute

                        if (indexUrlUri.Host != freshLinkUri.Host) return; // Check for host - filter out links to another domains
                        if (db.Links.Count(link => link.Url == freshLinkUri.AbsoluteUri) > 0) return; // Check existence in db - to avoid duplicates

                        Console.WriteLine("Adding {0}", freshLinkUri.AbsoluteUri);
                        db.Links.Add((new Link {Url = freshLinkUri.AbsoluteUri})); // Save link to database
                    });
                    db.Links.Single(link => link == currentLink).IsParsed = true; // Mark link as parsed
                    db.SaveChanges();
                });
            }
        }

        public static void SaveXmlFile()
        {
            var db = LinkContext.GetInstance();

            if (db.Links.Count() < 50000)
            {
                // Prepare xml document for sitemap
                var sitemap = new XmlDocument();
                var urlset = (XmlElement) sitemap.AppendChild(sitemap.CreateElement("urlset"));
                urlset.SetAttribute("xmlns", "http://www.sitemaps.org/schemas/sitemap/0.9");

                Parallel.ForEach(db.Links, (link) =>
                {
                    var url = (XmlElement) urlset.AppendChild(sitemap.CreateElement("url"));
                    var loc = (XmlElement) url.AppendChild(sitemap.CreateElement("loc"));
                    loc.InnerText = link.Url;
                });

                sitemap.Save(Config.savePath);
            }
            else
            {
                // TODO
                // Here we must to split links and save them to different files
                throw new Exception("Not implemented");
            }
        }
    }
}
