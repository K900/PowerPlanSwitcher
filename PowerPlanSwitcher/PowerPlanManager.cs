using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PowerPlanSwitcher
{
    /*
     * This is a modified version of the source code originally found in
     * https://github.com/dun3/Com.Hertkorn.SetPowerScheme
     * I don't know what license that is under, so if the author has any issues,
     * I'm willing to take it down and replace with my own implementation
     * (even though there isn't too much to replace)
     */

    public struct PowerPlan
    {
        public string Name;
        public Guid GUID;
    }

    public class PowerPlanManager
    {        
        public static PowerPlan Active {
            get
            {
                IntPtr pCurrentSchemeGuid = IntPtr.Zero;
                WinAPI.PowerGetActiveScheme(IntPtr.Zero, ref pCurrentSchemeGuid);
                Guid currentSchemeGuid = (Guid)Marshal.PtrToStructure(pCurrentSchemeGuid, typeof(Guid));
                return FindById(currentSchemeGuid);
            }
            set {
                Guid schemeGuid = value.GUID;
                WinAPI.PowerSetActiveScheme(IntPtr.Zero, ref schemeGuid);
            }
        }

        public static IEnumerable<PowerPlan> FindAll()
        {
            Guid schemeGuid = Guid.Empty;

            uint sizeSchemeGuid = (uint)Marshal.SizeOf(typeof(Guid));
            uint schemeIndex = 0;

            while (WinAPI.PowerEnumerate(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, (uint)WinAPI.AccessFlags.ACCESS_SCHEME, schemeIndex, ref schemeGuid, ref sizeSchemeGuid) == 0)
            {
                string friendlyName = ReadFriendlyName(schemeGuid);

                yield return new PowerPlan { Name = friendlyName, GUID = schemeGuid };

                schemeIndex++;
            }
        }

        public static PowerPlan FindById(Guid id)
        {
            IEnumerable<PowerPlan> plans = FindAll();
            foreach (PowerPlan plan in plans) {
                if (plan.GUID == id)
                {
                    return plan;
                }
            }
            return new PowerPlan { };
        }

        private static string ReadFriendlyName(Guid schemeGuid)
        {
            uint sizeName = 1024;
            IntPtr pSizeName = Marshal.AllocHGlobal((int)sizeName);

            string friendlyName;

            try
            {
                WinAPI.PowerReadFriendlyName(IntPtr.Zero, ref schemeGuid, IntPtr.Zero, IntPtr.Zero, pSizeName, ref sizeName);
                friendlyName = Marshal.PtrToStringUni(pSizeName);
            }
            finally
            {
                Marshal.FreeHGlobal(pSizeName);
            }

            return friendlyName;
        }
    }

    internal static class WinAPI
    {
        [DllImport("PowrProf.dll")]
        public static extern UInt32 PowerEnumerate(IntPtr RootPowerKey, IntPtr SchemeGuid, IntPtr SubGroupOfPowerSettingGuid, UInt32 AcessFlags, UInt32 Index, ref Guid Buffer, ref UInt32 BufferSize);

        public enum AccessFlags : uint
        {
            ACCESS_SCHEME = 16,
            ACCESS_SUBGROUP = 17,
            ACCESS_INDIVIDUAL_SETTING = 18
        }

        [DllImport("PowrProf.dll")]
        public static extern UInt32 PowerReadFriendlyName(IntPtr RootPowerKey, ref Guid SchemeGuid, IntPtr SubGroupOfPowerSettingGuid, IntPtr PowerSettingGuid, IntPtr Buffer, ref UInt32 BufferSize);

        [DllImport("PowrProf.dll")]
        public static extern uint PowerGetActiveScheme(IntPtr UserRootPowerKey, ref IntPtr ActivePolicyGuid);

        [DllImport("PowrProf.dll")]
        public static extern uint PowerSetActiveScheme(IntPtr UserRootPowerKey, ref Guid SchemeGuid);
    }
}
