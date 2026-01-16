using CyanTooth.Platform.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CyanTooth.Core.Services;
using CyanTooth.ViewModels;
using Wpf.Ui.Appearance;
using System.Collections.Generic;

namespace CyanTooth;

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
        // 关键调试：如果在极早期崩溃，显示消息框
        // MessageBox.Show("CyanTooth 正在进入构造函数...", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);

        // 第一步：立即挂载异常处理器
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            HandleFatalException(ex, "AppDomain");
        };
        
        DispatcherUnhandledException += (s, e) =>
        {
            HandleFatalException(e.Exception, "Dispatcher");
            e.Handled = true;
        };

        // 第二步：显式初始化日志
        DebugLogger.Initialize();
        DebugLogger.Log("==== App Instance Created ====");

        try
        {
            DebugLogger.Log("开始配置依赖注入...");
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<ConfigService>();
                    services.AddSingleton<BluetoothService>();
                    services.AddSingleton<NotificationService>();
                    services.AddSingleton<MainViewModel>();
                    services.AddTransient<SettingsViewModel>();
                    services.AddSingleton<TrayIconViewModel>();
                })
                .Build();
            DebugLogger.Log("DI 容器构建完成");
        }
        catch (Exception ex)
        {
            HandleFatalException(ex, "HostBuilder");
            throw;
        }
    }

    private void HandleFatalException(Exception? ex, string source)
    {
        string msg = ex?.Message ?? "未知错误";
        DebugLogger.LogError($"[{source}] 致命错误: {msg}", ex);
        
        // 确保在闪退前给用户提示
        MessageBox.Show($"程序发生异常 ({source}):\n{msg}\n\n详细日志请查看: %LocalAppData%\\CyanTooth\\logs\\debug.log", 
                        "CyanTooth 错误", MessageBoxButton.OK, MessageBoxImage.Error);
        
        if (source != "DispatcherUnhandledException")
        {
            Environment.Exit(1);
        }
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        DebugLogger.Log("OnStartup 开始执行");
        try
        {
            DebugLogger.Log("正在启动 Host...");
            await _host.StartAsync();
            DebugLogger.Log("Host 已启动");

            base.OnStartup(e);

            DebugLogger.Log("正在获取配置并应用主题...");
            var configService = Services.GetRequiredService<ConfigService>();
            ApplyTheme(configService.Settings.Theme);

            DebugLogger.Log("正在初始化蓝牙服务...");
            var bluetoothService = Services.GetRequiredService<BluetoothService>();
            var mainViewModel = Services.GetRequiredService<MainViewModel>();
            mainViewModel.Initialize();

            DebugLogger.Log("正在创建托盘图标...");
            CreateTrayIcon();

            bool startMinimized = e.Args.Contains("--minimized") || configService.Settings.StartMinimized;
            DebugLogger.Log($"启动模式: {(startMinimized ? "最小化" : "标准")}");

            if (!startMinimized)
            {
                DebugLogger.Log("正在显示主界面 (Flyout)...");
                ShowFlyout();
            }
            
            DebugLogger.Log("程序进入空闲循环");
        }
        catch (Exception ex)
        {
            HandleFatalException(ex, "Startup阶段");
            Shutdown();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        DebugLogger.Log("程序正在退出...");
        _trayIcon?.Dispose();
        var bluetoothService = Services.GetService<BluetoothService>();
        bluetoothService?.Dispose();
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }

    private void CreateTrayIcon()
    {
        try
        {
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

            var trayViewModel = Services.GetRequiredService<TrayIconViewModel>();
            _trayIcon.DataContext = trayViewModel;
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("创建托盘图标失败", ex);
        }
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
            if (_flyoutWindow == null || !_flyoutWindow.IsLoaded)
            {
                _flyoutWindow = new Views.FlyoutWindow();
            }

            if (_flyoutWindow.IsVisible)
            {
                _flyoutWindow.Hide();
            }
            else
            {
                PositionFlyoutWindow(_flyoutWindow);
                _flyoutWindow.Show();
                _flyoutWindow.Activate();
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("显示 Flyout 失败", ex);
            MessageBox.Show($"显示 Flyout 失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowSettings()
    {
        try
        {
            var settingsWindow = new Views.SettingsWindow();
            settingsWindow.Show();
            settingsWindow.Activate();
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("显示设置窗口失败", ex);
            MessageBox.Show($"显示设置窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
        catch (Exception ex)
        {
            DebugLogger.LogError("打开系统蓝牙设置失败", ex);
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
            ApplicationThemeManager.ApplySystemTheme();
        }
        else
        {
            ApplicationThemeManager.Apply(targetTheme);
        }
    }
}
