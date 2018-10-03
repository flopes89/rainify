using CommandLine;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using SysConsole = System.Console;

namespace rainify.Console
{
    [Verb("auth", HelpText = "Authorize with SpotifyWebAPI to acquire the initial plugin settings")]
    class Authorize
    {
        [Option('i', "clientId", Required = true, HelpText = "Your Spotify API ClientId")]
        public string ClientId { get; set; }

        [Option('s', "clientSecret", Required = true, HelpText = "Your Spotify API ClientSecret")]
        public string ClientSecret { get; set; }

        [Option('p', "port", Default = 80, HelpText = "The port to use for the Authroization callback (appended to localhost)")]
        public int Port { get; set; }
    }
    
    /// <summary>
    /// Console Utility to talk to the Spotify Web API
    /// </summary>
    class Program
    {
        /// <summary>
        /// Main program
        /// </summary>
        /// <param name="args_">CLI arguments</param>
        /// <returns>1 for errors, 0 otherwise</returns>
        [STAThread]
        static int Main(string[] args_)
        {
            try
            {
                SysConsole.WriteLine("Starting rainify console");
                
                return Parser.Default.ParseArguments<Authorize>(args_)
                    .MapResult(
                        (Authorize opts) => RunAuthorize(opts),
                        errs => 1
                    );
            }
            catch (Exception exc)
            {
                SysConsole.WriteLine("! " + exc.Message);
                SysConsole.WriteLine("! " + exc.StackTrace);
                return 1;
            }
            finally
            {
                SysConsole.WriteLine("Finished");
            }
        }
        
        /// <summary>
        /// Authorize with the Spotify Web API to retrieve a Refresh Token.
        /// This token is then to be used for any subsequent calls to <see cref="RunStatus(Status)"/>
        /// </summary>
        /// <param name="opts_">CLI arguments</param>
        /// <returns>1 for errors, 0 otherwise</returns>
        static int RunAuthorize(Authorize opts_)
        {
            SysConsole.WriteLine("Authorizing with SpotifyWebAPI");

            Token token = null;
            var auth = new AuthorizationCodeAuth(opts_.ClientId, opts_.ClientSecret, "http://localhost:" + opts_.Port, "http://localhost:" + opts_.Port, Scope.UserReadPrivate);
            auth.AuthReceived += (object sender_, AuthorizationCode payload_) =>
            {
                auth.Stop();
                var exchange = auth.ExchangeCode(payload_.Code);
                exchange.Wait(10000);

                if (!exchange.IsCompleted)
                {
                    throw new Exception("Timeout during authorization process!");
                }

                token = exchange.Result;
            };

            SysConsole.WriteLine("Starting authorization process");
            auth.Start();
            auth.OpenBrowser();

            SysConsole.Write("Waiting for authorzation to complete...");
            while (token == null)
            {
                SysConsole.Write(".");
                Task.Delay(500).Wait();
            }

            string settings = "ClientId=" + opts_.ClientId + "\r\n";
            settings += "ClientSecret=" + opts_.ClientSecret + "\r\n";
            settings += "RefreshToken=" + token.RefreshToken + "\r\n";
            Clipboard.SetText(settings);

            SysConsole.WriteLine("");
            SysConsole.WriteLine("Token received. Set the following settings in your web parser parent measure:");
            SysConsole.WriteLine("---");
            SysConsole.WriteLine(settings);
            SysConsole.WriteLine("---");
            SysConsole.WriteLine("(the settings have been copied to your clipboard as well)");

            return 0;
        }
    }
}
