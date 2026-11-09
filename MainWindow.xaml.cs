using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using OPoptimizer.Models;
using OPoptimizer.Services;

namespace OPoptimizer;

public partial class MainWindow : Window
{
    private readonly SystemStatsService _stats = new();
    private readonly CleanerService _cleaner = new();
    private readonly GameDiscoveryService _gameDiscovery = new();
    private readonly SettingsService _settingsService = new();
    private readonly AppSettings _settings;
    private readonly DispatcherTimer _statsTimer;
    private List<CleanupGroup> _groups = [];
    private bool _isBusy;

    public MainWindow()
    {
        InitializeComponent();
        _settings = _settingsService.Load();
        SelectPage("dashboard");
        RefreshAnimationStatus();
        RefreshStats();

        _statsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _statsTimer.Tick += (_, _) => RefreshStats();
        _statsTimer.Start();
        Closed += (_, _) => _statsTimer.Stop();
    }

    private void SelectPage(string page)
    {
        DashboardPage.Visibility = page == "dashboard" ? Visibility.Visible : Visibility.Collapsed;
        CleanPage.Visibility = page == "clean" ? Visibility.Visible : Visibility.Collapsed;
        GamesPage.Visibility = page == "games" ? Visibility.Visible : Visibility.Collapsed;
        SettingsPage.Visibility = page == "settings" ? Visibility.Visible : Visibility.Collapsed;

        (PageTitle.Text, PageSubTitle.Text) = page switch
        {
            "clean" => ("Limpieza inteligente", "Controlá el almacenamiento sin tocar tus partidas"),
            "games" => ("Mis juegos", "Bibliotecas detectadas en esta PC"),
            "settings" => ("Ajustes", "Preferencias seguras y reversibles"),
            _ => ("Panel principal", "Estado de tu PC en tiempo real")
        };

        Brush selected = new SolidColorBrush(Color.FromRgb(20, 64, 116));
        Brush normal = Brushes.Transparent;
        NavDashboard.Background = page == "dashboard" ? selected : normal;
        NavClean.Background = page == "clean" ? selected : normal;
        NavGames.Background = page == "games" ? selected : normal;
        NavSettings.Background = page == "settings" ? selected : normal;
    }

    private void NavDashboard_Click(object sender, RoutedEventArgs e) => SelectPage("dashboard");
    private void NavClean_Click(object sender, RoutedEventArgs e) => SelectPage("clean");
    private void NavGames_Click(object sender, RoutedEventArgs e) => SelectPage("games");
    private void NavSettings_Click(object sender, RoutedEventArgs e) => SelectPage("settings");
    private void GoClean_Click(object sender, RoutedEventArgs e) => SelectPage("clean");
    private void GoGames_Click(object sender, RoutedEventArgs e) => SelectPage("games");

    private async void StartAnalyze_Click(object sender, RoutedEventArgs e)
    {
        SelectPage("clean");
        await AnalyzeAsync();
    }

    private async void Analyze_Click(object sender, RoutedEventArgs e) => await AnalyzeAsync();

    private async Task AnalyzeAsync()
    {
        if (_isBusy) return;
        SetBusy(true);
        CleanupStatus.Text = "Analizando archivos permitidos...";
        try
        {
            _groups = await Task.Run(() => _cleaner.Scan());
            CleanupGroupsList.ItemsSource = _groups;
            long size = _groups.Sum(group => group.Bytes);
            int count = _groups.Sum(group => group.Files.Count);
            CleanupStatus.Text = $"Análisis terminado: {count:N0} archivos candidatos ({CleanupGroup.FormatBytes(size)}). Elegí las categorías antes de limpiar.";
        }
        catch (Exception ex)
        {
            CleanupStatus.Text = "No se pudo terminar el análisis: " + ex.Message;
        }
        finally { SetBusy(false); }
    }

