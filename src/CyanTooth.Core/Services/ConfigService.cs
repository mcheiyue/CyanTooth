using CyanTooth.Platform.Helpers;
using System.Text.Json;
using CyanTooth.Core.Models;

using CyanTooth.Platform.Helpers;
namespace CyanTooth.Core.Services;

/// <summary>
/// Service for managing application configuration
/// </summary>
public class ConfigService
{
    private readonly string _configPath;
    private AppSettings _settings;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AppSettings Settings => _settings;

    public event EventHandler? SettingsChanged;

    public ConfigService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "CyanTooth");
        Directory.CreateDirectory(appFolder);
        _configPath = Path.Combine(appFolder, "settings.json");
        _settings = LoadSettings();
    }

    /// <summary>
    /// Loads settings from file
    /// </summary>
    public AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                return settings ?? new AppSettings();
            }
        }
        catch
        {
            // Return default settings on error
        }

        return new AppSettings();
    }

    /// <summary>
    /// Saves settings to file
    /// </summary>
    public void SaveSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, JsonOptions);
            File.WriteAllText(_configPath, json);
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            // Ignore save errors
        }
    }

    /// <summary>
    /// Updates settings and saves
    /// </summary>
    public void UpdateSettings(Action<AppSettings> updateAction)
    {
        updateAction(_settings);
        SaveSettings();
    }

    /// <summary>
    /// Adds a device to favorites
    /// </summary>
    public void AddFavorite(string deviceId)
    {
        if (!_settings.FavoriteDevices.Contains(deviceId))
        {
            _settings.FavoriteDevices.Add(deviceId);
            SaveSettings();
        }
    }

    /// <summary>
    /// Removes a device from favorites
    /// </summary>
    public void RemoveFavorite(string deviceId)
    {
        if (_settings.FavoriteDevices.Remove(deviceId))
        {
            SaveSettings();
        }
    }

    /// <summary>
    /// Hides a device from the list
    /// </summary>
    public void HideDevice(string deviceId)
    {
        if (!_settings.HiddenDevices.Contains(deviceId))
        {
            _settings.HiddenDevices.Add(deviceId);
            SaveSettings();
        }
    }

    /// <summary>
    /// Shows a hidden device
    /// </summary>
    public void ShowDevice(string deviceId)
    {
        if (_settings.HiddenDevices.Remove(deviceId))
        {
            SaveSettings();
        }
    }

    /// <summary>
    /// Gets or sets startup registration
    /// </summary>
    public bool IsRunAtStartupEnabled
    {
        get => _settings.RunAtStartup;
        set
        {
            _settings.RunAtStartup = value;
            UpdateStartupRegistration(value);
            SaveSettings();
        }
    }

    private static void UpdateStartupRegistration(bool enable)
    {
        try
        {
            var keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            var appPath = Environment.ProcessPath;

            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(keyPath, true);
            if (key == null) return;

            if (enable && appPath != null)
            {
                key.SetValue("CyanTooth", $"\"{appPath}\" --minimized");
            }
            else
            {
                key.DeleteValue("CyanTooth", false);
            }
        }
        catch
        {
            // Ignore registry errors
        }
    }
}
