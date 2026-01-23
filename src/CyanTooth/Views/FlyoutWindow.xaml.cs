using CyanTooth.Platform.Helpers;


using System;
using System.Windows;
using CyanTooth.ViewModels;
using Microsoft.Extensions.DependencyInjection;


namespace CyanTooth.Views;

/// <summary>
/// Flyout window that appears when clicking the tray icon
/// </summary>
public partial class FlyoutWindow : Wpf.Ui.Controls.FluentWindow
{
    public FlyoutWindow()
    {
        DebugLogger.Log("FlyoutWindow: 正在初始化...");
        try
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetRequiredService<MainViewModel>();
            DebugLogger.Log("FlyoutWindow: 初始化完成。");
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("FlyoutWindow 初始化失败", ex);
            throw;
        }
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        // Hide window when it loses focus
        Hide();
    }

    private void Detail_Click(object sender, RoutedEventArgs e)
    {
        // Close flyout and open DetailWindow
        Hide();
        
        // Use reflection to call App.ShowDetailWindow if available, or just use App instance cast
        if (Application.Current is App app)
        {
            // We will implement this method in App.xaml.cs later
            // app.ShowDetailWindow();
            var method = typeof(App).GetMethod("ShowDetailWindow");
            method?.Invoke(app, null);
        }
    }

    private void OpenWindowsSettings_Click(object sender, RoutedEventArgs e)
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
            DebugLogger.LogError("打开系统设置失败", ex);
        }
    }
}
