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

        [Option('t', "threads", Required = false, HelpText = "Threads number of Minyami used to download video.", Default = 20)]
        public int Thread { get; set; }

        [Option('r', "retries", Required = false, HelpText = "Retries count of Minyami", Default = 999)]
        public int Retries { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CmdOptions>(args).WithParsed(o =>
            {
                var downloader = new Downloader(o.RoomName, o.OutputFile, o.Proxy, o.Thread, o.Retries);
                downloader.OnHlsUrlGet += Downloader_OnHlsUrlGet;
                downloader.Init();
                var hls = downloader.WaitForHls();
                if(hls == null)
                {
                    Console.WriteLine("Unable to get hls address.");
                }
                else
                {
                    downloader.Stop();
                    Console.WriteLine("FIND HLS! Program stop.");
                }
                
            });
        }

        private static async void Downloader_OnHlsUrlGet(object sender, string hlsUrl)
        {
            await Task.Run(() =>
            {
                Console.WriteLine("INFO: HLS URL: " + hlsUrl);
                var downloader = sender as Downloader;
                Console.WriteLine("Call Minyami...");
                var minyamiCmd = $"-d \"{hlsUrl}\" --output \"{downloader.OutputFile}\" --live --threads {downloader.Threads} --retries {downloader.Retries}";
                if (downloader.Proxy != null)
                {
                    minyamiCmd += $" --proxy \"{downloader.Proxy}\"";
                }
                Console.WriteLine("INFO: Use command: minyami " + minyamiCmd);
                try
                {
                    System.Diagnostics.Process.Start("minyami.cmd", minyamiCmd);
                }
                catch(System.ComponentModel.Win32Exception e)
                {
                    Console.WriteLine("ERROR: Emmmm? Minyami? Okite!!!!!");
                }
                
            });
            
        }
    }
}
