using Rainmeter;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
        /// <see cref="Plugin.Reload"/>
        /// </summary>
        /// <param name="api_"><see cref="Plugin.Reload"/></param>
        /// <param name="max_"><see cref="Plugin.Reload"/></param>
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
        /// <see cref="Plugin.Update"/>
        /// </summary>
        /// <returns><see cref="Plugin.Update"/></returns>
        internal virtual double Update()
        {
            return 0.0;
        }

        /// <summary>
        /// <see cref="Plugin.GetString"/>
        /// </summary>
        /// <returns><see cref="Plugin.GetString"/></returns>
        internal virtual string GetString()
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// The parent that holds the current playback context returned by the SpotifyApi
    /// And handles refreshing access tokens when necessary
    /// Fields are accessed by child measures by the field name
    /// </summary>
    class ParentMeasure : BaseMeasure
    {
        /// <summary>
        /// List of all parent measures is used by the child measures to find their parent
        /// </summary>
        internal static IList<ParentMeasure> Parents { get; private set; } = new List<ParentMeasure>();

        /// <summary>
        /// Current playback context returned from the Spotify Web API
        /// </summary>
        internal PlaybackContext Playback { get; private set; }

        /// <summary>
        /// Name of the parent measure (compared with the ParentName in a child measure to
        /// find the correct parent measure)
        /// </summary>
        internal string Name { get; set; } = string.Empty;

        /// <summary>
        /// Skin of the parent measure (compared with the skin of a child measure to
        /// find the correct parent measure)
        /// </summary>
        internal IntPtr Skin { get; set; } = IntPtr.Zero;

        /// <summary>
        /// Spotify Client ID
        /// </summary>
        string _clientId { get; set; } = string.Empty;

        /// <summary>
        /// Spotify Client Secret
        /// </summary>
        string _clientSecret { get; set; } = string.Empty;

        /// <summary>
        /// Refresh Token for Spotify API
        /// </summary>
        string _refreshToken { get; set; } = string.Empty;

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
        /// <see cref="BaseMeasure.Reload"/>
        /// </summary>
        /// <param name="api_"><see cref="BaseMeasure.Reload"/></param>
        /// <param name="max_"><see cref="BaseMeasure.Reload"/></param>
        internal override void Reload(Api api_, ref double max_)
        {
            base.Reload(api_, ref max_);

            Name = api_.GetMeasureName();
            Skin = api_.GetSkin();
            
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

            Update();
        }

        /// <summary>
        /// <see cref="BaseMeasure.Update"/>
        /// </summary>
        /// <returns><see cref="BaseMeasure.Update"/></returns>
        internal override double Update()
        {
            if (_accessToken == null || _accessToken.IsExpired())
            {
                var auth = new AuthorizationCodeAuth(_clientId, _clientSecret, string.Empty, string.Empty);
                var refresh = auth.RefreshToken(_refreshToken);

                if (!refresh.Wait(10000))
                {
                    _log(LogType.Error, "Timeout when refreshing token");
                    return 0;
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
        /// <see cref="BaseMeasure.Reload"/>
        /// </summary>
        /// <param name="api_"><see cref="BaseMeasure.Reload"/></param>
        /// <param name="max_"><see cref="BaseMeasure.Reload"/></param>
        internal override void Reload(Api api_, ref double max_)
        {
            base.Reload(api_, ref max_);

            _field = api_.ReadString("Field", string.Empty);

            var parentName = api_.ReadString("ParentName", string.Empty);
            var skin = api_.GetSkin();
            
            _parentMeasure = null;
            foreach (ParentMeasure parent in ParentMeasure.Parents)
            {
                if (parent.Skin.Equals(skin) && parent.Name.Equals(parentName))
                {
                    _parentMeasure = parent;
                }
            }

            if (_parentMeasure == null)
            {
                _log(LogType.Error, $"Parent [{parentName}] not found in Skin [{skin}]");
            }
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
            PropertyInfo lastProp = null;

            foreach (var propName in property_.Split('.'))
            {
                if (currentObject == null)
                {
                    _log(LogType.Error, $"Path [{property_}] leads to null object");
                    return string.Empty;
                }

                var nextProp = currentObject.GetType().GetProperty(propName);

                // nextProp will return null for dictionary entries or list indexes
                if (nextProp == null)
                {
                    // To access dictionaries or lists, a previous prop must exist
                    if (lastProp == null)
                    {
                        _log(LogType.Error, $"Cannot access [{property_}]: Path leads to nothing for [{propName}]");
                        return string.Empty;
                    }
                    
                    if (typeof(IList).IsAssignableFrom(lastProp.PropertyType))
                    {
                        if (int.TryParse(propName, out int listIndex))
                        {
                            currentObject = ((IList)currentObject)[listIndex];
                            lastProp = null;
                            continue;
                        }
                        else
                        {
                            _log(LogType.Error, $"Cannot access index [{listIndex}] on property [{propName}]: Not an integer!");
                            return string.Empty;
                        }
                    }

                    if (typeof(IDictionary).IsAssignableFrom(lastProp.PropertyType))
                    {
                        currentObject = ((IDictionary)currentObject)[propName];
                        lastProp = null;
                        continue;
                    }

                    _log(LogType.Error, $"Property [{propName}] doesn't exist");
                    return string.Empty;
                }

                currentObject = nextProp.GetValue(currentObject);
                lastProp = nextProp;
            }

            return currentObject.ToString();
        }
    }
}
