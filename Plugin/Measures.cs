﻿using Rainmeter;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using static Rainmeter.Api;

namespace rainify.Plugin
{
    /// <summary>
    /// Log delegate to use when logging messages
    /// </summary>
    /// <param name="type_">Message type</param>
    /// <param name="message_">Log message</param>
    delegate void Log(LogType type_, string message_);

    /// <summary>
    /// Base class for parents and child measures
    /// </summary>
    class BaseMeasure
    {
        /// <summary>
        /// Message function to use for logging
        /// </summary>
        protected Log _log { get; set; }

        /// <summary>
        /// Base Measure instance
        /// </summary>
        /// <param name="log_">Function to use for logging</param>
        protected BaseMeasure(Log log_)
        {
            _log = log_;
        }

        /// <summary>
        /// <see cref="Plugin.Reload(IntPtr, IntPtr, ref double)"/>
        /// </summary>
        /// <param name="api_"><see cref="Plugin.Reload(IntPtr, IntPtr, ref double)"/></param>
        /// <param name="max_"><see cref="Plugin.Reload(IntPtr, IntPtr, ref double)"/></param>
        internal virtual void Reload(Api api_, ref double max_)
        {
        }

        /// <summary>
        /// Dispose of the measures allocated data
        /// </summary>
        internal virtual void Dispose()
        {
        }

        /// <summary>
        /// <see cref="Plugin.Update(IntPtr)"/>
        /// </summary>
        /// <returns><see cref="Plugin.Update(IntPtr)"/></returns>
        internal virtual double Update()
        {
            return 0.0;
        }

        /// <summary>
        /// <see cref="Plugin.GetString(IntPtr)"/>
        /// </summary>
        /// <returns><see cref="Plugin.GetString(IntPtr)"/></returns>
        internal virtual string GetString()
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// The parent that holds all fields returned by the SpotifyApi
    /// Fields are accessed by child measures by the field name
    /// </summary>
    class ParentMeasure : BaseMeasure
    {
        /// <summary>
        /// List of all parent measures is used by the child measures to find their parent
        /// </summary>
        internal static IList<ParentMeasure> Parents = new List<ParentMeasure>();

        /// <summary>
        /// Current playback context returned from the Spotify Web API
        /// </summary>
        internal PlaybackContext Playback { get; private set; }

        /// <summary>
        /// Name of the parent measure (compared with the ParentName in a child measure to
        /// find the correct parent measure)
        /// </summary>
        internal string Name { get; set; }

        /// <summary>
        /// Skin of the parent measure (compared with the skin of a child measure to
        /// find the correct parent measure)
        /// </summary>
        internal IntPtr Skin { get; set; }

        /// <summary>
        /// Spotify Client ID
        /// </summary>
        string _clientId { get; set; }

        /// <summary>
        /// Spotify Client Secret
        /// </summary>
        string _clientSecret { get; set; }

        /// <summary>
        /// Refresh Token for Spotify API
        /// </summary>
        string _refreshToken { get; set; }
        
        /// <summary>
        /// Access Token for Spotify API
        /// </summary>
        Token _accessToken { get; set; }

        /// <summary>
        /// Add this parent measure to the list of parent measures
        /// </summary>
        internal ParentMeasure(Log log_) : base(log_)
        {
            Parents.Add(this);
        }

        /// <summary>
        /// <see cref="BaseMeasure.Reload(Api, ref double)"/>
        /// </summary>
        /// <param name="api_"><see cref="BaseMeasure.Reload(Api, ref double)"/></param>
        /// <param name="max_"><see cref="BaseMeasure.Reload(Api, ref double)"/></param>
        internal override void Reload(Api api_, ref double max_)
        {
            base.Reload(api_, ref max_);

            Name = api_.GetMeasureName();
            Skin = api_.GetSkin();

            _log(LogType.Debug, "Reloading parent measure [" + Name + "] in Skin [" + Skin + "]");

            _clientId = api_.ReadString("ClientId", string.Empty);
            _clientSecret = api_.ReadString("ClientSecret", string.Empty);
            _refreshToken = api_.ReadString("RefreshToken", string.Empty);

            if (string.IsNullOrWhiteSpace(_clientId) ||
                string.IsNullOrWhiteSpace(_clientSecret) ||
                string.IsNullOrWhiteSpace(_refreshToken))
            {
                _log(LogType.Error, "ClientId, ClientSecret and RefreshToken must be set. Use the console to generate them");
                return;
            }

            if (_accessToken.IsExpired())
            {
                var auth = new AuthorizationCodeAuth(_clientId, _clientSecret, string.Empty, string.Empty);
                var refresh = auth.RefreshToken(_refreshToken);

                if (!refresh.Wait(10000))
                {
                    _log(LogType.Error, "Timeout when refreshing token");
                    return;
                }

                _accessToken = refresh.Result;
            }

            var api = new SpotifyWebAPI
            {
                AccessToken = _accessToken.AccessToken,
                UseAuth = true,
                TokenType = _accessToken.TokenType,
            };

            Playback = api.GetPlayback();

            _log(LogType.Debug, "Reloading done");
        }

