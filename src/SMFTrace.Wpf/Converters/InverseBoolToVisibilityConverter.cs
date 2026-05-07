using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SMFTrace.Wpf.Converters;

/// <summary>
/// Converts a boolean to <see cref="Visibility"/> where <c>true</c> hides and <c>false</c> shows.
/// </summary>
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var b = value is bool v && v;
        return b ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility vis && vis != Visibility.Visible;
    }
}
