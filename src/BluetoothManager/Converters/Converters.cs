using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BluetoothManager.Converters;

/// <summary>
/// Converts boolean to visibility
/// </summary>
public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool bValue = value is bool b && b;
        bool invert = parameter?.ToString()?.ToLower() == "invert";
        
        if (invert) bValue = !bValue;
        return bValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts nullable byte (battery level) to visibility
/// </summary>
public class NullableToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts connection status to tooltip text
/// </summary>
public class ConnectionTooltipConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isConnected = value is bool b && b;
        return isConnected ? "断开连接" : "连接";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts battery level to color brush
/// </summary>
public class BatteryLevelToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not byte level)
            return System.Windows.Application.Current.FindResource("TextFillColorSecondaryBrush");

        return level switch
        {
            <= 20 => System.Windows.Application.Current.FindResource("SystemFillColorCautionBrush"),
            <= 50 => System.Windows.Application.Current.FindResource("SystemFillColorAttentionBrush"),
            _ => System.Windows.Application.Current.FindResource("SystemFillColorSuccessBrush")
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