        /// <summary>
        /// <see cref="BaseMeasure.Update"/>
        /// </summary>
        /// <returns><see cref="BaseMeasure.Update"/></returns>
        internal override double Update()
        {
            return base.Update();
        }

        /// <summary>
        /// Remove this parent measure from the list of parent measures
        /// </summary>
        internal override void Dispose()
        {
            Parents.Remove(this);
        }
    }

    /// <summary>
    /// The child measure that will return the value of a single field saved in the associated parent measure
    /// </summary>
    class ChildMeasure : BaseMeasure
    {
        /// <summary>
        /// The field that is used by the measure
        /// Fields itself are held by the ParentMeasure in a dictionary
        /// </summary>
        string _field { get; set; } = string.Empty;

        /// <summary>
        /// The parent measure of this child
        /// </summary>
        ParentMeasure _parentMeasure { get; set; } = null;

        /// <summary>
        /// Child Measure instance
        /// </summary>
        /// <param name="log_">Function to use for logging</param>
        internal ChildMeasure(Log log_) : base(log_)
        {

        }

        /// <summary>
        /// <see cref="BaseMeasure.Reload(Api, ref double)"/>
        /// </summary>
        /// <param name="api_"><see cref="BaseMeasure.Reload(Api, ref double)"/></param>
        /// <param name="max_"><see cref="BaseMeasure.Reload(Api, ref double)"/></param>
        internal override void Reload(Api api_, ref double max_)
        {
            base.Reload(api_, ref max_);

            _field = api_.ReadString("Field", string.Empty);

            var parentName = api_.ReadString("ParentName", string.Empty);
            var skin = api_.GetSkin();

            _log(LogType.Debug, "Reloading child measure [" + api_.GetMeasureName() + "] in Skin [" + skin + "]");
            _parentMeasure = null;
            foreach (ParentMeasure parent in ParentMeasure.Parents)
            {
                if (parent.Skin.Equals(skin) && parent.Name.Equals(parentName))
                {
                    _log(LogType.Debug, "Found parent measure for [" + api_.GetMeasureName() + "] in Skin [" + parent.Skin + "]: [" + parentName + "]");
                    _parentMeasure = parent;
                }
            }

            if (_parentMeasure == null)
            {
                _log(LogType.Error, "Parent [" + parentName + "] not found in Skin [" + skin + "]");
            }

            _log(LogType.Debug, "Reloading done");
        }

        /// <summary>
        /// Get the double value of a single field saved in the parent measure
        /// </summary>
        /// <returns>Double value of the field</returns>
        internal override double Update()
        {
            if (_parentMeasure == null)
            {
                return base.Update();
            }

            var stringValue = GetFieldValue(_field);
            if (double.TryParse(stringValue, out double value))
            {
                return value;
            }
            
            return 0;

        }

        /// <summary>
        /// Get the string value of a single field saved in the parent measure
        /// </summary>
        /// <returns>String value of the field</returns>
        internal override string GetString()
        {
            if (_parentMeasure == null)
            {
                return base.GetString();
            }

            return GetFieldValue(_field);
        }

        /// <summary>
        /// Gets the string value of the given property "path" (e.g. Item.Artist.Name) of the
        /// parents measures Playback context property
        /// </summary>
        /// <param name="property_">The property path</param>
        /// <returns>String value of that property</returns>
        string GetFieldValue(string property_)
        {
            object currentObject = _parentMeasure.Playback;

            foreach (var propName in property_.Split('.'))
            {
                var prop = currentObject.GetType().GetProperty(propName);

                if (prop == null)
                {
                    return $"Unknown property [{propName}]";
                }

                currentObject = prop.GetValue(currentObject);
            }

            return currentObject.ToString();
        }
    }
}
