using BetterHttpClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace srtransky
{
    class Downloader
    {
        const string UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36";

        private string RoomName;

        private string OutputFile;

        private string Proxy;

        public Downloader(string name, string outputFile, string proxy)
        {
            RoomName = name;
            OutputFile = outputFile;
            Proxy = proxy;
        }

        public void Init()
        {
            HttpClient httpClient;

            if(Proxy == null)
            {
                httpClient = new HttpClient()
                {
                    UserAgent = UA
                };
            }
            else
            {
                httpClient = new HttpClient(new Proxy(Proxy))
                {
                    UserAgent = UA
                };
                Console.WriteLine("Init: Use proxy " + Proxy);
            }

            string roomHomePage = "https://www.showroom-live.com/" + RoomName;

            Console.WriteLine("Init: download " + roomHomePage);

            var homePageString = httpClient.Get(roomHomePage);

            var re = Regex.Matches(homePageString, "<script id=\"js-live-data\" data-json=\"(.+?)\"></script>");
            string jsonString = null;
            foreach(Match matched in re)
            {
                jsonString = matched.Groups[1].ToString();
                break;
            }
            jsonString = jsonString.Replace("&quot;", "\"");

            var json = JObject.Parse(jsonString);
            Console.WriteLine(json.ToString());
        }
    }
}
