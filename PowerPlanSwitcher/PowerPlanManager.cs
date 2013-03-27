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
        public Guid Guid;
    }

    public class PowerPlanManager
    {        
        public static PowerPlan Active {
            get
            {
                var pCurrentSchemeGuid = IntPtr.Zero;
                WinApi.PowerGetActiveScheme(IntPtr.Zero, ref pCurrentSchemeGuid);
                var currentSchemeGuid = (Guid)Marshal.PtrToStructure(pCurrentSchemeGuid, typeof(Guid));
                return FindById(currentSchemeGuid);
            }
            set {
                var schemeGuid = value.Guid;
                WinApi.PowerSetActiveScheme(IntPtr.Zero, ref schemeGuid);
            }
        }

        public static IEnumerable<PowerPlan> FindAll()
        {
            var schemeGuid = Guid.Empty;

            var sizeSchemeGuid = (uint)Marshal.SizeOf(typeof(Guid));
            uint schemeIndex = 0;

            while (WinApi.PowerEnumerate(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, (uint)WinApi.AccessFlags.AccessScheme, schemeIndex, ref schemeGuid, ref sizeSchemeGuid) == 0)
            {
                var friendlyName = ReadFriendlyName(schemeGuid);
                yield return new PowerPlan { Name = friendlyName, Guid = schemeGuid };
                schemeIndex++;
            }
        }

        public static PowerPlan FindById(Guid id)
        {
            foreach (var plan in FindAll()) {
                if (plan.Guid == id)
                {
                    return plan;
                }
            }
            return new PowerPlan();
        }

        private static string ReadFriendlyName(Guid schemeGuid)
        {
            uint sizeName = 1024;
            var pSizeName = Marshal.AllocHGlobal((int)sizeName);

            string friendlyName;

            try
            {
                WinApi.PowerReadFriendlyName(IntPtr.Zero, ref schemeGuid, IntPtr.Zero, IntPtr.Zero, pSizeName, ref sizeName);
                friendlyName = Marshal.PtrToStringUni(pSizeName);
            }
            finally
            {
                Marshal.FreeHGlobal(pSizeName);
            }

            return friendlyName;
        }
    }

    internal static class WinApi
    {
        [DllImport("PowrProf.dll")]
        public static extern UInt32 PowerEnumerate(IntPtr rootPowerKey, IntPtr schemeGuid, IntPtr subGroupOfPowerSettingGuid, UInt32 acessFlags, UInt32 index, ref Guid buffer, ref UInt32 bufferSize);

        public enum AccessFlags : uint
        {
            AccessScheme = 16,
            AccessSubgroup = 17,
            AccessIndividualSetting = 18
        }

        [DllImport("PowrProf.dll")]
        public static extern UInt32 PowerReadFriendlyName(IntPtr rootPowerKey, ref Guid schemeGuid, IntPtr subGroupOfPowerSettingGuid, IntPtr powerSettingGuid, IntPtr buffer, ref UInt32 bufferSize);

        [DllImport("PowrProf.dll")]
        public static extern uint PowerGetActiveScheme(IntPtr userRootPowerKey, ref IntPtr activePolicyGuid);

        [DllImport("PowrProf.dll")]
        public static extern uint PowerSetActiveScheme(IntPtr userRootPowerKey, ref Guid schemeGuid);
    }
}
