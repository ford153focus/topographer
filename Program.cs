using System;
using System.Collections.Generic;
using System.Xml;
using HtmlAgilityPack;

namespace topographer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter host. \"http://example.com/\" for example.");
            String indexUrl = Console.ReadLine();
            // String indexUrl = "https://fittorg.ru/";
            Uri indexUrlUri = new Uri(indexUrl);

            var web = new HtmlWeb();

            var linksOrder = new List<String>();
            var parsedLinks = new List<String>();

            XmlDocument sitemap = new XmlDocument();
            XmlElement urlset = (XmlElement)sitemap.AppendChild(sitemap.CreateElement("urlset"));
            urlset.SetAttribute("xmlns", "http://www.sitemaps.org/schemas/sitemap/0.9");

            linksOrder.Add(indexUrl);

            while (linksOrder.Count > 0 && parsedLinks.Count < 50000)
            {
                Console.WriteLine("Parsing {0}", linksOrder[0]);

                var doc = web.Load(linksOrder[0]);
                HtmlNodeCollection freshLinks = doc.DocumentNode.SelectNodes("//a");

                foreach (var freshLink in freshLinks)
                {
                    var freshLinkHref = freshLink.GetAttributeValue("href", "");
                    var freshLinkUri = new Uri(indexUrlUri, freshLinkHref);
                    // Check for host
                    if (indexUrlUri.Host == freshLinkUri.Host)
                    {
                        // Check existence in list
                        if (linksOrder.FindIndex(link => link == freshLinkUri.AbsoluteUri) == -1 && 
                            parsedLinks.FindIndex(link => link == freshLinkUri.AbsoluteUri) == -1)
                        {
                            Console.WriteLine("Adding {0} to order", freshLinkUri.AbsoluteUri);
                            linksOrder.Add(freshLinkUri.AbsoluteUri);
                        }
                    }
                }
                // Add to parsed and remove from order
                parsedLinks.Add(linksOrder[0]);

                XmlElement url = (XmlElement)urlset.AppendChild(sitemap.CreateElement("url"));
                XmlElement loc = (XmlElement)url.AppendChild(sitemap.CreateElement("loc"));
                loc.InnerText = linksOrder[0];

                linksOrder.RemoveAt(0);
            }

            sitemap.Save(Environment.CurrentDirectory+"\\sitemap.xml");
        }
    }
}
