using System;
using System.IO;

namespace topographer
{
    public static class Config
    {
        public static string targetHost = "http://example.com/";
        public static string savePath = Environment.CurrentDirectory + Path.DirectorySeparatorChar + "sitemap.xml";
        public static bool verbose = false;
    }
}
