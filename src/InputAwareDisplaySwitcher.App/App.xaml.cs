using System.IO;
using System.Windows;
using InputAwareDisplaySwitcher.Core.Application;
using InputAwareDisplaySwitcher.Core.Domain.Diagnostics;
using InputAwareDisplaySwitcher.Infrastructure.Configuration;
using InputAwareDisplaySwitcher.Infrastructure.Diagnostics;
using InputAwareDisplaySwitcher.App.ViewModels;

namespace InputAwareDisplaySwitcher.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var diagnostics = new DiagnosticsService();
        diagnostics.Record(
            DiagnosticCategories.Application,
            DiagnosticEventTypes.ApplicationStarted,
            "Application startup began.");

        try
        {
            var configurationPath = GetConfigurationPath();
            diagnostics.Record(
                DiagnosticCategories.Application,
                DiagnosticEventTypes.ConfigurationPathSelected,
                "Application configuration path selected.",
                details: new Dictionary<string, string?>
                {
                    ["configurationPath"] = configurationPath
                });

            var configurationStore = new JsonAppConfigurationStore(configurationPath, diagnostics);
            var configurationService = new ApplicationConfigurationService(configurationStore);
            var configuration = await configurationService.LoadAsync();

            var mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(configuration, diagnostics, configurationPath)
            };

            diagnostics.Record(
                DiagnosticCategories.Application,
                DiagnosticEventTypes.ShellInitialized,
                "Application shell created.",
                details: new Dictionary<string, string?>
                {
                    ["configurationPath"] = configurationPath
                });

            MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception exception)
        {
            diagnostics.Record(
                DiagnosticCategories.Application,
                DiagnosticEventTypes.ApplicationStartupFailed,
                "Application startup failed.",
                DiagnosticSeverity.Error,
                new Dictionary<string, string?>
                {
                    ["error"] = exception.Message
                });

            MessageBox.Show(
                $"The application could not start.{Environment.NewLine}{Environment.NewLine}{exception.Message}",
                "Input Aware Display Switcher",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(-1);
        }
    }

    private static string GetConfigurationPath()
    {
        var appDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appDataRoot, "InputAwareDisplaySwitcher", "config.json");
    }
}
