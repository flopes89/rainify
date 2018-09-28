using Rainmeter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
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
        /// <param name="type_">Message type</param>
        /// <param name="message_">Log message</param>
        internal delegate void Log(LogType type_, string message_);

        /// <summary>
        /// The field that is used by the measure
        /// Fields itself are held by the ParentMeasure in a dictionary
        /// </summary>
        internal string Field { get; set; } = string.Empty;

        /// <summary>
        /// Message function to use for logging
        /// </summary>
        internal protected Log _log { get; set; }

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
        /// <param name="api_"><see cref="Plugin.Reload(IntPtr, IntPtr, ref double)"/></param>
        /// <param name="max_"><see cref="Plugin.Reload(IntPtr, IntPtr, ref double)"/></param>
        internal virtual void Reload(Api api_, ref double max_)
        {
            Field = api_.ReadString("Field", string.Empty);
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
        /// <param name="api_"><see cref="BaseMeasure.Reload(Api, ref double)"/></param>
        /// <param name="max_"><see cref="BaseMeasure.Reload(Api, ref double)"/></param>
        internal override void Reload(Api api_, ref double max_)
        {
            base.Reload(api_, ref max_);

            Name = api_.GetMeasureName();
            Skin = api_.GetSkin();

            _log(LogType.Notice, "Reloading parent measure [" + Name + "] in Skin [" + Skin + "]");

            _clientId = api_.ReadString("ClientId", string.Empty);
            _clientSecret = api_.ReadString("ClientSecret", string.Empty);
            _refreshToken = api_.ReadString("RefreshToken", string.Empty);
            _consolePath = api_.ReadPath("ConsolePath", string.Empty);

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
            
            var console = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = consoleExe.FullName,
                    Arguments = string.Format("status -i {0} -s {1} -t {2}", _clientId, _clientSecret, _refreshToken),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };

            console.Start();

            var error = console.StandardError.ReadToEnd();
            if (!string.IsNullOrWhiteSpace(error))
            {
                _log(LogType.Error, "Dumping rainifyConsole errors:\r\n" + error);
            }

            var output = console.StandardOutput.ReadToEnd();
            console.WaitForExit();

            var matches = Regex.Matches(output, "^(?<Name>.+)===(?<Value>.+)$", RegexOptions.Multiline);
            FieldValues = new Dictionary<string, FieldValue>();
            foreach (Match match in matches)
            {
                var name = match.Groups["Name"].Value;
                var value = match.Groups["Value"].Value;

                var fieldValue = new FieldValue
                {
                    StringValue = value
                };

                var doubleValue = 0.0;
                double.TryParse(value, out doubleValue);
                fieldValue.DoubleValue = doubleValue;

                _log(LogType.Debug, "Adding new FieldValue [" + name + "]");
                FieldValues.Add(name, fieldValue);
            }

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
            if (_parentMeasure != null)
            {
                if (_parentMeasure.FieldValues.ContainsKey(Field))
                {
                    var value = _parentMeasure.FieldValues[Field];
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
            if (_parentMeasure != null)
            {
                if (_parentMeasure.FieldValues.ContainsKey(Field))
                {
                    var value = _parentMeasure.FieldValues[Field];
                    return value.StringValue;
                }

                _log(LogType.Warning, "Access to unknown Field [" + Field + "]");
                return "Access to unknown Field [" + Field + "]";
            }

            return base.GetString();
        }
    }
}
