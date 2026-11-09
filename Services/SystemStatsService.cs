using System.IO;
using System.Runtime.InteropServices;

namespace OPoptimizer.Services;

public sealed record SystemStats(double CpuPercent, double RamPercent, string RamUsedText,
    double DiskPercent, string DiskFreeText);

public sealed class SystemStatsService
{
    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime
    {
        public uint Low;
        public uint High;
        public readonly ulong Number => ((ulong)High << 32) | Low;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatus
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out FileTime idle, out FileTime kernel, out FileTime user);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatus memory);

    private ulong? _lastIdle;
    private ulong? _lastTotal;
    private double _latestCpu;

    public SystemStats Sample()
    {
        if (GetSystemTimes(out var idle, out var kernel, out var user))
        {
            ulong total = kernel.Number + user.Number;
            if (_lastIdle.HasValue && _lastTotal.HasValue && total > _lastTotal.Value)
            {
                ulong deltaTotal = total - _lastTotal.Value;
                ulong deltaIdle = idle.Number >= _lastIdle.Value ? idle.Number - _lastIdle.Value : 0;
                _latestCpu = Math.Clamp(100.0 * (1.0 - (double)deltaIdle / deltaTotal), 0, 100);
            }
            _lastIdle = idle.Number;
            _lastTotal = total;
        }

        var ram = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() };
        bool hasRam = GlobalMemoryStatusEx(ref ram);
        double ramPercentage = hasRam && ram.TotalPhysical > 0
            ? (double)(ram.TotalPhysical - ram.AvailablePhysical) / ram.TotalPhysical * 100 : 0;
        string ramText = hasRam ? $"{(ram.TotalPhysical - ram.AvailablePhysical) / Math.Pow(1024, 3):0.0} GB usados" : "No disponible";

        try
        {
            string root = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
            var disk = new DriveInfo(root);
            double percent = disk.TotalSize > 0 ? 100.0 * (disk.TotalSize - disk.AvailableFreeSpace) / disk.TotalSize : 0;
            return new(_latestCpu, ramPercentage, ramText, percent,
                $"{disk.AvailableFreeSpace / Math.Pow(1024, 3):0.0} GB libres");
        }
        catch
        {
            return new(_latestCpu, ramPercentage, ramText, 0, "No disponible");
        }
    }
}
