using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace SMFTrace.Wpf.ViewModels;

/// <summary>
/// View model for a single lyric line.
/// </summary>
public sealed class LyricLineViewModel : INotifyPropertyChanged
{
    private bool _isActive;
    private double _cachedDpi;
    private double _cachedFontSize;
    private string? _cachedText;
    private FormattedText? _activeText;
    private FormattedText? _inactiveText;

    public LyricLineViewModel(TimeSpan time, string text)
    {
        Time = time;
        Text = text;
    }

    public TimeSpan Time { get; }

    public string Text { get; }

    public bool IsActive
    {
        get => _isActive;
        set => SetField(ref _isActive, value);
    }

    public FormattedText GetFormattedText(double dpi, Typeface typeface, double fontSize, Brush activeBrush, Brush inactiveBrush)
    {
        if (_cachedText != Text
            || Math.Abs(_cachedDpi - dpi) > 0.1
            || Math.Abs(_cachedFontSize - fontSize) > 0.1)
        {
            _cachedText = Text;
            _cachedDpi = dpi;
            _cachedFontSize = fontSize;
            _activeText = null;
            _inactiveText = null;
        }

        if (IsActive)
        {
            _activeText ??= new FormattedText(
                Text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                activeBrush,
                dpi);
            return _activeText;
        }

        _inactiveText ??= new FormattedText(
            Text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            inactiveBrush,
            dpi);
        return _inactiveText;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
