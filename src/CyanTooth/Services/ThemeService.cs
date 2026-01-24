using System;
using System.Linq;
using System.Windows;
using CyanTooth.Core.Models;
using Wpf.Ui.Appearance;

namespace CyanTooth.Services;

/// <summary>
/// Service for managing application themes (Light/Dark/System).
/// Handles both WPF-UI theme manager and custom resource dictionaries.
/// </summary>
public class ThemeService
{
    private const string LightThemeResource = "Themes/LightTheme.xaml";
    private const string DarkThemeResource = "Themes/DarkTheme.xaml";

    /// <summary>
    /// Applies the specified application theme.
    /// </summary>
    /// <param name="theme">The theme to apply.</param>
    public void ApplyTheme(AppTheme theme)
    {
        // 1. Determine target WPF-UI theme
        var targetTheme = theme switch
        {
            AppTheme.Light => ApplicationTheme.Light,
            AppTheme.Dark => ApplicationTheme.Dark,
            _ => ApplicationTheme.Unknown // System
        };

        // 2. Determine which custom resource dictionary to load
        string resourcePath = DarkThemeResource; // Default

        if (theme == AppTheme.Light)
        {
            resourcePath = LightThemeResource;
        }
        else if (theme == AppTheme.System)
        {
            // If System, we need to check the actual system theme to decide our resources
            var systemTheme = ApplicationThemeManager.GetSystemTheme();
            if (systemTheme == SystemTheme.Light)
            {
                resourcePath = LightThemeResource;
            }
        }

        // 3. Apply WPF-UI Theme
        if (targetTheme == ApplicationTheme.Unknown)
        {
            ApplicationThemeManager.ApplySystemTheme();
        }
        else
        {
            ApplicationThemeManager.Apply(targetTheme);
        }

        // 4. Swap Resource Dictionaries (UI Thread Safe)
        Application.Current.Dispatcher.Invoke(() =>
        {
            UpdateThemeResources(resourcePath);
        });
    }

    /// <summary>
    /// Updates the application resources to use the specified theme file.
    /// </summary>
    private void UpdateThemeResources(string resourcePath)
    {
        try
        {
            var dictionaries = Application.Current.Resources.MergedDictionaries;

            // Find existing theme dictionary
            var existingTheme = dictionaries.FirstOrDefault(d => 
                d.Source != null && 
                (d.Source.OriginalString.EndsWith("DarkTheme.xaml") || 
                 d.Source.OriginalString.EndsWith("LightTheme.xaml")));

            // If the resource is already loaded, do nothing (optimization)
            if (existingTheme != null && existingTheme.Source.OriginalString.EndsWith(resourcePath))
            {
                return;
            }

            // Remove existing
            if (existingTheme != null)
            {
                dictionaries.Remove(existingTheme);
            }

            // Add new
            dictionaries.Add(new ResourceDictionary 
            { 
                Source = new Uri(resourcePath, UriKind.Relative) 
            });
        }
        catch (Exception ex)
        {
            // We can't use DebugLogger here easily without circular dep or passing it in, 
            // but we can just swallow or rethrow. 
            // Since this is a service, let's trust the caller handles global exceptions 
            // or just use System.Diagnostics.Debug for now.
            System.Diagnostics.Debug.WriteLine($"Error applying theme resource {resourcePath}: {ex.Message}");
        }
    }
}
