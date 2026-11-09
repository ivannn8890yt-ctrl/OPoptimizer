using System.IO;
using OPoptimizer.Models;

namespace OPoptimizer.Services;

/// <summary>
/// Only predeclared, user-readable locations are scanned. Nothing is deleted during Scan().
/// Clean() revalidates each candidate so changed/recent files are not removed.
/// </summary>
public sealed class CleanerService
{
    private sealed record CleanupDefinition(string Name, string Description, string Path,
        int MinimumAgeDays, string[]? AllowedExtensions, bool DefaultSelected);

    private static readonly StringComparison PathComparison = StringComparison.OrdinalIgnoreCase;

    public List<CleanupGroup> Scan()
    {
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var definitions = new[]
        {
            new CleanupDefinition("Temporales de Windows (usuario)",
                "Solo archivos con más de 7 días en tu carpeta temporal. No borra documentos ni descargas.",
                Path.GetTempPath(), 7, null, true),
            new CleanupDefinition("Informes de fallos antiguos",
                "Archivos .dmp de diagnóstico con más de 14 días. Opcional: pueden servir para investigar errores.",
                Path.Combine(local, "CrashDumps"), 14, [".dmp"], false),
            new CleanupDefinition("Registros antiguos de Roblox",
                "Solo archivos de registro con más de 7 días. No toca juegos, cuentas ni configuraciones.",
                Path.Combine(local, "Roblox", "logs"), 7, [".log", ".txt"], false),
            new CleanupDefinition("Registros antiguos de Minecraft",
                "Solo logs con más de 7 días; se conserva latest.log y los mundos.",
                Path.Combine(roaming, ".minecraft", "logs"), 7, [".log", ".gz", ".txt"], false)
        };

        var result = new List<CleanupGroup>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in definitions)
        {
            var candidates = new List<CleanupCandidate>();
            try
            {
                string root = Path.GetFullPath(item.Path);
                if (!Directory.Exists(root) || !seenPaths.Add(root.TrimEnd(Path.DirectorySeparatorChar)))
                    continue;

                var enumeration = new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true,
                    AttributesToSkip = FileAttributes.ReparsePoint,
                    ReturnSpecialDirectories = false
                };

                foreach (var path in Directory.EnumerateFiles(root, "*", enumeration))
                {
                    try
                    {
                        if (!IsUnderRoot(path, root) || Path.GetFileName(path).Equals("latest.log", PathComparison))
                            continue;
                        if (item.AllowedExtensions is not null &&
                            !item.AllowedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                            continue;
                        var info = new FileInfo(path);
                        if (!info.Exists || (info.Attributes & FileAttributes.ReparsePoint) != 0 ||
                            info.LastWriteTimeUtc > DateTime.UtcNow.AddDays(-item.MinimumAgeDays))
                            continue;
                        candidates.Add(new CleanupCandidate(info.FullName, root, info.Length,
                            info.LastWriteTimeUtc, item.MinimumAgeDays));
                    }
                    catch (Exception) { /* inaccessible file: skip */ }
                }
                result.Add(new CleanupGroup
                {
                    Name = item.Name,
                    Description = item.Description,
                    Root = root,
                    Files = candidates,
                    IsSelected = item.DefaultSelected && candidates.Count > 0
                });
            }
            catch (Exception) { /* inaccessible folder: skip */ }
        }
        return result;
    }

    public CleanupOutcome Clean(IEnumerable<CleanupCandidate> candidates)
    {
        int deleted = 0;
        int skipped = 0;
        long freedBytes = 0;
        foreach (var candidate in candidates.DistinctBy(x => x.FilePath, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                string path = Path.GetFullPath(candidate.FilePath);
                string root = Path.GetFullPath(candidate.TrustedRoot);
                if (!IsUnderRoot(path, root) || !File.Exists(path) ||
                    Path.GetFileName(path).Equals("latest.log", PathComparison))
                {
                    skipped++;
                    continue;
                }

                var info = new FileInfo(path);
                // Protect changed files, newly active files, and junction/symlink targets.
                if ((info.Attributes & FileAttributes.ReparsePoint) != 0 ||
                    info.LastWriteTimeUtc != candidate.LastWriteTimeUtc ||
                    info.Length != candidate.Length ||
                    info.LastWriteTimeUtc > DateTime.UtcNow.AddDays(-candidate.MinAgeDays))
                {
                    skipped++;
                    continue;
                }
                File.Delete(path);
                freedBytes += candidate.Length;
                deleted++;
            }
            catch (Exception) { skipped++; /* locked, changed or inaccessible: skip */ }
        }
        return new CleanupOutcome(deleted, skipped, freedBytes);
    }

    private static bool IsUnderRoot(string path, string root)
    {
        string candidate = Path.GetFullPath(path);
        string parent = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        return candidate.StartsWith(parent, PathComparison);
    }
}
