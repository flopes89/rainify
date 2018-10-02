using CommandLine;
using SpotifyAPI.Web.Models;
using System;
using System.Collections;
using System.Collections.Generic;
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
        static int RunAuthorize(Authorize opts_)
        {
            Log(LogType.Debug, "Authorizing with SpotifyWebAPI");
            
            var token = Facade.GetToken(opts_.ClientId, opts_.ClientSecret, opts_.Port);
            
            string settings = "ClientId=" + opts_.ClientId + "\r\n";
            settings += "ClientSecret=" + opts_.ClientSecret + "\r\n";
            settings += "RefreshToken=" + token.RefreshToken + "\r\n";
            Clipboard.SetText(settings);

            System.Console.WriteLine("Token received. Set the following settings in your web parser parent measure:");
            System.Console.WriteLine("---");
            System.Console.WriteLine(settings);
            System.Console.WriteLine("---");
            System.Console.WriteLine("(the settings have been copied to your clipboard as well)");

            return 0;
        }
        
        /// <summary>
        /// Log a message to the console
        /// </summary>
        /// <param name="type_">Log level</param>
        /// <param name="message_">Message to log</param>
        static void Log(LogType type_, string message_)
        {
            System.Console.WriteLine(type_.ToString().PadLeft(7, ' ') + ": " + message_);
        }

        /// <summary>
        /// Dumps out all properties of an objects in Key=Value style
        /// Recurses into any property that is not a serializable type
        /// </summary>
        /// <param name="obj_">The object to dump</param>
        /// <param name="prefix_">Prefix for the name of the property</param>
        static void Dump(object obj_, string prefix_ = "")
        {
            foreach (var property in obj_.GetType().GetProperties())
            {
                string name = property.Name;
                object value = property.GetValue(obj_, null);

                Log(LogType.Debug, "Dumping property [" + name + "] with string value [" + value + "]");

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
                    Log(LogType.Debug, "Dumping simple object [" + prefix_ + name + "]");
                    Dump(value, prefix_ + name + ".");
                }
                else if (property.PropertyType == typeof(Dictionary<string, string>))
                {
                    Log(LogType.Debug, "Dumping dictionary [" + prefix_ + name + "]");
                    foreach (var row in (Dictionary<string, string>)value)
                    {
                        System.Console.WriteLine(prefix_ + name + "." + row.Key + "===" + row.Value);
                    }
                }
                //else if (property.PropertyType == typeof(List<string>))
                //{
                //    Log(LogType.Debug, "Dumping string list [" + prefix_ + name + "]");
                //    uint index = 0;
                //    foreach (var row in (List<string>)value)
                //    {
                //        System.Console.WriteLine(prefix_ + name + "." + index++ + "===" + row);
                //    }
                //}
                else if (property.PropertyType == typeof(List<SimpleArtist>) ||
                    property.PropertyType == typeof(List<Image>))
                {
                    Log(LogType.Debug, "Dumping object list [" + prefix_ + name + "]");
                    uint index = 0;
                    foreach (var row in (IEnumerable)value)
                    {
                        Dump(row, prefix_ + name + "." + index++ + ".");
                    }
                }
                else
                {
                    Log(LogType.Debug, "Dumping property [" + prefix_ + name + "]");
                    System.Console.WriteLine(prefix_ + name + "===" + value);
                }
            }
        }
    }
}
