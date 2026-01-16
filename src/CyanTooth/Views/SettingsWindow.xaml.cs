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
        DebugLogger.Log("SettingsWindow: 正在初始化...");
        try
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetRequiredService<SettingsViewModel>();
            DebugLogger.Log("SettingsWindow: 初始化完成。");
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("SettingsWindow 初始化失败", ex);
            throw;
        }
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
