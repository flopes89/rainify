using Rainmeter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using static Rainmeter.Api;

namespace rainify.Plugin
{
    /// <summary>
    /// Base class for parents and child measures
    /// </summary>
    internal class BaseMeasure
    {
        /// <summary>
        /// Log delegate to use when logging messages
        /// </summary>
        /// <param name="type">Message type</param>
        /// <param name="message">Log message</param>
        internal delegate void Log(LogType type, string message);

        /// <summary>
        /// The field that is used by the measure
        /// Fields itself are held by the ParentMeasure in a dictionary
        /// </summary>
        internal string Field = string.Empty;

        /// <summary>
        /// Message to use for logging
        /// </summary>
        internal protected Log _log;

        /// <summary>
        /// Base Measure instance
        /// </summary>
        /// <param name="log_">Function to use for logging</param>
        internal BaseMeasure(Log log_)
        {
            _log = log_;
        }

        /// <summary>
        /// <see cref="Plugin.Reload(IntPtr, IntPtr, ref double)"/>
        /// </summary>
        /// <param name="api"><see cref="Plugin.Reload(IntPtr, IntPtr, ref double)"/></param>
        /// <param name="max"><see cref="Plugin.Reload(IntPtr, IntPtr, ref double)"/></param>
        internal virtual void Reload(Api api, ref double max)
        {
            Field = api.ReadString("Field", string.Empty);
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
            return "ParentMeasure doesn't return anything. Use child measures to access fields";
        }
    }

    /// <summary>
    /// The parent that holds all fields returned by the SpotifyApi
    /// Fields are accessed by child measures by the field name
    /// </summary>
    internal class ParentMeasure : BaseMeasure
    {
        /// <summary>
        /// List of all parent measures is used by the child measures to find their parent
        /// </summary>
        internal static IList<ParentMeasure> Parents = new List<ParentMeasure>();

        /// <summary>
        /// The fields returned by the SpotifyApi that can be accessed
        /// </summary>
        internal IDictionary<string, FieldValue> FieldValues = new Dictionary<string, FieldValue>();

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
        /// Path to the rainify console
        /// </summary>
        string _consolePath { get; set; }

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
        /// <param name="api"><see cref="BaseMeasure.Reload(Api, ref double)"/></param>
        /// <param name="max"><see cref="BaseMeasure.Reload(Api, ref double)"/></param>
        internal override void Reload(Api api, ref double max)
        {
            base.Reload(api, ref max);

            Name = api.GetMeasureName();
            Skin = api.GetSkin();

            _log(LogType.Debug, "Reloading parent measure [" + Name + "] in Skin [" + Skin + "]");

            _clientId = api.ReadString("ClientId", string.Empty);
            _clientSecret = api.ReadString("ClientSecret", string.Empty);
            _refreshToken = api.ReadString("RefreshToken", string.Empty);
            _consolePath = api.ReadPath("ConsolePath", "Console.exe");

            if (string.IsNullOrWhiteSpace(_clientId) ||
                string.IsNullOrWhiteSpace(_clientSecret) ||
                string.IsNullOrWhiteSpace(_refreshToken))
            {
                _log(LogType.Error, "ClientId, ClientSecret and RefreshToken must be set. Use the console to generate them");
                return;
            }

            if (string.IsNullOrWhiteSpace(_consolePath))
            {
                _log(LogType.Error, "ConsolePath must be set");
                return;
            }

            if (!_consolePath.EndsWith("Console.exe", StringComparison.InvariantCultureIgnoreCase))
            {
                _log(LogType.Error, "ConsolePath must point to Console.exe");
                return;
            }

            var consoleExe = new FileInfo(_consolePath);

            _log(LogType.Debug, "Executing console exe at [" + consoleExe.FullName + "]");

            //var console = new Process()
            //{
            //    StartInfo = new ProcessStartInfo()
            //    {
            //        FileName = consoleExe,
            //        Arguments = string.Format("status -c {0} -s {1} -t {2}", ClientId, ClientSecret, RefreshToken),
            //        CreateNoWindow = false,
            //        RedirectStandardError = true,
            //        RedirectStandardOutput = true
            //    }
            //};

            //console.WaitForExit();

            FieldValues["hello"] = new FieldValue()
            {
                StringValue = "world",
            };

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
    internal class ChildMeasure : BaseMeasure
    {
        /// <summary>
        /// The parent measure of this child
        /// </summary>
        ParentMeasure ParentMeasure = null;

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
        /// <param name="api"><see cref="BaseMeasure.Reload(Api, ref double)"/></param>
        /// <param name="max"><see cref="BaseMeasure.Reload(Api, ref double)"/></param>
        internal override void Reload(Api api, ref double max)
        {
            base.Reload(api, ref max);
            
            var parentName = api.ReadString("ParentName", string.Empty);
            var skin = api.GetSkin();

            _log(LogType.Debug, "Reloading child measure [" + api.GetMeasureName() + "] in Skin [" + skin + "]");
            ParentMeasure = null;
            foreach (ParentMeasure parent in ParentMeasure.Parents)
            {
                if (parent.Skin.Equals(skin) && parent.Name.Equals(parentName))
                {
                    _log(LogType.Debug, "Found parent measure for [" + api.GetMeasureName() + "] in Skin [" + parent.Skin + "]: [" + parentName + "]");
                    ParentMeasure = parent;
                }
            }

            if (ParentMeasure == null)
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
            if (ParentMeasure != null)
            {
                if (ParentMeasure.FieldValues.ContainsKey(Field))
                {
                    var value = ParentMeasure.FieldValues[Field];
                    return value.DoubleValue;
                }

                _log(LogType.Warning, "Access to unknown Field [" + Field + "]");
                return 0;
            }

            return base.Update();
        }

        /// <summary>
        /// Get the string value of a single field saved in the parent measure
        /// </summary>
        /// <returns>String value of the field</returns>
        internal override string GetString()
        {
            if (ParentMeasure != null)
            {
                if (ParentMeasure.FieldValues.ContainsKey(Field))
                {
                    var value = ParentMeasure.FieldValues[Field];
                    return value.StringValue;
                }

                _log(LogType.Warning, "Access to unknown Field [" + Field + "]");
                return "Access to unknown Field [" + Field + "]";
            }

            return base.GetString();
        }
    }
}
