using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace SMFTrace.Wpf.ViewModels;

/// <summary>
/// View model for a single lyric line.
/// </summary>
public sealed class LyricLineViewModel : INotifyPropertyChanged
{
    private bool _isActive;

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
