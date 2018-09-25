using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System;
using System.Diagnostics;
using System.Threading;
using static Rainmeter.Api;

namespace rainify.Console
{
    public class Facade
    {
        /// <summary>
        /// The current playback context
        /// </summary>
        public PlaybackContext Playback
        {
            get
            {
                Log(LogType.Debug, "Accessing playback data");

                if (_playback == null)
                {
                    Refresh();
                }

                return _playback;
            }

            protected set
            {
                Log(LogType.Debug, "Saving new playback data");
                _playback = value;
            }
        }
        protected PlaybackContext _playback;
        
        /// <summary>
        /// Spotify Client ID
        /// </summary>
        protected string _clientId { get; set; }

        /// <summary>
        /// Spotify Client Secret
        /// </summary>
        protected string _clientSecret { get; set; }

        /// <summary>
        /// Access Token from the Spotify API
        /// </summary>
        protected Token _token { get; set; }

        /// <summary>
        /// Refresh Token from the Spotify API
        /// </summary>
        protected string _refreshToken { get; set; }

        /// <summary>
        /// Spotify API reference
        /// </summary>
        protected SpotifyWebAPI _api { get; set; }

        /// <summary>
        /// Create a new facade to the spotify web api
        /// </summary>
        /// <param name="clientId">Spotify ClientId</param>
        /// <param name="clientSecrect">Spotify ClientSecret</param>
        /// <param name="refreshToken">Refresh token previously acquired by <see cref="Facade.GetToken()"/></param>
        public Facade(string clientId, string clientSecrect, string refreshToken)
        {
            _clientId = clientId;
            _clientSecret = clientSecrect;
            _refreshToken = refreshToken;
        }

        /// <summary>
        /// Fetch the initial token data using the given client id and secrect
        /// </summary>
        /// <param name="clientId">Spotify ClientId</param>
        /// <param name="clientSecret">Spotify ClientSecret</param>
        /// <param name="port">Port to use for the postback from the authorization flow</param>
        /// <returns>The received token data</returns>
        public static Token GetToken(string clientId, string clientSecret, int port = 80)
        {
            Token token = null;

            Log(LogType.Notice, "Getting new token data");

            var auth = new AutorizationCodeAuth()
            {
                ClientId = clientId,
                RedirectUri = "http://localhost:" + port,
                Scope = Scope.UserReadPlaybackState,
            };

            auth.StartHttpServer();
            auth.OnResponseReceivedEvent += (AutorizationCodeAuthResponse response) =>
            {
                token = auth.ExchangeAuthCode(response.Code, clientSecret);
            };

            Log(LogType.Debug, "Starting authorization process");

            auth.DoAuth();

            var watch = new Stopwatch();
            watch.Start();
            while (token == null && watch.Elapsed < TimeSpan.FromSeconds(30))
            {
                Log(LogType.Debug, "Token not yet received, waiting...");
                Thread.Sleep(1000);
            }

            watch.Stop();
            auth.StopHttpServer();

            if (token == null)
            {
                Log(LogType.Error, "Could not acquire token data");
                throw new Exception("Timeout: Could not acquire a token");
            }

            Log(LogType.Debug, "Done getting token data");

            return token;
        }

        /// <summary>
        /// Refresh the playback data
        /// </summary>
        public void Refresh()
        {
            Log(LogType.Notice, "Refreshing playback data");
            CheckToken();

            Playback = _api.GetPlayback();

            if (Playback.HasError())
            {
                Log(LogType.Error, Playback.Error.Message);
                return;
            }

            Log(LogType.Debug, "Done refreshing playback data");
        }

        /// <summary>
        /// Check if the current token data is valid and, if not, try to refresh them
        /// with the saved refresh token
        /// </summary>
        protected void CheckToken()
        {
            if (_token == null || _token.IsExpired())
            {
                Log(LogType.Notice, "Access token is expired or has never been set, trying to refresh token");

                var auth = new AutorizationCodeAuth()
                {
                    ClientId = _clientId,
                };

                _token = auth.RefreshToken(_refreshToken, _clientSecret);
                _api = new SpotifyWebAPI()
                {
                    AccessToken = _token.AccessToken,
                    TokenType = _token.TokenType,
                };

                Log(LogType.Debug, "Done refreshing token");
            }
        }

        /// <summary>
        /// Write a log message with the attached log writeres
        /// </summary>
        /// <param name="type">Log level</param>
        /// <param name="message">Message</param>
        /// <param name="args">Arguments to use when formatting the message</param>
        protected static void Log(LogType type, string message, params string[] args)
        {
            System.Console.WriteLine(type.ToString(), message, args);
        }
    }
}
