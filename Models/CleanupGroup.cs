namespace OPoptimizer.Models;

public sealed record CleanupCandidate(
    string FilePath,
    string TrustedRoot,
    long Length,
    DateTime LastWriteTimeUtc,
    int MinAgeDays);

public sealed class CleanupGroup
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Root { get; init; }
    public required List<CleanupCandidate> Files { get; init; }
    public bool IsSelected { get; set; }
    public long Bytes => Files.Sum(file => file.Length);
    public string Summary => $"{Files.Count:N0} archivos • {FormatBytes(Bytes)}";

    public static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double number = Math.Max(0, bytes);
        int index = 0;
        while (number >= 1024 && index < units.Length - 1)
        {
            number /= 1024;
            index++;
        }
        return $"{number:0.##} {units[index]}";
    }
}

public sealed record CleanupOutcome(int Deleted, int Skipped, long FreedBytes);
