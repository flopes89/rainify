using Rainmeter;
using System;
using System.Collections.Generic;
using static Rainmeter.Api;

namespace rainify.Plugin
{
    /// <summary>
    /// Base class for parents and child measures
    /// </summary>
    class BaseMeasure
    {
        /// <summary>
        /// The field that is used by the measure
        /// Fields itself are held by the ParentMeasure in a dictionary
        /// </summary>
        internal string Field = string.Empty;
        
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
    class ParentMeasure : BaseMeasure
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
        
        string ClientId { get; set; }
        string ClientSecret { get; set; }
        string RefreshToken { get; set; }

        /// <summary>
        /// Add this parent measure to the list of parent measures
        /// </summary>
        internal ParentMeasure()
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

            api.Log(LogType.Debug, "Reloading parent rainify measure [" + Name + "] in Skin [" + Skin + "]");

            ClientId = api.ReadString("ClientId", string.Empty);
            ClientSecret = api.ReadString("ClientSecret", string.Empty);
            RefreshToken = api.ReadString("RefreshToken", string.Empty);

            if (string.IsNullOrWhiteSpace(ClientId) ||
                string.IsNullOrWhiteSpace(ClientSecret) ||
                string.IsNullOrWhiteSpace(RefreshToken))
            {
                api.Log(LogType.Error, "ClientId, ClientSecret and RefreshToken must be set. Use the console to generate them");
                return;
            }

            FieldValues["hello"] = new FieldValue()
            {
                StringValue = "world",
            };

            api.Log(LogType.Debug, "Reloading done");
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
        /// The parent measure of this child
        /// </summary>
        private ParentMeasure ParentMeasure = null;

        /// <summary>
        /// <see cref="BaseMeasure.Reload(Api, ref double)"/>
        /// </summary>
        /// <param name="api"><see cref="BaseMeasure.Reload(Api, ref double)"/></param>
        /// <param name="max"><see cref="BaseMeasure.Reload(Api, ref double)"/></param>
        internal override void Reload(Api api, ref double max)
        {
            base.Reload(api, ref max);

            api.Log(LogType.Debug, "Reloading child rainify measure [" + api.GetMeasureName() + "]");

            var parentName = api.ReadString("ParentName", string.Empty);
            var skin = api.GetSkin();

            ParentMeasure = null;
            foreach (ParentMeasure parent in ParentMeasure.Parents)
            {
                if (parent.Skin.Equals(skin) && parent.Name.Equals(parentName))
                {
                    ParentMeasure = parent;
                }
            }

            if (ParentMeasure == null)
            {
                api.Log(LogType.Error, "Parent [" + parentName + "] not found!");
            }

            api.Log(LogType.Debug, "Reloading done");
        }

        /// <summary>
        /// Get the double value of a single field saved in the parent measure
        /// </summary>
        /// <returns>Double value of the field</returns>
        internal override double Update()
        {
            if (ParentMeasure != null)
            {
                var value = ParentMeasure.FieldValues[Field];
                return value.DoubleValue;
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
                var value = ParentMeasure.FieldValues[Field];
                return value.StringValue;
            }

            return base.GetString();
        }
    }
}
