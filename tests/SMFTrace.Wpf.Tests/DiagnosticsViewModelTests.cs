using SMFTrace.Core.Models;
using SMFTrace.Wpf.ViewModels;
using Xunit;

namespace SMFTrace.Wpf.Tests;

public class DiagnosticsViewModelTests
{
    private static NoteOnEvent CreateNoteOn(long tick, byte channel) =>
        new()
        {
            AbsoluteTick = tick,
            Time = TimeSpan.FromMilliseconds(tick),
            TrackIndex = 0,
            OriginalIndex = 0,
            RawBytes = [],
            Channel = channel,
            NoteNumber = 60,
            Velocity = 100
        };

    private static ControlChangeEvent CreateCC(long tick, byte channel, byte cc, byte value) =>
        new()
        {
            AbsoluteTick = tick,
            Time = TimeSpan.FromMilliseconds(tick),
            TrackIndex = 0,
            OriginalIndex = 0,
            RawBytes = [],
            Channel = channel,
            ControllerNumber = cc,
            Value = value
        };

    private static MetaEvent CreateMeta(long tick, byte type, string text) =>
        new()
        {
            AbsoluteTick = tick,
            Time = TimeSpan.FromMilliseconds(tick),
            TrackIndex = 0,
            OriginalIndex = 0,
            RawBytes = [],
            MetaType = type,
            Data = System.Text.Encoding.UTF8.GetBytes(text)
        };

    [Fact]
    public void LoadEventsPopulatesFilteredEvents()
    {
        // Arrange
        var vm = new DiagnosticsViewModel();
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(0, 0),
            CreateNoteOn(100, 0),
            CreateCC(200, 0, 7, 100)
        };

        // Act
        vm.LoadEvents(events);

        // Assert
        Assert.Equal(3, vm.FilteredEvents.Count);
    }

    [Fact]
    public void FilterNotesHidesNotes()
    {
        // Arrange
        var vm = new DiagnosticsViewModel();
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(0, 0),
            CreateCC(100, 0, 7, 100)
        };
        vm.LoadEvents(events);

        // Act
        vm.ShowNotes = false;

        // Assert
        Assert.Single(vm.FilteredEvents);
        Assert.Equal("Control Change", vm.FilteredEvents[0].EventType);
    }

    [Fact]
    public void FilterControlChangesHidesCC()
    {
        // Arrange
        var vm = new DiagnosticsViewModel();
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(0, 0),
            CreateCC(100, 0, 7, 100)
        };
        vm.LoadEvents(events);

        // Act
        vm.ShowControlChanges = false;

        // Assert
        Assert.Single(vm.FilteredEvents);
        Assert.Equal("Note On", vm.FilteredEvents[0].EventType);
    }

    [Fact]
    public void MetaOnlyModeShowsOnlyMetaEvents()
    {
        // Arrange
        var vm = new DiagnosticsViewModel();
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(0, 0),
            CreateMeta(50, 0x03, "Track 1"), // Track Name
            CreateCC(100, 0, 7, 100)
        };
        vm.LoadEvents(events);

        // Act
        vm.MetaOnlyMode = true;

        // Assert
        Assert.Single(vm.FilteredEvents);
        Assert.Contains("Meta", vm.FilteredEvents[0].EventType);
    }

    [Fact]
    public void SearchTextFiltersEvents()
    {
        // Arrange
        var vm = new DiagnosticsViewModel();
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(0, 0),
            CreateCC(100, 0, 7, 100)
        };
        vm.LoadEvents(events);

        // Act
        vm.SearchText = "CC7";

        // Assert
        Assert.Single(vm.FilteredEvents);
        Assert.Contains("CC7", vm.FilteredEvents[0].Summary);
    }

    [Fact]
    public void ClearFiltersShowsAllEvents()
    {
        // Arrange
        var vm = new DiagnosticsViewModel();
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(0, 0),
            CreateCC(100, 0, 7, 100)
        };
        vm.LoadEvents(events);
        vm.ShowNotes = false;
        vm.ShowControlChanges = false;

        // Act
        vm.ClearFilters();

        // Assert
        Assert.Equal(2, vm.FilteredEvents.Count);
    }

    [Fact]
    public void SelectingEventSetsSelectedEvent()
    {
        // Arrange
        var vm = new DiagnosticsViewModel();
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(0, 0),
            CreateCC(100, 0, 7, 100)
        };
        vm.LoadEvents(events);

        // Act
        vm.SelectedEvent = vm.FilteredEvents[0];

        // Assert
        Assert.NotNull(vm.SelectedEvent);
        Assert.True(vm.HasSelection);
    }

    [Fact]
    public void DetailTextShowsEventInfo()
    {
        // Arrange
        var vm = new DiagnosticsViewModel();
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(0, 0)
        };
        vm.LoadEvents(events);
        vm.SelectedEvent = vm.FilteredEvents[0];

        // Act
        var detail = vm.DetailText;

        // Assert
        Assert.Contains("Note On", detail);
        Assert.Contains("Tick: 0", detail);
    }
}
