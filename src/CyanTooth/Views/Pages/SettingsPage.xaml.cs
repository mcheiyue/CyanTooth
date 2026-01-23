using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using CyanTooth.ViewModels;
using CyanTooth.Platform.Helpers;
using System;

namespace CyanTooth.Views.Pages;

public partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        try
        {
            DataContext = App.Current.Services.GetRequiredService<SettingsViewModel>();
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("SettingsPage DataContext resolution failed", ex);
        }
    }
}
