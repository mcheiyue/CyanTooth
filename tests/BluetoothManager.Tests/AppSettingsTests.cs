using BluetoothManager.Core.Models;
using Xunit;

namespace BluetoothManager.Tests;

public class AppSettingsTests
{
    [Fact]
    public void DefaultSettings_HaveCorrectValues()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        Assert.True(settings.StartMinimized);
        Assert.False(settings.RunAtStartup);
        Assert.Equal(60, settings.BatteryPollIntervalSeconds);
        Assert.Equal(20, settings.LowBatteryThreshold);
        Assert.True(settings.ShowBatteryInTray);
        Assert.True(settings.ShowConnectionNotifications);
        Assert.True(settings.ShowLowBatteryNotifications);
        Assert.Equal(AppTheme.System, settings.Theme);
    }

    [Fact]
    public void FavoriteDevices_DefaultsToEmptyList()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        Assert.NotNull(settings.FavoriteDevices);
        Assert.Empty(settings.FavoriteDevices);
    }

    [Fact]
    public void HiddenDevices_DefaultsToEmptyList()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        Assert.NotNull(settings.HiddenDevices);
        Assert.Empty(settings.HiddenDevices);
    }
}
