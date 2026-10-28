using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using fasterLegit.Common.Extensions;
using fasterLegit.Common.Helpers;
using fasterLegit.Domain.Configuration;
using fasterLegit.Domain.UI;
using fasterLegit.Resources.Languages;
using fasterLegit.Services.Configuration;
using fasterLegit.Services.Optimization;
using fasterLegit.Services.Revert;
using fasterLegit.Services.System;
using fasterLegit.Services.System.Primitives;
using fasterLegit.Services.UI;
using fasterLegit.UI.Behaviors;
using fasterLegit.UI.Pages;
using fasterLegit.UI.ViewModels.Pages;
using fasterLegit.UI.ViewModels.Windows;
using fasterLegit.UI.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Parsing;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.DependencyInjection;
using Wpf.Ui.Extensions;

namespace fasterLegit;

public class ScopeBlockTextFormatter : ITextFormatter
{
    public void Format(LogEvent logEvent, TextWriter output)
    {
        var timestamp = $"{logEvent.Timestamp:yyyy-MM-dd HH:mm:ss}";
        var ctx = logEvent.Properties.TryGetValue("SourceContext", out var sourceContext)
            ? sourceContext is ScalarValue { Value: string s }
                ? s.Split(".")[^1]
                : sourceContext.ToString().Split(".")[^1]
            : "";
        var levelText = logEvent.Level switch
        {
            LogEventLevel.Verbose => "VERBOSE",
            LogEventLevel.Debug => "DEBUG",
            LogEventLevel.Information => "INFO",
            LogEventLevel.Warning => "WARNING",
            LogEventLevel.Error => "ERROR",
            LogEventLevel.Fatal => "FATAL",
            _ => "UNKNOWN",
        };

        //var prefix = $"{timestamp} | {ctx,-67} | {levelText,-7} | "; // byebye 67 char SourceContext truncation, we have a new design now...
        var prefix = $"{timestamp} | {ctx, -35} | {levelText, -7} | ";

        output.WriteLine(prefix + RenderWithoutQuotes(logEvent));

        foreach (var kvp in logEvent.Properties)
        {
            if (kvp.Key == "SourceContext")
                continue;

            var usedInMessage = logEvent
                .MessageTemplate.Tokens.OfType<PropertyToken>()
                .Any(t => t.PropertyName == kvp.Key);
            if (usedInMessage)
                continue;

            var valueText = kvp.Value is ScalarValue sv
                ? sv.Value?.ToString() ?? ""
                : kvp.Value.ToString();

            output.WriteLine(new string(' ', prefix.Length) + $"{kvp.Key} = {valueText}");
        }

        if (logEvent.Exception != null)
        {
            output.WriteLine("Exception : ");
            output.WriteLine($"    {logEvent.Exception}");
        }
    }

    private static string RenderWithoutQuotes(LogEvent logEvent)
    {
        var output = new StringWriter();
        foreach (var token in logEvent.MessageTemplate.Tokens)
            if (
                token is PropertyToken pt
                && logEvent.Properties.TryGetValue(pt.PropertyName, out var value)
            )
            {
                if (value is ScalarValue { Value: string s })
                    output.Write(s);
                else
                    pt.Render(logEvent.Properties, output);
            }
            else
            {
                token.Render(logEvent.Properties, output);
            }

        return output.ToString();
    }
}

public partial class App : Application
{
    private IHost? _host;

    public IHost? AppHost => _host;
    private ILogger<App> _logger = null!;
    private bool _allowClose;
    private IContentDialogService? _contentDialogService = null;

    private bool _hasPendingChanges;

    public bool HasPendingChanges
    {
        get => _hasPendingChanges;
        set
        {
            if (_hasPendingChanges == value)
                return;
            _hasPendingChanges = value;
            Dispatcher.InvokeAsync(() =>
            {
                if (Current.MainWindow is MainWindow mw)
                    mw.UpdatePendingIndicator(value);
            });
        }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        _ = SafeFireAndForgetAsync(() => OnStartupAsync(e));
    }