    private async void Clean_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy) return;
        var selected = _groups.Where(group => group.IsSelected).SelectMany(group => group.Files).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show(this, "Primero seleccioná una categoría que tenga archivos antiguos.",
                "OPoptimizer", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        long bytes = selected.Sum(file => file.Length);
        string message = $"Vas a borrar permanentemente {selected.Count:N0} archivos ({CleanupGroup.FormatBytes(bytes)}).\n\n" +
            "No se borrarán carpetas, instalaciones, mundos ni partidas guardadas. " +
            "Los archivos recientes o modificados desde el análisis se omiten.\n\n¿Continuar?";
        if (MessageBox.Show(this, message, "Confirmar limpieza", MessageBoxButton.YesNo,
                MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes)
            return;

        SetBusy(true);
        CleanupStatus.Text = "Limpiando los archivos confirmados...";
        try
        {
            var result = await Task.Run(() => _cleaner.Clean(selected));
            // Refresh the candidates after cleaning; no further deletion occurs.
            _groups = await Task.Run(() => _cleaner.Scan());
            CleanupGroupsList.ItemsSource = null;
            CleanupGroupsList.ItemsSource = _groups;
            CleanupStatus.Text = $"Limpieza finalizada: {result.Deleted:N0} archivos borrados, " +
                $"{CleanupGroup.FormatBytes(result.FreedBytes)} liberados, {result.Skipped:N0} omitidos.";
            RefreshStats();
        }
        catch (Exception ex)
        {
            CleanupStatus.Text = "La limpieza se interrumpió: " + ex.Message;
        }
        finally { SetBusy(false); }
    }

    private void SetBusy(bool busy)
    {
        _isBusy = busy;
        BtnAnalyze.IsEnabled = !busy;
        BtnClean.IsEnabled = !busy && _groups.Any(group => group.Files.Count > 0);
    }

    private void RefreshStats()
    {
        try
        {
            SystemStats sample = _stats.Sample();
            CpuValue.Text = $"{sample.CpuPercent:0}%";
            CpuProgress.Value = sample.CpuPercent;
            RamValue.Text = $"{sample.RamPercent:0}%";
            RamProgress.Value = sample.RamPercent;
            RamDetail.Text = sample.RamUsedText;
            DiskValue.Text = $"{sample.DiskPercent:0}%";
            DiskProgress.Value = sample.DiskPercent;
            DiskDetail.Text = sample.DiskFreeText;
        }
        catch (Exception) { /* Stats are informative; don't crash the rest of the app. */ }
    }

    private async void ScanGames_Click(object sender, RoutedEventArgs e)
    {
        BtnScanGames.IsEnabled = false;
        GameStatus.Text = "Buscando instalaciones conocidas...";
        try
        {
            var games = await Task.Run(() => _gameDiscovery.FindGames());
            GamesList.ItemsSource = games;
            GameStatus.Text = games.Count > 0
                ? $"{games.Count} instalaciones detectadas. Puede haber otras que no estén en estas bibliotecas."
                : "No se encontraron juegos en las bibliotecas compatibles. No se cambió ningún archivo.";
        }
        catch (Exception ex) { GameStatus.Text = "No se pudo completar la búsqueda: " + ex.Message; }
        finally { BtnScanGames.IsEnabled = true; }
    }

    private void OpenGameFolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string path }) return;
        try
        {
            if (!Directory.Exists(path)) throw new DirectoryNotFoundException("La carpeta ya no existe.");
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "No se pudo abrir la carpeta: " + ex.Message,
                "OPoptimizer", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void RefreshAnimationStatus()
    {
        if (WindowsAnimationsService.TryGetEnabled(out bool enabled))
        {
            AnimationStatus.Text = enabled ? "Estado actual: animaciones activadas" : "Estado actual: animaciones reducidas";
            BtnReduceAnimations.IsEnabled = enabled;
        }
        else
        {
            AnimationStatus.Text = "Windows no permitió leer esta configuración.";
            BtnReduceAnimations.IsEnabled = false;
        }
        BtnRestoreAnimations.IsEnabled = _settings.OriginalClientAnimationsEnabled.HasValue;
    }

    private void ReduceAnimations_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!WindowsAnimationsService.TryGetEnabled(out bool previous))
                throw new InvalidOperationException("No se pudo leer el estado actual.");
            if (!previous) { RefreshAnimationStatus(); return; }
            // Save the original value BEFORE changing Windows, to make undo reliable.
            if (!_settings.OriginalClientAnimationsEnabled.HasValue)
            {
                _settings.OriginalClientAnimationsEnabled = previous;
                _settingsService.Save(_settings);
            }
            if (!WindowsAnimationsService.TrySetEnabled(false))
                throw new InvalidOperationException("Windows rechazó el cambio.");
            RefreshAnimationStatus();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "No se pudieron cambiar las animaciones: " + ex.Message,
                "OPoptimizer", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RestoreAnimations_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            bool? original = _settings.OriginalClientAnimationsEnabled;
            if (!original.HasValue) return;
            if (!WindowsAnimationsService.TrySetEnabled(original.Value))
                throw new InvalidOperationException("Windows rechazó la restauración.");
            _settings.OriginalClientAnimationsEnabled = null;
            _settingsService.Save(_settings);
            RefreshAnimationStatus();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "No se pudo restaurar la configuración: " + ex.Message,
                "OPoptimizer", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OpenGamingSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = "ms-settings:gaming-gamemode", UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "No se pudo abrir Configuración: " + ex.Message,
                "OPoptimizer", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
