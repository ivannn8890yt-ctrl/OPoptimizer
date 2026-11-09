using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using OPoptimizer.Models;

namespace OPoptimizer.Services;

/// <summary>Read-only discovery: never opens or edits game files.</summary>
public sealed class GameDiscoveryService
{
    public List<GameEntry> FindGames()
    {
        var games = new List<GameEntry>();
        FindSteam(games);
        FindEpic(games);
        FindCommonLaunchers(games);
        return games
            .Where(game => Directory.Exists(game.InstallPath))
            .DistinctBy(game => Path.GetFullPath(game.InstallPath), StringComparer.OrdinalIgnoreCase)
            .OrderBy(game => game.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static void FindSteam(List<GameEntry> games)
    {
        var possibleRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            string? steamPath = key?.GetValue("SteamPath") as string;
            if (!string.IsNullOrWhiteSpace(steamPath)) possibleRoots.Add(steamPath);
        }
        catch (Exception) { }
        possibleRoots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"));

        foreach (string steamRoot in possibleRoots)
        {
            try
            {
                if (!Directory.Exists(steamRoot)) continue;
                var libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { steamRoot };
                string vdfFile = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
                if (File.Exists(vdfFile))
                {
                    string content = File.ReadAllText(vdfFile);
                    foreach (Match match in Regex.Matches(content, "\"path\"\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase))
                    {
                        string folder = match.Groups[1].Value.Replace("\\\\", "\\");
                        if (Directory.Exists(folder)) libraries.Add(folder);
                    }
                }

                foreach (string library in libraries)
                {
                    string steamApps = Path.Combine(library, "steamapps");
                    if (!Directory.Exists(steamApps)) continue;
                    foreach (string manifest in Directory.EnumerateFiles(steamApps, "appmanifest_*.acf"))
                    {
                        try
                        {
                            string text = File.ReadAllText(manifest);
                            var name = Regex.Match(text, "\"name\"\\s*\"([^\"]+)\"").Groups[1].Value;
                            var folder = Regex.Match(text, "\"installdir\"\\s*\"([^\"]+)\"").Groups[1].Value;
                            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(folder)) continue;
                            string install = Path.GetFullPath(Path.Combine(steamApps, "common", folder));
                            if (Directory.Exists(install)) games.Add(new GameEntry(name, "Steam", install));
                        }
                        catch (Exception) { }
                    }
                }
            }
            catch (Exception) { }
        }
    }

    private static void FindEpic(List<GameEntry> games)
    {
        try
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Epic", "EpicGamesLauncher", "Data", "Manifests");
            if (!Directory.Exists(folder)) return;
            foreach (string file in Directory.EnumerateFiles(folder, "*.item"))
            {
                try
                {
                    using var document = JsonDocument.Parse(File.ReadAllText(file));
                    var json = document.RootElement;
                    string? name = json.TryGetProperty("DisplayName", out var n) ? n.GetString() : null;
                    string? location = json.TryGetProperty("InstallLocation", out var loc) ? loc.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(location) && Directory.Exists(location))
                        games.Add(new GameEntry(name, "Epic Games", location));
                }
                catch (Exception) { }
            }
        }
        catch (Exception) { }
    }

    private static void FindCommonLaunchers(List<GameEntry> games)
    {
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var minecraft = Path.Combine(roaming, ".minecraft");
        if (Directory.Exists(minecraft)) games.Add(new GameEntry("Minecraft Java", "Minecraft", minecraft));
        var roblox = Path.Combine(local, "Roblox", "Versions");
        if (Directory.Exists(roblox)) games.Add(new GameEntry("Roblox", "Roblox", roblox));
    }
}
