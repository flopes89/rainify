using CommandLine;
using Newtonsoft.Json;
using RainmeterSpotifyPlugin;
using System;
using System.Windows.Forms;

namespace Console
{
    class CommonOptions
    {
        [Option('i', "clientId", Required = true, HelpText = "Your Spotify API ClientId")]
        public string ClientId { get; set; }

        [Option('s', "clientSecret", Required = true, HelpText = "Your Spotify API ClientSecret")]
        public string ClientSecret { get; set; }
    }

    [Verb("auth", HelpText = "Authorize with SpotifyWebAPI to acquire the initial plugin settings")]
    class Authorize : CommonOptions
    {
        [Option('p', "port", Default = 80, HelpText = "The port to use for the Authroization callback (appended to localhost)")]
        public int Port { get; set; }
    }

    [Verb("status", HelpText = "Show the current playback status to verfiy the plugin is working as expected")]
    class Status : CommonOptions
    {
        [Option('t', "token", Required = true, HelpText = "The refresh token to use")]
        public string RefreshToken { get; set; }
    }

    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Facade.WriteLogMessage += (string type, string message, string[] logArgs) =>
                {
                    Log(string.Format("[" + type + "] " + message, logArgs));
                };

                Parser.Default.ParseArguments<Authorize, Status>(args)
                    .MapResult(
                        (Authorize opts) => RunAuthorize(opts),
                        (Status opts) => RunStatus(opts),
                        errs => 1
                    );
            }
            catch (Exception exc)
            {
                Log(exc.Message);
                Log(exc.StackTrace);
            }
            finally
            {
                Log("Finished");
#if DEBUG
                System.Console.Read();
#endif
            }
        }
        
        static int RunStatus(Status opts)
        {
            Log("Fetching current playback status");

            var facade = new Facade(opts.ClientId, opts.ClientSecret, opts.RefreshToken);
            Dump(facade.Playback);

            return 0;
        }

        static int RunAuthorize(Authorize opts)
        {
            Log("Authorizing with SpotifyWebAPI");
            
            var token = Facade.GetToken(opts.ClientId, opts.ClientSecret, opts.Port);
            
            string settings = "ClientId=" + opts.ClientId + "\r\n";
            settings += "ClientSecret=" + opts.ClientSecret + "\r\n";
            settings += "RefreshToken=" + token.RefreshToken + "\r\n";
            Clipboard.SetText(settings);

            Log("Token received. Set the following settings in the parent measure:");
            Log("---");
            Log(settings);
            Log("---");
            Log("(the settings have been copied to your clipboard as well)");

            return 0;
        }

        static void Log(string message)
        {
            System.Console.WriteLine(message);
        }

        static void Dump(object obj)
        {
            Log(JsonConvert.SerializeObject(obj, Formatting.Indented));
        }
    }
}
