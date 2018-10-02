using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System;
using System.Diagnostics;
using System.Threading;

namespace Console
{
    /// <summary>
    /// Facade to make talking with the Spotify Web API a bit easier
    /// </summary>
    class Facade
    {
        /// <summary>
        /// Log delegate to use when logging messages
        /// </summary>
        /// <param name="type_">Message type</param>
        /// <param name="message_">Log message</param>
        internal delegate void Log(LogType type_, string message_);

        /// <summary>
        /// Function to use for logging
        /// </summary>
        internal static Log LogMessage;
        
        /// <summary>
        /// Fetch the initial token data using the given client id and secrect
        /// </summary>
        /// <param name="clientId_">Spotify ClientId</param>
        /// <param name="clientSecret_">Spotify ClientSecret</param>
        /// <param name="port_">Port to use for the postback from the authorization flow</param>
        /// <returns>The received token data</returns>
        internal static Token GetToken(string clientId_, string clientSecret_, int port_ = 80)
        {
            Token token = null;

            LogMessage(LogType.Notice, "Fetching new refresh token");

            var auth = new AutorizationCodeAuth
            {
                ClientId = clientId_,
                RedirectUri = "http://localhost:" + port_,
                Scope = Scope.UserReadPlaybackState,
            };

            auth.StartHttpServer();
            auth.OnResponseReceivedEvent += (AutorizationCodeAuthResponse response) =>
            {
                token = auth.ExchangeAuthCode(response.Code, clientSecret_);
            };

            LogMessage(LogType.Debug, "Starting authorization process");

            auth.DoAuth();

            var watch = new Stopwatch();
            watch.Start();
            while (token == null && watch.Elapsed < TimeSpan.FromSeconds(30))
            {
                LogMessage(LogType.Debug, "Token not yet received, waiting...");
                Thread.Sleep(1000);
            }

            watch.Stop();
            auth.StopHttpServer();

            if (token == null)
            {
                LogMessage(LogType.Error, "Could not acquire token data");
                throw new Exception("Timeout: Could not acquire a token");
            }

            LogMessage(LogType.Debug, "Done getting token data");

            return token;
        }

        /// <summary>
        /// Refresh the playback data
        /// </summary>
        /// <param name="clientId_">Spotify ClientId</param>
        /// <param name="clientSecrect">Spotify ClientSecret</param>
        /// <param name="refreshToken_">Refresh token previously acquired by <see cref="Facade.GetToken()"/></param>
        /// <returns>The current playback context</returns>
        internal static PlaybackContext Refresh(string clientId_, string clientSecret_, string refreshToken_)
        {
            LogMessage(LogType.Notice, "Getting new access token");
            
            var auth = new AutorizationCodeAuth
            {
                ClientId = clientId_,
            };

            var token = auth.RefreshToken(refreshToken_, clientSecret_);
            var api = new SpotifyWebAPI
            {
                AccessToken = token.AccessToken,
                TokenType = token.TokenType,
            };

            LogMessage(LogType.Debug, "Fetching new playback context");

            var playback = api.GetPlayback();

            if (playback.HasError())
            {
                throw new Exception(playback.Error.Message);
            }

            LogMessage(LogType.Debug, "Done refreshing playback data");

            return playback;
        }
    }
}
