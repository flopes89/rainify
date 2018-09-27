using CommandLine;
using SpotifyAPI.Web.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using static Rainmeter.Api;

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
        /// Log verbose messages or not
        /// </summary>
        static bool _verbose { get; set; } = false;

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
                Log(LogType.Debug, "Starting rainify console");

                Facade.LogMessage = Log;

                return Parser.Default.ParseArguments<Authorize, Status>(args_)
                    .MapResult(
                        (Authorize opts) => RunAuthorize(opts),
                        (Status opts) => RunStatus(opts),
                        errs => 1
                    );
            }
            catch (Exception exc)
            {
                Log(LogType.Error, exc.Message);
                Log(LogType.Error, exc.StackTrace);
                return 1;
            }
            finally
            {
                Log(LogType.Debug, "Finished");
            }
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
            Log(LogType.Debug, "Fetching current playback status");

            var playback = Facade.Refresh(opts_.ClientId, opts_.ClientSecret, opts_.RefreshToken);
            Dump(playback);

            return 0;
        }

        /// <summary>
        /// Authorize with the Spotify Web API to retrieve a Refresh Token.
        /// This token is then to be used for any subsequent calls to <see cref="RunStatus(Status)"/>
        /// </summary>
        /// <param name="opts_">CLI arguments</param>
        /// <returns>1 for errors, 0 otherwise</returns>
        static int RunAuthorize(Authorize opts)
        {
            Log(LogType.Debug, "Authorizing with SpotifyWebAPI");
            
            var token = Facade.GetToken(opts.ClientId, opts.ClientSecret, opts.Port);
            
            string settings = "ClientId=" + opts.ClientId + "\r\n";
            settings += "ClientSecret=" + opts.ClientSecret + "\r\n";
            settings += "RefreshToken=" + token.RefreshToken + "\r\n";
            Clipboard.SetText(settings);

            System.Console.WriteLine("Token received. Set the following settings in the parent measure:");
            System.Console.WriteLine("---");
            System.Console.WriteLine(settings);
            System.Console.WriteLine("---");
            System.Console.WriteLine("(the settings have been copied to your clipboard as well)");

            return 0;
        }
        
        /// <summary>
        /// Log a message to the console
        /// </summary>
        /// <param name="type">Log level</param>
        /// <param name="message">Message to log</param>
        static void Log(LogType type, string message)
        {
            System.Console.WriteLine(type.ToString().PadLeft(7, ' ') + ": " + message);
        }

        /// <summary>
        /// Dumps out all properties of an objects in Key=Value style
        /// Recurses into any property that is not a serializable type
        /// </summary>
        /// <param name="obj">The object to dump</param>
        static void Dump(object obj, string prefix = "")
        {
            foreach (var property in obj.GetType().GetProperties())
            {
                string name = property.Name;
                object value = property.GetValue(obj, null);
                
                if (value == null)
                {
                    continue;
                }

                if (property.PropertyType == typeof(Device) ||
                    property.PropertyType == typeof(Context) ||
                    property.PropertyType == typeof(FullTrack) ||
                    property.PropertyType == typeof(SimpleAlbum) ||
                    property.PropertyType == typeof(LinkedFrom)
                    )
                {
                    Dump(value, prefix + name + ".");
                }
                else if (property.PropertyType == typeof(Dictionary<string, string>))
                {
                    foreach (var row in (Dictionary<string, string>)value)
                    {
                        System.Console.WriteLine(prefix + name + "." + row.Key + "===" + row.Value);
                    }
                }
                else if (property.PropertyType == typeof(List<string>))
                {
                    uint index = 0;
                    foreach (var row in (List<string>)value)
                    {
                        System.Console.WriteLine(prefix + name + "." + index++ + "===" + row);
                    }
                }
                else if (property.PropertyType == typeof(List<SimpleArtist>) ||
                    property.PropertyType == typeof(List<Image>))
                {
                    uint index = 0;
                    foreach (var row in (IEnumerable)value)
                    {
                        Dump(row, prefix + name + "." + index++ + ".");
                    }
                }
                else
                {
                    System.Console.WriteLine(prefix + name + "===" + value);
                }
            }
        }
    }
}
