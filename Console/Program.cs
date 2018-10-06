using CommandLine;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using SysConsole = System.Console;

namespace rainify.Console
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
                
                return Parser.Default.ParseArguments<Authorize, Status>(args_)
                    .MapResult(
                        (Authorize opts) => RunAuthorize(opts),
                        (Status opts) => RunStatus(opts),
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
#if DEBUG
                SysConsole.Read();
#endif
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

        /// <summary>
        /// Fetch the current playback status from the Spotify Web API.
        /// Writes out the playback status to a flat keyvale representations as FieldName=FieldValue\r\n
        /// so it can be easily parsed
        /// </summary>
        /// <param name="opts_">CLI arguments</param>
        /// <returns>1 for errors, 0 otherwise</returns>
        static int RunStatus(Status opts_)
        {
            SysConsole.WriteLine("Fetching current playback status");

            var auth = new AuthorizationCodeAuth(opts_.ClientId, opts_.ClientSecret, string.Empty, string.Empty);
            var refresh = auth.RefreshToken(opts_.RefreshToken);

            if (!refresh.Wait(10000))
            {
                SysConsole.WriteLine("Timeout when refreshing token");
                return 1;
            }

            var token = refresh.Result;
            var api = new SpotifyWebAPI
            {
                AccessToken = token.AccessToken,
                TokenType = token.TokenType,
            };

            Dump(api.GetPlayback());

            return 0;
        }
        /// <summary>
        /// Dumps out all properties of an objects in Key=Value style
        /// Recurses into any property that is not a serializable type
        /// </summary>
        /// <param name="obj_">The object to dump</param>
        /// <param name="prefix_">Prefix for the name of the property</param>
        static void Dump(object obj_, string prefix_ = "")
        {
            if (obj_.GetType() == typeof(string) || obj_.GetType().IsValueType)
            {
                SysConsole.WriteLine($"{prefix_.Substring(0, prefix_.Length-1)}={obj_.ToString()}");
                return;
            }

            foreach (var property in obj_.GetType().GetProperties())
            {
                string name = property.Name;
                object value = property.GetValue(obj_);

                if (value == null)
                {
                    continue;
                }

                if (typeof(IDictionary).IsAssignableFrom(property.PropertyType))
                {
                    foreach (DictionaryEntry row in (IDictionary)value)
                    {
                        Dump(row.Value, $"{prefix_}{name}.{row.Key}.");
                    }
                }
                else if (typeof(IList).IsAssignableFrom(property.PropertyType))
                {
                    uint index = 0;
                    foreach (var row in (IEnumerable)value)
                    {
                        Dump(row, $"{prefix_}{name}.{index++}.");
                    }
                }
                else if (property.PropertyType == typeof(string) || property.PropertyType.IsValueType)
                {
                    SysConsole.WriteLine($"{prefix_}{name}={value}");
                }
                else
                {
                    Dump(value, $"{prefix_}{name}.");
                }
            }
        }
    }
}
