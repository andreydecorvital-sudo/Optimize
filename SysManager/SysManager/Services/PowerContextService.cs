// Optimize · current power context
// Based on SysManager (MIT) — original license preserved in repository.

using System.Management;
using Serilog;

namespace SysManager.Services;

public sealed class PowerContextService
{
    /// <summary>
    /// True when Windows reports a discharging battery, false when a battery is present
    /// and not discharging, null when the machine has no battery or WMI cannot determine it.
    /// </summary>
    public bool? IsRunningOnBattery()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT BatteryStatus FROM Win32_Battery");
            using var collection = searcher.Get();
            var found = false;
            foreach (ManagementObject mo in collection)
            {
                using (mo)
                {
                    found = true;
                    var status = Convert.ToUInt16(mo["BatteryStatus"] ?? 0);
                    // Win32_Battery: 1 = Discharging. Most other known states imply external
                    // power or charging; unknown 0 is treated as undetermined rather than AC.
                    if (status == 1) return true;
                    if (status > 1) return false;
                }
            }
            return found ? null : null;
        }
        catch (Exception ex) when (ex is ManagementException or System.Runtime.InteropServices.COMException or FormatException or InvalidCastException)
        {
            Log.Debug("Optimize battery power-state detection unavailable: {Error}", ex.Message);
            return null;
        }
    }
}
