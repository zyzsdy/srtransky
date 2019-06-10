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

        [Option('p', "proxy", Required = false, HelpText = "Socks5 proxy address.")]
        public string Proxy { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CmdOptions>(args).WithParsed(o =>
            {
                var downloader = new Downloader(o.RoomName, o.OutputFile, o.Proxy);
                downloader.Init();
                var hls = downloader.WaitForHls();
                if(hls == null)
                {
                    Console.WriteLine("Unable to get hls address.");
                }
                else
                {
                    downloader.Stop();
                }
                
            });
        }
    }
}
