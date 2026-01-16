using System;
using System.Windows;
using CyanTooth.ViewModels;

namespace CyanTooth.Views;

/// <summary>
/// Settings window
/// </summary>
public partial class SettingsWindow : Wpf.Ui.Controls.FluentWindow
{
    public SettingsWindow()
    {
        try
        {
            System.IO.File.AppendAllText("crash.log", $"[{DateTime.Now}] SettingsWindow: Before InitializeComponent\n");
            InitializeComponent();
            System.IO.File.AppendAllText("crash.log", $"[{DateTime.Now}] SettingsWindow: After InitializeComponent\n");
            DataContext = App.Current.Services.GetService(typeof(SettingsViewModel));
            System.IO.File.AppendAllText("crash.log", $"[{DateTime.Now}] SettingsWindow: DataContext set\n");
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText("crash.log", $"[{DateTime.Now}] SettingsWindow CRASH:\n{ex}\n");
            throw;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // 先执行保存命令
        if (DataContext is SettingsViewModel vm)
        {
            vm.SaveCommand.Execute(null);
        }
        // 不自动关闭窗口，让用户自己关闭
    }
}
