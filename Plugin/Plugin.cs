using Rainmeter;
using System;
using System.Runtime.InteropServices;
using static Rainmeter.Api;

namespace rainify
{
    class Measure
    {
        static public implicit operator Measure(IntPtr data)
        {
            return (Measure)GCHandle.FromIntPtr(data).Target;
        }
        
        public string clientId = string.Empty;
        public string clientSecrect = string.Empty;
        public string refreshToken = string.Empty;

    }

    public class Plugin
    {
        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            Api api = rm;

            api.Log(LogType.Debug, "Initializing rainify plugin");

            string clientId = api.ReadString("ClientId", string.Empty);
            string clientSecret = api.ReadString("ClientSecret", string.Empty);
            string refreshToken = api.ReadString("RefreshToken", string.Empty);

            if (string.IsNullOrWhiteSpace(clientId) ||
                string.IsNullOrWhiteSpace(clientSecret) ||
                string.IsNullOrWhiteSpace(refreshToken))
            {
                api.Log(LogType.Error, "ClientId, ClientSecret and RefreshToken must be set. Use the console to generate them");
                return;
            }

            Measure measure = GCHandle.ToIntPtr(GCHandle.Alloc(new Measure()));
            measure.clientId = clientId;
            measure.clientSecrect = clientSecret;
            measure.refreshToken = refreshToken;

            api.Log(LogType.Notice, "Finished initialization");
        }

        [DllExport]
        public static void Finalize(IntPtr data)
        {
            GCHandle.FromIntPtr(data).Free();
        }

        [DllExport]
        public static void Reload(IntPtr data, IntPtr rm, ref double maxValue)
        {
            Measure measure = data;
        }

        [DllExport]
        public static double Update(IntPtr data)
        {
            Measure measure = data;
            return 0.0;
        }

        [DllExport]
        public static IntPtr GetString(IntPtr data)
        {
            Measure measure = data;
            return Marshal.StringToHGlobalUni("clientId:" + measure.clientId + " clientSecret:" + measure.clientSecrect + " refreshToken:" + measure.refreshToken);
        }

        //[DllExport]
        //public static void ExecuteBang(IntPtr data, [MarshalAs(UnmanagedType.LPWStr)]String args)
        //{
        //    Measure measure = (Measure)data;
        //}

        //[DllExport]
        //public static IntPtr (IntPtr data, int argc,
        //    [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] argv)
        //{
        //    Measure measure = (Measure)data;
        //    if (measure.buffer != IntPtr.Zero)
        //    {
        //        Marshal.FreeHGlobal(measure.buffer);
        //        measure.buffer = IntPtr.Zero;
        //    }
        //
        //    measure.buffer = Marshal.StringToHGlobalUni("");
        //
        //    return measure.buffer;
        //}
    }
}

