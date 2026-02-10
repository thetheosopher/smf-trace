using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SMFTrace.Wpf.Converters;

public sealed class RightInsetConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var width = values.Length > 0 && values[0] is double widthValue ? widthValue : 0.0;
        var margin = values.Length > 1 && values[1] is Thickness thickness ? thickness : new Thickness(0);
        var gap = 0.0;
        var top = 0.0;

        if (parameter is string parameterText)
        {
            var parts = parameterText.Split([','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length > 0
                && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var gapValue))
            {
                gap = gapValue;
            }

            if (parts.Length > 1
                && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var topValue))
            {
                top = topValue;
            }
        }

        var inset = width + margin.Right + gap;
        if (targetType == typeof(Thickness))
        {
            return new Thickness(0, top, inset, 0);
        }

        return inset;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
