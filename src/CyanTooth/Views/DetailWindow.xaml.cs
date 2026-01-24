using CyanTooth.Platform.Helpers;
using System;
using System.Windows;
using CyanTooth.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CyanTooth.Views;

/// <summary>
/// Detailed main window with tabs
/// </summary>
public partial class DetailWindow : Wpf.Ui.Controls.FluentWindow
{
    private bool _hasNavigated = false;

    public DetailWindow()
    {
        InitializeComponent();
        try
        {
            DataContext = App.Current.Services.GetRequiredService<DetailViewModel>();
            
            // Set TargetPageTypes in code-behind to avoid XAML parser issues
            if (RootNavigation.MenuItems[0] is Wpf.Ui.Controls.NavigationViewItem deviceItem)
                deviceItem.TargetPageType = typeof(Pages.DevicePage);
                
            if (RootNavigation.MenuItems[1] is Wpf.Ui.Controls.NavigationViewItem settingsItem)
                settingsItem.TargetPageType = typeof(Pages.SettingsPage);
                
            if (RootNavigation.FooterMenuItems[0] is Wpf.Ui.Controls.NavigationViewItem aboutItem)
                aboutItem.TargetPageType = typeof(Pages.AboutPage);
            
            // Ensure back button is hidden
            Loaded += (s, e) => 
            {
                RootNavigation.IsBackButtonVisible = Wpf.Ui.Controls.NavigationViewBackButtonVisible.Collapsed;
                
                // Navigate to first page only if not yet navigated
                if (!_hasNavigated)
                {
                     RootNavigation.Navigate(typeof(Pages.DevicePage));
                     _hasNavigated = true;
                }
            };
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("DetailWindow DataContext injection failed", ex);
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // 除非是应用显式退出(通过托盘菜单)，否则点击关闭只隐藏窗口
        if (!App.IsExiting)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnClosing(e);
    }
}
