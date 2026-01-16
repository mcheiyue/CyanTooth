namespace CyanTooth.Core.Models;

/// <summary>
/// Application settings
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Whether to start the application minimized to tray
    /// </summary>
    public bool StartMinimized { get; set; } = true;

    /// <summary>
    /// Whether to run at Windows startup
    /// </summary>
    public bool RunAtStartup { get; set; } = false;

    /// <summary>
    /// Battery poll interval in seconds
    /// </summary>
    public int BatteryPollIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Low battery threshold for notifications (percentage)
    /// </summary>
    public int LowBatteryThreshold { get; set; } = 20;

    /// <summary>
    /// Whether to show battery level in tray icon
    /// </summary>
    public bool ShowBatteryInTray { get; set; } = true;

    /// <summary>
    /// Whether to show notifications for device connection changes
    /// </summary>
    public bool ShowConnectionNotifications { get; set; } = true;

    /// <summary>
    /// Whether to show low battery notifications
    /// </summary>
    public bool ShowLowBatteryNotifications { get; set; } = true;

    /// <summary>
    /// UI Theme (Light/Dark/System)
    /// </summary>
    public AppTheme Theme { get; set; } = AppTheme.System;

    /// <summary>
    /// Flyout window position relative to tray
    /// </summary>
    public FlyoutPosition FlyoutPosition { get; set; } = FlyoutPosition.Auto;

    /// <summary>
    /// Favorite device IDs for quick access
    /// </summary>
    public List<string> FavoriteDevices { get; set; } = new();

    /// <summary>
    /// Hidden device IDs (won't show in the list)
    /// </summary>
    public List<string> HiddenDevices { get; set; } = new();
}

public enum AppTheme
{
    Light,
    Dark,
    System
}

public enum FlyoutPosition
{
    Auto,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}