    /// <summary>
    ///     Applies the VELORA teal accent palette tuned for the given theme. Light
    ///     uses deeper teal tones so accent text and fills stay readable on white;
    ///     dark keeps the brighter mint tones. Centralized so startup and theme
    ///     switches always use the same palettes.
    /// </summary>
    internal static void ApplyAccentFor(ApplicationTheme theme)
    {
        ApplyBrandSurfaces(theme == ApplicationTheme.Dark);
        if (theme == ApplicationTheme.Dark)
        {
            ApplicationAccentColorManager.Apply(
                systemAccent: Color.FromRgb(33, 169, 143),
                primaryAccent: Color.FromRgb(84, 222, 193),
                secondaryAccent: Color.FromRgb(124, 235, 211),
                tertiaryAccent: Color.FromRgb(49, 189, 160)
            );
            RefreshToggleSwitchBrushes();
            return;
        }

        ApplicationAccentColorManager.Apply(
            systemAccent: Color.FromRgb(0, 107, 89),
            primaryAccent: Color.FromRgb(0, 107, 89),
            secondaryAccent: Color.FromRgb(0, 94, 78),
            tertiaryAccent: Color.FromRgb(0, 80, 67)
        );
        RefreshToggleSwitchBrushes();
    }

    private static void ApplyBrandSurfaces(bool dark)
    {
        var resources = Application.Current.Resources;
        void Brush(string key, string value) =>
            resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));

        Brush("ApplicationBackgroundBrush", dark ? "#0B1216" : "#EDF4F2");
        Brush("NavigationViewContentBackground", dark ? "#10191E" : "#F6FAF9");
        Brush("CardBackgroundFillColorDefaultBrush", dark ? "#172329" : "#FFFFFF");
        Brush("CardBackgroundFillColorSecondaryBrush", dark ? "#1D2D34" : "#EAF2EF");
        Brush("CardStrokeColorDefaultBrush", dark ? "#2C4149" : "#C9DCD5");
        Brush("NavigationViewContentGridBorderBrush", dark ? "#2C4149" : "#C9DCD5");
        Brush("TextFillColorPrimaryBrush", dark ? "#ECF5F2" : "#142D29");
        Brush("TextFillColorSecondaryBrush", dark ? "#AEC3BE" : "#49665F");
        Brush("BrandAccentBrush", dark ? "#54DEC1" : "#006B59");
        Brush("BrandOnAccentBrush", dark ? "#07251F" : "#FFFFFF");
        Brush("BrandAccentMutedBrush", dark ? "#183D36" : "#D7F3E9");
    }

    private static void RefreshToggleSwitchBrushes()
    {
        var app = Application.Current;
        if (app == null)
            return;

        Color GetColor(string key, Color fallback) =>
            app.Resources[key] is Color color ? color : fallback;

        var primary = GetColor("SystemAccentColorPrimary", Color.FromRgb(84, 222, 193));
        var secondary = GetColor("AccentFillColorSecondary", Color.FromRgb(124, 235, 211));
        var tertiary = GetColor("AccentFillColorTertiary", Color.FromRgb(49, 189, 160));
        var knobOn = GetColor("TextOnAccentFillColorPrimary", Colors.White);

        app.Resources["ToggleSwitchFillOn"] = new SolidColorBrush(primary);
        app.Resources["ToggleSwitchFillOnPointerOver"] = new SolidColorBrush(secondary);
        app.Resources["ToggleSwitchFillOnPressed"] = new SolidColorBrush(tertiary);
        app.Resources["ToggleSwitchStrokeOn"] = new SolidColorBrush(primary);
        app.Resources["ToggleSwitchStrokeOnPointerOver"] = new SolidColorBrush(secondary);
        app.Resources["ToggleSwitchStrokeOnPressed"] = new SolidColorBrush(tertiary);
        app.Resources["ToggleSwitchKnobFillOn"] = new SolidColorBrush(knobOn);
        app.Resources["ToggleSwitchKnobFillOnPointerOver"] = new SolidColorBrush(knobOn);
        app.Resources["ToggleSwitchKnobFillOnPressed"] = new SolidColorBrush(knobOn);
    }

    private static async Task SafeFireAndForgetAsync(Func<Task> taskFactory)
    {
        try
        {
            await taskFactory();
        }
        catch (Exception ex)
        {
            try
            {
                await HandleStartupErrorStaticAsync(ex);
            }
            catch { }
        }
    }

    private static async Task HandleStartupErrorStaticAsync(Exception ex)
    {
        LogExceptionToFile("App.Startup", ex);

        try
        {
            Log.Logger?.Fatal(ex, "Fatal error during startup");
            await Log.CloseAndFlushAsync();
        }
        catch
        {
            // ignore logging failures during fatal startup handling
        }

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            System.Windows.MessageBox.Show(
                $"Failed to start fasterLegit.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "fasterLegit",
                System.Windows.MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            Application.Current.Shutdown(-1);
        });
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString());
        LogExceptionToFile("AppDomain.UnhandledException", ex);
        Log.Logger?.Fatal(
            ex,
            "Unhandled AppDomain exception. IsTerminating={IsTerminating}",
            e.IsTerminating
        );
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogExceptionToFile("UnobservedTaskException", e.Exception);
        Log.Logger?.Error(e.Exception, "Unobserved task exception");
        e.SetObserved();
    }

    private void OnDispatcherUnhandledException(
        object sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e
    )
    {
        LogExceptionToFile("DispatcherUnhandledException", e.Exception);
        Log.Logger?.Error(e.Exception, "Unhandled UI dispatcher exception");

        // The exception is swallowed below, so the user is told about it here.
        try
        {
            _host?.Services.GetService<UserErrorSurface>()?.TryReport(e.Exception);
        }
        catch (Exception ex)
        {
            Log.Logger?.Warning(ex, "Could not report the unhandled exception to the user");
        }

        e.Handled = true;
    }

    private static void LogExceptionToFile(string category, Exception ex)
    {
        try
        {
            var crashDir = Path.Combine(Shared.RootDirectory, "Crashes");
            Directory.CreateDirectory(crashDir);
            var crashFile = Path.Combine(
                crashDir,
                $"crash_{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}.log"
            );
            File.WriteAllText(
                crashFile,
                $"""
                [{category}] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}
                Type: {ex.GetType().FullName}
                Message: {ex.Message}
                StackTrace:
                {ex.StackTrace}

                {(ex.InnerException is not null ? $"Inner: {ex.InnerException}" : "")}
                """
            );
        }
        catch
        {
            // cannot do anything if crash logging itself fails
        }
    }

    private async Task OnStartupAsync(StartupEventArgs e)
    {
        Directory.CreateDirectory(Shared.RootDirectory);
        Directory.CreateDirectory(Shared.ResourcesDirectory);
        Directory.CreateDirectory(Shared.DownloadsDirectory);
        Directory.CreateDirectory(Shared.AssetsDirectory);
        Directory.CreateDirectory(Shared.RevertDirectory);
        Directory.CreateDirectory(Shared.HistoryDirectory);

        var logPath = Path.Combine(Shared.RootDirectory, "fasterLegit.log");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.File(
                new ScopeBlockTextFormatter(),
                logPath,
                rollingInterval: RollingInterval.Infinite
            )
            .CreateLogger();

        _host = Microsoft
            .Extensions.Hosting.Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureAppConfiguration(c =>
            {
                using var loggerFactory = LoggerFactory.Create(b => b.AddSerilog(Log.Logger));
                ConfigManager.ValidateConfig(loggerFactory.CreateLogger(typeof(ConfigManager)));
                c.AddJsonFile(Path.Combine(Shared.RootDirectory, "appsettings.json"), false, true);
            })
            // Fail fast instead of discovering a broken graph later: ValidateOnBuild catches
            // an unbuildable registration, ValidateScopes catches a scoped service leaking
            // into a singleton (the ChangeSet must never be resolved from the root provider).
            .UseDefaultServiceProvider(options =>
            {
                options.ValidateOnBuild = true;
                options.ValidateScopes = true;
            })
            .ConfigureServices(
                (context, services) => services.AddOptimizerApplication(context.Configuration)
            )
            .Build();

        await _host.StartAsync();

        var config = _host.Services.GetRequiredService<ConfigManager>();
        await config.InitializeAsync();
        await config.EnsureDefaultsAsync();

        var appOptionsMonitor = _host.Services.GetRequiredService<IOptionsMonitor<AppSettings>>();

        // init WMI helper for cleanup on abnormal termination
        WmiHelper.Initialize();

        var appSettings = appOptionsMonitor.CurrentValue;

        SmoothScrollBehavior.GlobalEnabled = appSettings.Optimize.SmoothScrolling;

        await Dispatcher.InvokeAsync(() =>
        {
            Loc.Instance.ChangeCulture(SupportedLanguages.Default.Culture);
        });

        _logger = _host.Services.GetRequiredService<ILogger<App>>();
        _logger.LogInformation(
            "\n{Logo}\nVersion: {Version}\n\n",
            Shared.RawLogo,
            Shared.FileVersion
        );
        _logger.LogInformation(
            "Loaded language: {Language}",
            SupportedLanguages.Default.Culture.Name
        );

        var optimizationRegistry = _host.Services.GetRequiredService<OptimizationRegistry>();

        await Dispatcher.InvokeAsync(() =>
        {
            // Theme first: ApplyAccentFor reads GetAppTheme() cache to pick
            // AccentFillColorDefault (secondary for Dark, primary for Light).
            ApplicationThemeManager.Apply(
                appSettings.App.Theme switch
                {
                    ApplicationTheme.Dark => ApplicationTheme.Dark,
                    ApplicationTheme.HighContrast => ApplicationTheme.HighContrast,
                    _ => ApplicationTheme.Light,
                },
                updateAccent: false
            );
            ApplyAccentFor(appSettings.App.Theme);

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Closing += MainWindow_Closing;
            mainWindow.Show();
        });

        _logger.LogInformation("Preloading optimizations...");
        await optimizationRegistry.PreloadOptimizationsAsync().ConfigureAwait(false);

        RevertManager.RemoveOrphanedTempFiles(_logger);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }

            WmiHelper.DisposeScopes();
            Log.CloseAndFlush();
        }
        catch (Exception ex)
        {
            LogExceptionToFile("App.OnExit", ex);
        }

        base.OnExit(e);
    }

    protected async void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        try
        {
            if (_allowClose)
            {
                return;
            }

            if (!HasPendingChanges)
            {
                return;
            }

            e.Cancel = true;

            _contentDialogService ??= _host!.Services.GetRequiredService<IContentDialogService>();

            var result = await _contentDialogService.ShowSimpleDialogAsync(
                new SimpleContentDialogCreateOptions
                {
                    Title = Loc.Instance["Dialog.PendingChanges.Title"],
                    Content = Loc.Instance["Dialog.PendingChanges.Content"],
                    CloseButtonText = Loc.Instance["Dialog.PendingChanges.CloseButton"],
                    PrimaryButtonText = Loc.Instance["Dialog.PendingChanges.PrimaryButton"],
                    SecondaryButtonText = Loc.Instance["Dialog.PendingChanges.SecondaryButton"],
                }
            );

            switch (result)
            {
                case ContentDialogResult.Primary:
                    _logger.LogInformation("User chose to restart PC.");

                    await _host!
                        .Services.GetRequiredService<ShellService>()
                        .QueryCMDAsync("shutdown /r /t 0");
                    break;

                case ContentDialogResult.Secondary:
                    _logger.LogInformation("User chose to restart Explorer.");

                    await _host!
                        .Services.GetRequiredService<ShellService>()
                        .QueryCMDAsync("taskkill /f /im explorer.exe && start explorer.exe");
                    _allowClose = true;
                    Current.Shutdown();
                    break;

                case ContentDialogResult.None:
                    _logger.LogInformation("User chose to exit without applying changes.");

                    _allowClose = true;
                    Current.Shutdown();
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to handle window closing event. Allowing close to prevent hanging."
            );

            // Ensure we don't block close on error
            _allowClose = true;
            e.Cancel = false;
        }
    }
}
