using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CyanTooth.Converters
{
    public class CodecToColorConverter : IValueConverter
    {
        // Cache brushes to avoid creating new ones every time
        private static readonly SolidColorBrush LdacBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D4AF37")); // Metallic Gold
        private static readonly SolidColorBrush AptxHdBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4500")); // OrangeRed
        private static readonly SolidColorBrush AptxBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00C853")); // Green
        private static readonly SolidColorBrush AacBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2979FF")); // Blue
        private static readonly SolidColorBrush Lc3Brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AA00FF")); // Purple
        private static readonly SolidColorBrush SbcBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#757575")); // Grey
        private static readonly SolidColorBrush DefaultBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9E9E9E"));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string codec && !string.IsNullOrEmpty(codec))
            {
                // Normalize string just in case
                var upper = codec.ToUpperInvariant();
                
                if (upper.Contains("LDAC")) return LdacBrush;
                if (upper.Contains("APTX HD")) return AptxHdBrush;
                if (upper.Contains("APTX")) return AptxBrush;
                if (upper.Contains("AAC")) return AacBrush;
                if (upper.Contains("LC3")) return Lc3Brush;
                if (upper.Contains("SBC")) return SbcBrush;
            }

            return DefaultBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}