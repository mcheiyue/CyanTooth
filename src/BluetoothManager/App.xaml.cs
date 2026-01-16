using System;
using System.Linq;
using System.Threading.Tasks;
using BluetoothManager.Core.Helpers;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BluetoothManager.Core.Services;
using BluetoothManager.ViewModels;
using Wpf.Ui.Appearance;

namespace BluetoothManager;

/// <summary>
/// Main application class
/// </summary>
public partial class App : System.Windows.Application
{
    private readonly IHost _host;
    private Hardcodet.Wpf.TaskbarNotification.TaskbarIcon? _trayIcon;

    public IServiceProvider Services => _host.Services;

    public new static App Current => (App)System.Windows.Application.Current;

    public App()
    {
        // Global exception handlers
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            DebugLogger.LogError("[UnhandledException]", ex);
            System.Windows.MessageBox.Show($"致命错误:\n{ex?.Message}\n\n请在 AppData 目录中查看日志获取详细信息。", "程序崩溃", MessageBoxButton.OK, MessageBoxImage.Error);
        };
        
        DispatcherUnhandledException += (s, e) =>
        {
            DebugLogger.LogError("[DispatcherUnhandledException]", e.Exception);
            System.Windows.MessageBox.Show($"UI 错误:\n{e.Exception.Message}\n\n请在 AppData 目录中查看日志获取详细信息。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        };
        
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            DebugLogger.LogError("[UnobservedTaskException]", e.Exception);
            e.SetObserved();
        };

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Core Services
                services.AddSingleton<ConfigService>();
                services.AddSingleton<BluetoothService>();
                services.AddSingleton<NotificationService>();

                // ViewModels
                services.AddSingleton<MainViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddSingleton<TrayIconViewModel>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();
        base.OnStartup(e);

        // Apply theme based on settings
        var configService = Services.GetRequiredService<ConfigService>();
        ApplyTheme(configService.Settings.Theme);

        // Initialize Bluetooth service
        var bluetoothService = Services.GetRequiredService<BluetoothService>();
        var mainViewModel = Services.GetRequiredService<MainViewModel>();
        mainViewModel.Initialize();

        // Create tray icon
        CreateTrayIcon();

        // Check if started with --minimized flag
        bool startMinimized = e.Args.Contains("--minimized") || configService.Settings.StartMinimized;

        if (!startMinimized)
        {
            // Show the flyout window on first start
            ShowFlyout();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        
        var bluetoothService = Services.GetRequiredService<BluetoothService>();
        bluetoothService.Dispose();

        await _host.StopAsync();
        _host.Dispose();

        base.OnExit(e);
    }

    private void CreateTrayIcon()
    {
        // Load icon from embedded resource
        var iconUri = new Uri("pack://application:,,,/Resources/Icons/tray.ico");
        var iconStream = System.Windows.Application.GetResourceStream(iconUri)?.Stream;
        
        _trayIcon = new Hardcodet.Wpf.TaskbarNotification.TaskbarIcon
        {
            Icon = iconStream != null ? new System.Drawing.Icon(iconStream) : System.Drawing.SystemIcons.Application,
            ToolTipText = "CyanTooth",
            ContextMenu = CreateContextMenu()
        };

        _trayIcon.TrayMouseDoubleClick += (s, e) => ShowFlyout();
        _trayIcon.TrayLeftMouseUp += (s, e) => ShowFlyout();

        // Update tooltip with device info
        var trayViewModel = Services.GetRequiredService<TrayIconViewModel>();
        _trayIcon.DataContext = trayViewModel;
    }

    private System.Windows.Controls.ContextMenu CreateContextMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();

        var openItem = new System.Windows.Controls.MenuItem { Header = "打开" };
        openItem.Click += (s, e) => ShowFlyout();
        menu.Items.Add(openItem);

        var refreshItem = new System.Windows.Controls.MenuItem { Header = "刷新设备" };
        refreshItem.Click += (s, e) => Services.GetRequiredService<BluetoothService>().RefreshDevices();
        menu.Items.Add(refreshItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var settingsItem = new System.Windows.Controls.MenuItem { Header = "设置" };
        settingsItem.Click += (s, e) => ShowSettings();
        menu.Items.Add(settingsItem);

        var windowsSettingsItem = new System.Windows.Controls.MenuItem { Header = "系统蓝牙设置" };
        windowsSettingsItem.Click += (s, e) => OpenWindowsBluetoothSettings();
        menu.Items.Add(windowsSettingsItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var exitItem = new System.Windows.Controls.MenuItem { Header = "退出" };
        exitItem.Click += (s, e) => Shutdown();
        menu.Items.Add(exitItem);

        return menu;
    }

    private Views.FlyoutWindow? _flyoutWindow;

    private void ShowFlyout()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] ShowFlyout called");
            if (_flyoutWindow == null || !_flyoutWindow.IsLoaded)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] Creating new FlyoutWindow");
                _flyoutWindow = new Views.FlyoutWindow();
                System.Diagnostics.Debug.WriteLine("[DEBUG] FlyoutWindow created successfully");
            }

            if (_flyoutWindow.IsVisible)
            {
                _flyoutWindow.Hide();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] Positioning FlyoutWindow");
                PositionFlyoutWindow(_flyoutWindow);
                System.Diagnostics.Debug.WriteLine("[DEBUG] Showing FlyoutWindow");
                _flyoutWindow.Show();
                _flyoutWindow.Activate();
                System.Diagnostics.Debug.WriteLine("[DEBUG] FlyoutWindow shown");
            }
        }
        catch (Exception ex)
        {
            if (ex is not null)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] ShowFlyout failed: {ex}");
                System.Windows.MessageBox.Show($"ShowFlyout 错误:\n{ex.Message}\n\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ShowSettings()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] ShowSettings called");
            var settingsWindow = new Views.SettingsWindow();
            System.Diagnostics.Debug.WriteLine("[DEBUG] SettingsWindow created");
            settingsWindow.Show();
            settingsWindow.Activate();
            System.Diagnostics.Debug.WriteLine("[DEBUG] SettingsWindow shown");
        }
        catch (Exception ex)
        {
            if (ex is not null)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] ShowSettings failed: {ex}");
                System.Windows.MessageBox.Show($"ShowSettings 错误:\n{ex.Message}\n\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private static void OpenWindowsBluetoothSettings()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ms-settings:bluetooth",
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore errors
        }
    }

    private static void PositionFlyoutWindow(Window window)
    {
        var workArea = SystemParameters.WorkArea;
        window.Left = workArea.Right - window.Width - 10;
        window.Top = workArea.Bottom - window.Height - 10;
    }

    private static void ApplyTheme(Core.Models.AppTheme theme)
    {
        var targetTheme = theme switch
        {
            Core.Models.AppTheme.Light => ApplicationTheme.Light,
            Core.Models.AppTheme.Dark => ApplicationTheme.Dark,
            _ => ApplicationTheme.Unknown
        };

        if (targetTheme == ApplicationTheme.Unknown)
        {
            // Use system theme
            ApplicationThemeManager.ApplySystemTheme();
        }
        else
        {
            ApplicationThemeManager.Apply(targetTheme);
        }
    }
}
