using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SMFTrace.Core.Models;

namespace SMFTrace.Wpf.ViewModels;

/// <summary>
/// View model for the diagnostics tab.
/// </summary>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public sealed class DiagnosticsViewModel : INotifyPropertyChanged
#pragma warning restore CA1711
{
    private IReadOnlyList<MidiEventBase> _allEvents = [];
    private List<DiagnosticEventViewModel> _allEventViewModels = [];
    private ObservableCollection<DiagnosticEventViewModel> _filteredEvents = [];
    private DiagnosticEventViewModel? _selectedEvent;
    private long _currentTick;

    // Filters
    private bool _showNotes = true;
    private bool _showControlChanges = true;
    private bool _showProgramChanges = true;
    private bool _showMeta = true;
    private bool _showSysEx = true;
    private bool _showOther = true;
    private bool _metaOnlyMode;
    private string _searchText = "";

    public DiagnosticsViewModel()
    {
    }

    #region Properties

    public ObservableCollection<DiagnosticEventViewModel> FilteredEvents
    {
        get => _filteredEvents;
        private set => SetField(ref _filteredEvents, value);
    }

    public DiagnosticEventViewModel? SelectedEvent
    {
        get => _selectedEvent;
        set
        {
            if (SetField(ref _selectedEvent, value))
            {
                OnPropertyChanged(nameof(HasSelection));
                OnPropertyChanged(nameof(DetailText));
            }
        }
    }

    public bool HasSelection => _selectedEvent != null;

    public long CurrentTick
    {
        get => _currentTick;
        set => SetField(ref _currentTick, value);
    }

    #region Filters

    public bool ShowNotes
    {
        get => _showNotes;
        set { if (SetField(ref _showNotes, value)) ApplyFilters(); }
    }

    public bool ShowControlChanges
    {
        get => _showControlChanges;
        set { if (SetField(ref _showControlChanges, value)) ApplyFilters(); }
    }

    public bool ShowProgramChanges
    {
        get => _showProgramChanges;
        set { if (SetField(ref _showProgramChanges, value)) ApplyFilters(); }
    }

    public bool ShowMeta
    {
        get => _showMeta;
        set { if (SetField(ref _showMeta, value)) ApplyFilters(); }
    }

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
    public bool ShowSysEx
#pragma warning restore CA1711
    {
        get => _showSysEx;
        set { if (SetField(ref _showSysEx, value)) ApplyFilters(); }
    }

    public bool ShowOther
    {
        get => _showOther;
        set { if (SetField(ref _showOther, value)) ApplyFilters(); }
    }

    public bool MetaOnlyMode
    {
        get => _metaOnlyMode;
        set { if (SetField(ref _metaOnlyMode, value)) ApplyFilters(); }
    }

    public string SearchText
    {
        get => _searchText;
        set { if (SetField(ref _searchText, value)) ApplyFilters(); }
    }

    #endregion

    public string DetailText
    {
        get
        {
            if (_selectedEvent == null) return "";

            var evt = _selectedEvent.Event;
            var lines = new List<string>
            {
                $"Event Type: {_selectedEvent.EventType}",
                $"Tick: {evt.AbsoluteTick}",
                $"Time: {evt.Time:mm\\:ss\\.fff}",
                $"Track: {evt.TrackIndex}"
            };

            switch (evt)
            {
                case NoteOnEvent n:
                    lines.Add($"Channel: {n.Channel + 1}");
                    lines.Add($"Note: {_selectedEvent.Summary.Split(' ')[1]}");
                    lines.Add($"Velocity: {n.Velocity}");
                    break;

                case NoteOffEvent n:
                    lines.Add($"Channel: {n.Channel + 1}");
                    lines.Add($"Note: {_selectedEvent.Summary.Split(' ')[1]}");
                    lines.Add($"Velocity: {n.Velocity}");
                    break;

                case ControlChangeEvent cc:
                    lines.Add($"Channel: {cc.Channel + 1}");
                    lines.Add($"Controller: {cc.ControllerNumber} ({GetControllerName(cc.ControllerNumber)})");
                    lines.Add($"Value: {cc.Value}");
                    break;

                case ProgramChangeEvent pc:
                    lines.Add($"Channel: {pc.Channel + 1}");
                    lines.Add($"Program: {pc.ProgramNumber}");
                    break;

                case PitchBendEvent pb:
                    lines.Add($"Channel: {pb.Channel + 1}");
                    lines.Add($"Value: {pb.Value}");
                    break;

                case MetaEvent meta:
                    lines.Add($"Meta Type: {meta.TypeName}");
                    if (!string.IsNullOrEmpty(meta.TextContent))
                        lines.Add($"Text: \"{meta.TextContent}\"");
                    if (meta.IsSetTempo)
                        lines.Add($"Tempo: {meta.Bpm:F2} BPM");
                    if (meta.IsTimeSignature && meta.Data.Length >= 2)
                        lines.Add($"Time Signature: {meta.Data[0]}/{(int)Math.Pow(2, meta.Data[1])}");
                    break;

                case SysExEvent sysex:
                    lines.Add($"Length: {sysex.Data.Length} bytes");
                    lines.Add($"Manufacturer ID: {FormatHexBytes(sysex.ManufacturerId)}");
                    break;
            }

            lines.Add("");
            lines.Add("Raw Bytes:");
            lines.Add(_selectedEvent.RawBytesHex);

            return string.Join(Environment.NewLine, lines);
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Loads events for display.
    /// </summary>
    public void LoadEvents(IReadOnlyList<MidiEventBase> events)
    {
        _allEvents = events;
        _allEventViewModels = BuildEventViewModels(events);
        ApplyFilters();
    }

    public async Task LoadEventsAsync(IReadOnlyList<MidiEventBase> events)
    {
        _allEvents = events;
        _allEventViewModels = await Task.Run(() => BuildEventViewModels(events));
        ApplyFilters();
    }

    /// <summary>
    /// Clears all filters and shows all events.
    /// </summary>
    public void ClearFilters()
    {
        _showNotes = true;
        _showControlChanges = true;
        _showProgramChanges = true;
        _showMeta = true;
        _showSysEx = true;
        _showOther = true;
        _metaOnlyMode = false;
        _searchText = "";

        OnPropertyChanged(nameof(ShowNotes));
        OnPropertyChanged(nameof(ShowControlChanges));
        OnPropertyChanged(nameof(ShowProgramChanges));
        OnPropertyChanged(nameof(ShowMeta));
        OnPropertyChanged(nameof(ShowSysEx));
        OnPropertyChanged(nameof(ShowOther));
        OnPropertyChanged(nameof(MetaOnlyMode));
        OnPropertyChanged(nameof(SearchText));

        ApplyFilters();
    }

    #endregion

    #region Private Methods

    private void ApplyFilters()
    {
        var searchLower = string.IsNullOrWhiteSpace(_searchText) ? string.Empty : _searchText.ToLowerInvariant();
        var filtered = new List<DiagnosticEventViewModel>();

        foreach (var vm in _allEventViewModels)
        {
            if (!PassesFilter(vm, searchLower))
            {
                continue;
            }

            filtered.Add(vm);
        }

        FilteredEvents = new ObservableCollection<DiagnosticEventViewModel>(filtered);
    }

    private static List<DiagnosticEventViewModel> BuildEventViewModels(IReadOnlyList<MidiEventBase> events)
    {
        var list = new List<DiagnosticEventViewModel>(events.Count);
        for (var i = 0; i < events.Count; i++)
        {
            list.Add(new DiagnosticEventViewModel(events[i], i));
        }

        return list;
    }

    private bool PassesFilter(DiagnosticEventViewModel vm, string searchLower)
    {
        // Meta-only mode
        if (_metaOnlyMode && !vm.IsMeta) return false;

        // Category filters
        var passesCategory = vm.Category switch
        {
            EventCategory.Note => _showNotes,
            EventCategory.ControlChange => _showControlChanges,
            EventCategory.ProgramChange => _showProgramChanges,
            EventCategory.Meta => _showMeta,
            EventCategory.SysExCategory => _showSysEx,
            EventCategory.Other => _showOther,
            _ => true
        };

        if (!passesCategory) return false;

        // Text search
        if (!string.IsNullOrEmpty(searchLower))
        {
            var matchesSearch =
                vm.EventType.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ||
                vm.Summary.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ||
                vm.RawBytesHex.Contains(searchLower, StringComparison.OrdinalIgnoreCase);

            if (!matchesSearch) return false;
        }

        return true;
    }

    private static string GetControllerName(byte cc) => cc switch
    {
        0 => "Bank Select MSB",
        1 => "Modulation",
        2 => "Breath Controller",
        4 => "Foot Controller",
        5 => "Portamento Time",
        6 => "Data Entry MSB",
        7 => "Volume",
        8 => "Balance",
        10 => "Pan",
        11 => "Expression",
        32 => "Bank Select LSB",
        64 => "Sustain Pedal",
        65 => "Portamento",
        66 => "Sostenuto",
        67 => "Soft Pedal",
        68 => "Legato Footswitch",
        69 => "Hold 2",
        91 => "Reverb",
        93 => "Chorus",
        94 => "Celeste/Detune",
        95 => "Phaser",
        120 => "All Sound Off",
        121 => "Reset All Controllers",
        123 => "All Notes Off",
        _ => $"CC {cc}"
    };

    private static string FormatHexBytes(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return string.Empty;
        }

        var chars = new char[(bytes.Length * 3) - 1];
        var position = 0;

        for (var i = 0; i < bytes.Length; i++)
        {
            var hex = bytes[i].ToString("X2", System.Globalization.CultureInfo.InvariantCulture);
            chars[position++] = hex[0];
            chars[position++] = hex[1];
            if (i < bytes.Length - 1)
            {
                chars[position++] = ' ';
            }
        }

        return new string(chars);
    }

    #endregion

    #region INotifyPropertyChanged

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

    #endregion
}
