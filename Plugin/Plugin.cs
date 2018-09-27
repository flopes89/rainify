﻿using Rainmeter;
using System;
using System.Runtime.InteropServices;
using static Rainmeter.Api;

namespace rainify.Plugin
{
    /// <summary>
    /// Rainmeter plugin class
    /// </summary>
    public class Plugin
    {
        /// <summary>
        /// Static reference to the Rainmeter API
        /// </summary>
        static Api _api;

        /// <summary>
        /// Called when a measure is created (i.e. when a skin is loaded or when a skin is refreshed).
        /// Create your measure object here. Any other initialization or code that only needs to happen once should be placed here.
        /// </summary>
        /// <param name="data">You may allocate and store measure specific data to this variable. The object you save here will be passed to other functions below.</param>
        /// <param name="rm">Internal pointer that is passed to most API functions. If needed, you may save this value for later use (like for logging functions).</param>
        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            _api = rm;

            try
            {
                _api.Log(LogType.Debug, "Initializing plugin");
                string parent = _api.ReadString("ParentName", string.Empty);
                BaseMeasure measure;

                if (string.IsNullOrWhiteSpace(parent))
                {
                    measure = new ParentMeasure(_api.Log);
                }
                else
                {
                    measure = new ChildMeasure(_api.Log);
                }

                data = GCHandle.ToIntPtr(GCHandle.Alloc(measure));
            }
            catch (Exception ex)
            {
                _api.Log(LogType.Error, ex.Message);
                _api.Log(LogType.Error, ex.StackTrace);
            }
            finally
            {
                _api.Log(LogType.Debug, "Initializing plugin done");
            }
        }

        /// <summary>
        /// Called by Rainmeter when the measure settings are to be read directly after Initialize.
        /// If DynamicVariables=1 is set on the measure, this function is called just before every call to the Update function during the update cycle.
        /// </summary>
        /// <param name="data">Pointer to the data set in Initialize.</param>
        /// <param name="rm">Internal pointer that is passed to most API functions.</param>
        /// <param name="maxValue">Pointer to a double that can be assigned to the default maximum value for this measure.
        /// A value of 0.0 will make it based on the highest value returned from the Update function. Do not set maxValue unless necessary.</param>
        [DllExport]
        public static void Reload(IntPtr data, IntPtr rm, ref double maxValue)
        {
            try
            {
                _api.Log(LogType.Debug, "Reloading plugin");
                BaseMeasure measure = (BaseMeasure)GCHandle.FromIntPtr(data).Target;
                measure.Reload(rm, ref maxValue);
            }
            catch (Exception ex)
            {
                _api.Log(LogType.Error, ex.Message);
                _api.Log(LogType.Error, ex.StackTrace);
            }
            finally
            {
                _api.Log(LogType.Debug, "Reloading plugin done");
            }
        }

        /// <summary>
        /// Called by Rainmeter when a measure value is to be updated (i.e. on each update cycle). The number returned represents the number value of the measure.
        /// </summary>
        /// <param name="data">Pointer to the data set in Initialize.</param>
        /// <returns>The number value of the measure (as a double).
        /// This value will be used as the string value of the measure if the GetString function is not used or returns a null.</returns>
        [DllExport]
        public static double Update(IntPtr data)
        {
            try
            {
                _api.Log(LogType.Debug, "Updating plugin");
                BaseMeasure measure = (BaseMeasure)GCHandle.FromIntPtr(data).Target;
                return measure.Update();
            }
            catch (Exception ex)
            {
                _api.Log(LogType.Error, ex.Message);
                _api.Log(LogType.Error, ex.StackTrace);
                return 0;
            }
            finally
            {
                _api.Log(LogType.Debug, "Updating plugin done");
            }
        }

        /// <summary>
        /// Optional function that returns the string value of the measure.
        /// Since this function is called 'on-demand' and may be called multiple times during the update cycle,
        /// do not process any data or consume CPU in this function. Do as minimal processing as possible to
        /// return the desired string. It is recommended to do all processing during the Update function and set
        /// a string variable there and retrieve that string variable in this function.
        /// The return value must be marshalled from a C# style string to a C style string (WCHAR*).
        /// </summary>
        /// <param name="data">Pointer to the data set in Initialize.</param>
        /// <returns>The string value for the measure. If you want the number value (returned from Update) to be used
        /// as the measures value, return null instead. The return value must be marshalled.</returns>
        [DllExport]
        public static IntPtr GetString(IntPtr data)
        {
            try
            {
                _api.Log(LogType.Debug, "GetString called");
                BaseMeasure measure = (BaseMeasure)GCHandle.FromIntPtr(data).Target;
                return Marshal.StringToHGlobalUni(measure.GetString());
            }
            catch (Exception ex)
            {
                _api.Log(LogType.Error, ex.Message);
                _api.Log(LogType.Error, ex.StackTrace);
                return Marshal.StringToHGlobalUni(string.Empty);
            }
            finally
            {
                _api.Log(LogType.Debug, "GetString done");
            }
        }

        /// <summary>
        /// Called by Rainmeter when a measure is about to be destroyed. Perform cleanup here.
        /// </summary>
        /// <param name="data">Pointer to the data set in Initialize.</param>
        [DllExport]
        public static void Finalize(IntPtr data)
        {
            try
            {
                _api.Log(LogType.Debug, "Finalizing plugin");
                BaseMeasure measure = (BaseMeasure)GCHandle.FromIntPtr(data).Target;
                measure.Dispose();
                GCHandle.FromIntPtr(data).Free();
            }
            catch (Exception ex)
            {
                _api.Log(LogType.Error, ex.Message);
                _api.Log(LogType.Error, ex.StackTrace);
            }
            finally
            {
                _api.Log(LogType.Debug, "Finalizing plugin done");
            }
        }
    }
}
