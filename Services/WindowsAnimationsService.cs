using System.Runtime.InteropServices;

namespace OPoptimizer.Services;

/// <summary>Changes only the supported per-user Windows client-area animation setting.</summary>
public static class WindowsAnimationsService
{
    private const uint SpiGetClientAreaAnimation = 0x1042;
    private const uint SpiSetClientAreaAnimation = 0x1043;
    private const uint SpifUpdateIniFile = 0x01;
    private const uint SpifSendChange = 0x02;

    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(uint action, uint parameter, ref int value, uint flags);

    public static bool TryGetEnabled(out bool enabled)
    {
        int value = 0;
        bool success = SystemParametersInfo(SpiGetClientAreaAnimation, 0, ref value, 0);
        enabled = value != 0;
        return success;
    }

    public static bool TrySetEnabled(bool enabled)
    {
        int value = enabled ? 1 : 0;
        return SystemParametersInfo(SpiSetClientAreaAnimation, 0, ref value, SpifUpdateIniFile | SpifSendChange);
    }
}
