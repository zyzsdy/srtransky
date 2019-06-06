using BetterHttpClient;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace srtransky
{
    class CmdOptions
    {
        [Value(0, HelpText = "Room name in the URL.", Required = true)]
        public string RoomName { get; set; }

        [Option('o', "output", MetaValue = "FILE", Required = false, HelpText = "Output file.", Default = "output.ts")]
        public string OutputFile { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CmdOptions>(args).WithParsed(o =>
            {
                Console.WriteLine(o.RoomName);
                Console.WriteLine(o.OutputFile);

                string proxyAddress = "127.0.0.1:1080";
                HttpClient client = new HttpClient(new Proxy(proxyAddress))
                {
                    UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:41.0) Gecko/20100101 Firefox/41.0"
                };

                string page = client.Get("https://google.com");
                Console.WriteLine(page);
            });
        }
    }
}
