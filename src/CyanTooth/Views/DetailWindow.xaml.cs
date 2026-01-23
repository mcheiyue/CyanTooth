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
            
            // Navigate to first page
            RootNavigation.Navigate(typeof(Pages.DevicePage));
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("DetailWindow DataContext injection failed", ex);
        }
    }
}
