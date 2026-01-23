using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using CyanTooth.ViewModels;
using CyanTooth.Platform.Helpers;

namespace CyanTooth.Views.Pages;

public partial class DevicePage : Page
{
    public DevicePage()
    {
        InitializeComponent();
        try
        {
            // Explicitly resolve MainViewModel (which is the context for DeviceList)
            DataContext = App.Current.Services.GetRequiredService<MainViewModel>();
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("DevicePage DataContext resolution failed", ex);
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
