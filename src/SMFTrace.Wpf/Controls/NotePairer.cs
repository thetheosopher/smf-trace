using SMFTrace.Core.Models;

namespace SMFTrace.Wpf.Controls;

/// <summary>
/// Pairs NoteOn and NoteOff events to create renderable note rectangles.
/// </summary>
public static class NotePairer
{
    /// <summary>
    /// Pairs notes from a list of MIDI events.
    /// </summary>
    /// <param name="events">Sorted list of MIDI events.</param>
    /// <returns>List of paired notes with start/end times.</returns>
    public static List<PairedNote> PairNotes(IReadOnlyList<MidiEventBase> events)
    {
        var result = new List<PairedNote>();

        // Track open notes by (track, channel, note) => NoteOnEvent
        var openNotes = new Dictionary<(int Track, byte Channel, byte Note), NoteOnEvent>();

        foreach (var evt in events)
        {
            switch (evt)
            {
                case NoteOnEvent noteOn when noteOn.Velocity > 0:
                    HandleNoteOn(noteOn, openNotes, result);
                    break;

                case NoteOnEvent noteOnOff when noteOnOff.Velocity == 0:
                    // NoteOn with velocity 0 is treated as NoteOff
                    HandleNoteOff(noteOnOff.TrackIndex, noteOnOff.Channel, noteOnOff.NoteNumber,
                        noteOnOff.AbsoluteTick, noteOnOff.Time, openNotes, result);
                    break;

                case NoteOffEvent noteOff:
                    HandleNoteOff(noteOff.TrackIndex, noteOff.Channel, noteOff.NoteNumber,
                        noteOff.AbsoluteTick, noteOff.Time, openNotes, result);
                    break;
            }
        }

        // Close any remaining open notes at their last known position
        foreach (var kvp in openNotes)
        {
            var noteOn = kvp.Value;
            result.Add(new PairedNote
            {
                StartTick = noteOn.AbsoluteTick,
                EndTick = noteOn.AbsoluteTick + 480, // Default duration of 1 beat
                StartTime = noteOn.Time,
                EndTime = noteOn.Time + TimeSpan.FromMilliseconds(500),
                Channel = noteOn.Channel,
                NoteNumber = noteOn.NoteNumber,
                Velocity = noteOn.Velocity,
                TrackIndex = noteOn.TrackIndex,
                Lane = new LaneId(noteOn.TrackIndex, noteOn.Channel)
            });
        }

        return result;
    }

    private static void HandleNoteOn(
        NoteOnEvent noteOn,
        Dictionary<(int Track, byte Channel, byte Note), NoteOnEvent> openNotes,
        List<PairedNote> result)
    {
        var key = (noteOn.TrackIndex, noteOn.Channel, noteOn.NoteNumber);

        // If there's already an open note with the same key, close it immediately
        if (openNotes.TryGetValue(key, out var existing))
        {
            // Overlapping NoteOn closes previous note immediately
            result.Add(new PairedNote
            {
                StartTick = existing.AbsoluteTick,
                EndTick = noteOn.AbsoluteTick,
                StartTime = existing.Time,
                EndTime = noteOn.Time,
                Channel = existing.Channel,
                NoteNumber = existing.NoteNumber,
                Velocity = existing.Velocity,
                TrackIndex = existing.TrackIndex,
                Lane = new LaneId(existing.TrackIndex, existing.Channel)
            });
        }

        openNotes[key] = noteOn;
    }

    private static void HandleNoteOff(
        int trackIndex,
        byte channel,
        byte noteNumber,
        long endTick,
        TimeSpan endTime,
        Dictionary<(int Track, byte Channel, byte Note), NoteOnEvent> openNotes,
        List<PairedNote> result)
    {
        var key = (trackIndex, channel, noteNumber);

        if (openNotes.TryGetValue(key, out var noteOn))
        {
            result.Add(new PairedNote
            {
                StartTick = noteOn.AbsoluteTick,
                EndTick = endTick,
                StartTime = noteOn.Time,
                EndTime = endTime,
                Channel = noteOn.Channel,
                NoteNumber = noteOn.NoteNumber,
                Velocity = noteOn.Velocity,
                TrackIndex = noteOn.TrackIndex,
                Lane = new LaneId(noteOn.TrackIndex, noteOn.Channel)
            });

            openNotes.Remove(key);
        }
        // If no matching NoteOn found, ignore the NoteOff (orphan)
    }

    /// <summary>
    /// Groups paired notes by lane.
    /// </summary>
    public static Dictionary<LaneId, List<PairedNote>> GroupByLane(IEnumerable<PairedNote> notes)
    {
        var result = new Dictionary<LaneId, List<PairedNote>>();

        foreach (var note in notes)
        {
            if (!result.TryGetValue(note.Lane, out var list))
            {
                list = [];
                result[note.Lane] = list;
            }

            list.Add(note);
        }

        return result;
    }
}
