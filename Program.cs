using System;
using System.Collections.Generic;
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
            var app = new CommandLineApplication();
            app.HelpOption("-h|--help");
            var optionUrl = app.Option<string>("-u|--url <URL>", "What we will parse? Full Url.", CommandOptionType.SingleValue);
            var optionSavePath = app.Option<string>("-s|--save-to <PATH>", "Where we will save sitemap-file? Path to folder.", CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                if (optionUrl.HasValue() || !optionSavePath.HasValue())
                {
                    Console.WriteLine(app.GetHelpText());
                    Environment.Exit(1);
                }

                Config.savePath = optionSavePath.Value();
                Config.targetHost = optionUrl.Value();
                GenerateSitemap();
            });

            app.Execute(args);
        }

        public static void GenerateSitemap()
        {
            var indexUrlUri = new Uri(Config.targetHost);
            var web = new HtmlWeb();

            // Here we will save links that must be parsed
            var linksOrder = new List<string> {Config.targetHost};
            // Here we will save links that already parsed
            var parsedLinks = new List<string>();

            // Prepare xml document for sitemap
            var sitemap = new XmlDocument();
            var urlset = (XmlElement) sitemap.AppendChild(sitemap.CreateElement("urlset"));
            urlset.SetAttribute("xmlns", "http://www.sitemaps.org/schemas/sitemap/0.9");

            // Sitemap can't contain over 50000 records
            while (linksOrder.Count > 0 && parsedLinks.Count < 50000)
            {
                Console.WriteLine("Parsing {0}", linksOrder[0]);

                // Load url from order
                var doc = web.Load(linksOrder[0]);

                // Grab all links from loaded page
                var freshLinks = doc.DocumentNode.SelectNodes("//a");

                // Put all grabbed links in order
                Parallel.ForEach(freshLinks, (freshLink) =>
                {
                    string freshLinkHref = freshLink.GetAttributeValue("href", "");
                    var freshLinkUri = new Uri(indexUrlUri, freshLinkHref);
                    // Check for host
                    if (indexUrlUri.Host != freshLinkUri.Host) return;
                    // Check existence in list
                    if (linksOrder.FindIndex(link => link == freshLinkUri.AbsoluteUri) != -1) return;
                    if (parsedLinks.FindIndex(link => link == freshLinkUri.AbsoluteUri) != -1) return;

                    Console.WriteLine("Adding {0} to order", freshLinkUri.AbsoluteUri);
                    linksOrder.Add(freshLinkUri.AbsoluteUri);
                });

                // Add link to list of parsed and remove from order
                var url = (XmlElement) urlset.AppendChild(sitemap.CreateElement("url"));
                var loc = (XmlElement) url.AppendChild(sitemap.CreateElement("loc"));
                loc.InnerText = linksOrder[0];
                parsedLinks.Add(linksOrder[0]);
                linksOrder.RemoveAt(0);
            }

            sitemap.Save(Config.savePath);
        }
    }
}