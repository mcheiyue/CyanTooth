using System;
using System.Windows;
using BluetoothManager.ViewModels;

namespace BluetoothManager.Views;

/// <summary>
/// Flyout window that appears when clicking the tray icon
/// </summary>
public partial class FlyoutWindow : Wpf.Ui.Controls.FluentWindow
{
    public FlyoutWindow()
    {
        try
        {
            System.IO.File.AppendAllText("crash.log", $"[{DateTime.Now}] FlyoutWindow: Before InitializeComponent\n");
            InitializeComponent();
            System.IO.File.AppendAllText("crash.log", $"[{DateTime.Now}] FlyoutWindow: After InitializeComponent\n");
            DataContext = App.Current.Services.GetService(typeof(MainViewModel));
            System.IO.File.AppendAllText("crash.log", $"[{DateTime.Now}] FlyoutWindow: DataContext set\n");
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText("crash.log", $"[{DateTime.Now}] FlyoutWindow CRASH:\n{ex}\n");
            throw;
        }
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        // Hide window when it loses focus
        Hide();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.Show();
        Hide();
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
        catch
        {
            // Ignore errors
        }
    }
}
