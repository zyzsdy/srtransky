using BetterHttpClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using System.Threading;

namespace srtransky
{
    public delegate void HlsUrlGetEventHandler(object sender, string hlsUrl);
    class Downloader
    {
        const string UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36";

        private string RoomName;
        public string OutputFile { get; }
        public string Proxy { get; }
        public int Threads { get; }
        public int Retries { get; }
        private string BroadcastKey;
        private string BroadcastHost;
        private bool Running;
        private string HlsUrl = null;
        private AutoResetEvent HlsParsedEvent = new AutoResetEvent(false);
        private WebSocket wsclient = null;

        public event HlsUrlGetEventHandler OnHlsUrlGet;

        public Downloader(string name, string outputFile, string proxy, int threads, int retries)
        {
            RoomName = name;
            OutputFile = outputFile;
            Proxy = proxy;
            Threads = threads;
            Retries = retries;
            Running = false;
        }

        public string WaitForHls()
        {
            Task.Run(() =>
            {
                HlsParsedEvent.WaitOne();
            }).Wait();

            return HlsUrl;
        }

        public string HTTPGet(string url)
        {
            HttpClient httpClient;

            if (Proxy == null)
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
                Console.WriteLine("INFO: Use proxy " + Proxy);
            }

            try
            {
                return httpClient.Get(url);
            }
            catch
            {
                throw;
            }
        }

        public void Init()
        {
            try
            {
                var json = ParseRoomData();
                var isLive = json["is_live"].Value<int>();
                BroadcastKey = json["broadcast_key"].ToString();
                BroadcastHost = json["broadcast_host"].ToString();

                if (isLive == 0)
                {
                    Console.WriteLine("Init: Live stopped.");
                }
                else
                {
                    Console.WriteLine("Init: Live broadcast.");
                    StartGetHls(json);
                }

                Console.WriteLine("Init: Broadcast Key: " + BroadcastKey);

                StartWebSocket();
            }
            catch
            {
                return;
            }
        }

        private JObject ParseRoomData()
        {
            string roomHomePage = "https://www.showroom-live.com/" + RoomName;

            string roomApi = "https://www.showroom-live.com/api/room/status?room_url_key=" + RoomName;

            Console.WriteLine("Init: download " + roomHomePage);

            try
            {
                var homePageString = HTTPGet(roomHomePage);
                var re = Regex.Matches(homePageString, @"<script id=""js-live-data"" data-json=""(.+?)""></script>");
                string jsonString = null;
                foreach (Match matched in re)
                {
                    jsonString = matched.Groups[1].ToString();
                    break;
                }
                jsonString = jsonString.Replace("&quot;", "\"");

                var json = JObject.Parse(jsonString);

                return json;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                throw;
            }
        }

        private void StartWebSocket()
        {
            Running = true;
            var wsserverUri = "wss://" + BroadcastHost;

            wsclient = new WebSocket(wsserverUri);
            wsclient.OnMessage += Ws_OnMessage;
            wsclient.OnOpen += (s, e) =>
            {
                // SUB
                WSSend("SUB\t" + BroadcastKey);
            };
            wsclient.OnClose += Wsclient_OnClose;
            wsclient.Connect();
            Console.WriteLine("Init: Connect to websocket server.");

            StartHeartBeat();
        }

        private void Wsclient_OnClose(object sender, CloseEventArgs e)
        {
            Console.WriteLine("Info: WebSocket closed.");
        }

        private void WSSend(string data)
        {
            Console.WriteLine("SEND: " + data);
            wsclient?.Send(data);
        }

        public void Stop()
        {
            Running = false;
            WSSend("QUIT");
            wsclient?.Close();

            HlsParsedEvent.Set();
        }

        private async void StartHeartBeat()
        {
            await Task.Run(async () =>
            {
                while (Running)
                {
                    await Task.Delay(60000);
                    WSSend("PING\tshowroom");
                }
            });
        }

        private void Ws_OnMessage(object sender, MessageEventArgs e)
        {
            Console.WriteLine("RECV: " + e.Data);
            if (e.Data.StartsWith("MSG"))
            {
                try
                {
                    var msg = JObject.Parse(e.Data.Split('\t')[2]);
                    var msgType = msg["t"].Value<int>();

                    switch (msgType)
                    {
                        case 101:
                            Console.WriteLine("INFO: Live stop!");
                            Stop();
                            break;
                        case 104:
                            Console.WriteLine("INFO: Live start!");
                            var json = ParseRoomData();
                            StartGetHls(json);
                            break;
                        default:
                            Console.WriteLine("Type: " + msgType);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
        }

        private async void StartGetHls(JObject json)
        {
            await Task.Run(() =>
            {
                var tempHlsUrl = (from u in (json["streaming_url_list"] as JArray) orderby u["quality"] select u["url"].ToString()).ToList()[0];
                HlsUrl = HlsCheck(tempHlsUrl);

                if(HlsUrl != null)
                {
                    OnHlsUrlGet?.Invoke(this, HlsUrl);
                    HlsParsedEvent.Set();
                }
            });
        }

        private string HlsCheck(string hlsUrl)
        {
            var hlsString = HTTPGet(hlsUrl);
            var re = Regex.Matches(hlsString, @"^.+\.m3u8$", RegexOptions.Multiline);
            string hlsPlaylist = null;
            foreach (Match matched in re)
            {
                hlsPlaylist = matched.ToString();
                break;
            }
            if(hlsPlaylist == null)
            {
                Console.WriteLine("ERROR: Can't parse any m3u8 playlist.");
                return null;
            }
            var newHls = new Uri(new Uri(hlsUrl), hlsPlaylist).ToString();
            return newHls;
        }
    }
}
