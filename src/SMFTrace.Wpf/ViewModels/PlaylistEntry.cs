using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace SMFTrace.Wpf.ViewModels;

public enum PlaylistMetadataStatus
{
    Unparsed,
    Parsing,
    Parsed,
    Failed
}

public sealed class PlaylistEntry : INotifyPropertyChanged
{
    private string _displayTitle;
    private string _durationDisplay = "-";
    private string _tempoDisplay = "-";
    private string _timeSignatureDisplay = "4/4";
    private string _keySignatureDisplay = "-";
    private string _smfTypeDisplay = "-";
    private int _trackCount;
    private string _sysExPresentDisplay = "No";
    private string _lyricsPresentDisplay = "No";
    private PlaylistMetadataStatus _metadataStatus = PlaylistMetadataStatus.Unparsed;
    private bool _isNowPlaying;

    public PlaylistEntry(string filePath, string displayTitle)
    {
        FilePath = filePath;
        _displayTitle = displayTitle;
    }

    public string FilePath { get; }

    public string DisplayTitle
    {
        get => _displayTitle;
        set => SetField(ref _displayTitle, value);
    }

    public string DurationDisplay
    {
        get => _durationDisplay;
        set => SetField(ref _durationDisplay, value);
    }

    public string TempoDisplay
    {
        get => _tempoDisplay;
        set => SetField(ref _tempoDisplay, value);
    }

    public string TimeSignatureDisplay
    {
        get => _timeSignatureDisplay;
        set => SetField(ref _timeSignatureDisplay, value);
    }

    public string KeySignatureDisplay
    {
        get => _keySignatureDisplay;
        set => SetField(ref _keySignatureDisplay, value);
    }

    public string SmfTypeDisplay
    {
        get => _smfTypeDisplay;
        set => SetField(ref _smfTypeDisplay, value);
    }

    public int TrackCount
    {
        get => _trackCount;
        set => SetField(ref _trackCount, value);
    }

    public string SysExPresentDisplay
    {
        get => _sysExPresentDisplay;
        set => SetField(ref _sysExPresentDisplay, value);
    }

    public string LyricsPresentDisplay
    {
        get => _lyricsPresentDisplay;
        set => SetField(ref _lyricsPresentDisplay, value);
    }

    public PlaylistMetadataStatus MetadataStatus
    {
        get => _metadataStatus;
        set => SetField(ref _metadataStatus, value);
    }

    public bool IsNowPlaying
    {
        get => _isNowPlaying;
        set => SetField(ref _isNowPlaying, value);
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
