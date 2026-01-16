using BluetoothManager.Core.Models;
using Xunit;

namespace BluetoothManager.Tests;

public class BluetoothDeviceTests
{
    [Fact]
    public void MacAddress_FormatsCorrectly()
    {
        // Arrange
        var device = new BluetoothDevice
        {
            Id = "test-device",
            Address = 0xAABBCCDDEEFF,
            Name = "Test Device"
        };

        // Act
        var macAddress = device.MacAddress;

        // Assert
        Assert.Equal("AA:BB:CC:DD:EE:FF", macAddress);
    }

    [Fact]
    public void MacAddress_ZeroAddress_ReturnsEmpty()
    {
        // Arrange
        var device = new BluetoothDevice
        {
            Id = "test-device",
            Address = 0,
            Name = "Test Device"
        };

        // Act
        var macAddress = device.MacAddress;

        // Assert
        Assert.Equal(string.Empty, macAddress);
    }

    [Fact]
    public void BatteryLevel_CanBeSet()
    {
        // Arrange
        var device = new BluetoothDevice
        {
            Id = "test-device",
            Name = "Test Device"
        };

        // Act
        device.BatteryLevel = 75;

        // Assert
        Assert.Equal((byte)75, device.BatteryLevel);
    }

    [Fact]
    public void IsConnected_DefaultsFalse()
    {
        // Arrange & Act
        var device = new BluetoothDevice
        {
            Id = "test-device",
            Name = "Test Device"
        };

        // Assert
        Assert.False(device.IsConnected);
    }
}
