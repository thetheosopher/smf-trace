using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace SMFTrace.Wpf.ViewModels;

/// <summary>
/// View model for per-track mute/solo state.
/// </summary>
public sealed class TrackPlaybackViewModel : INotifyPropertyChanged
{
    private bool _isMuted;
    private bool _isSolo;

    public TrackPlaybackViewModel(int trackIndex, string? name)
    {
        TrackIndex = trackIndex;
        DisplayName = string.IsNullOrWhiteSpace(name)
            ? $"Track {trackIndex + 1}"
            : $"Track {trackIndex + 1}: {name}";
    }

    public int TrackIndex { get; }

    public string DisplayName { get; }

    public bool IsMuted
    {
        get => _isMuted;
        set => SetField(ref _isMuted, value);
    }

    public bool IsSolo
    {
        get => _isSolo;
        set => SetField(ref _isSolo, value);
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
