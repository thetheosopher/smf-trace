using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using SMFTrace.Core.Ordering;
using DryNoteOnEvent = Melanchall.DryWetMidi.Core.NoteOnEvent;
using DryNoteOffEvent = Melanchall.DryWetMidi.Core.NoteOffEvent;
using DryControlChangeEvent = Melanchall.DryWetMidi.Core.ControlChangeEvent;
using DryProgramChangeEvent = Melanchall.DryWetMidi.Core.ProgramChangeEvent;
using DryPitchBendEvent = Melanchall.DryWetMidi.Core.PitchBendEvent;
using NoteOnEvent = SMFTrace.Core.Models.NoteOnEvent;
using NoteOffEvent = SMFTrace.Core.Models.NoteOffEvent;
using ControlChangeEvent = SMFTrace.Core.Models.ControlChangeEvent;
using ProgramChangeEvent = SMFTrace.Core.Models.ProgramChangeEvent;
using PitchBendEvent = SMFTrace.Core.Models.PitchBendEvent;
using ChannelPressureEvent = SMFTrace.Core.Models.ChannelPressureEvent;
using PolyPressureEvent = SMFTrace.Core.Models.PolyPressureEvent;
using SysExEvent = SMFTrace.Core.Models.SysExEvent;
using MetaEvent = SMFTrace.Core.Models.MetaEvent;
using TrackInfo = SMFTrace.Core.Models.TrackInfo;
using MidiEventBase = SMFTrace.Core.Models.MidiEventBase;

namespace SMFTrace.Core.Sequencer;

/// <summary>
/// Maximum file size (50 MB) to prevent memory issues.
/// </summary>
internal static class MidiFileConstraints
{
    public const long MaxFileSizeBytes = 50 * 1024 * 1024;
}

/// <summary>
/// Result of loading a MIDI file.
/// </summary>
public sealed class MidiFileData
{
    /// <summary>The original file path.</summary>
    public required string FilePath { get; init; }

    /// <summary>SMF format (0, 1, or 2).</summary>
    public required int Format { get; init; }

    /// <summary>Ticks per quarter note (PPQ / division).</summary>
    public required int TicksPerQuarterNote { get; init; }

    /// <summary>Track information.</summary>
    public required IReadOnlyList<TrackInfo> Tracks { get; init; }

    /// <summary>All events flattened and sorted.</summary>
    public required IReadOnlyList<MidiEventBase> Events { get; init; }

    /// <summary>Total duration of the file.</summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>Total tick count.</summary>
    public required long TotalTicks { get; init; }

    /// <summary>Tempo map for tick-to-time conversion.</summary>
    public required TempoMap TempoMap { get; init; }
}

/// <summary>
/// Loads and parses Standard MIDI Files.
/// </summary>
public static class MidiFileLoader
{
    /// <summary>
    /// Loads a MIDI file from the specified path.
    /// </summary>
    /// <param name="filePath">Path to the .mid file.</param>
    /// <returns>Parsed MIDI file data with flattened, sorted events.</returns>
    /// <exception cref="MidiFileException">If the file cannot be loaded or is invalid.</exception>
    public static MidiFileData Load(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        // Check file exists
        if (!File.Exists(filePath))
        {
            throw new MidiFileException(
                $"File not found: {filePath}",
                filePath,
                MidiFileErrorType.FileNotFound);
        }

        // Check file size
        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length > MidiFileConstraints.MaxFileSizeBytes)
        {
            throw new MidiFileException(
                $"File is too large ({fileInfo.Length / (1024 * 1024):F1} MB). Maximum supported size is {MidiFileConstraints.MaxFileSizeBytes / (1024 * 1024)} MB.",
                filePath,
                MidiFileErrorType.FileTooLarge);
        }

        if (fileInfo.Length == 0)
        {
            throw new MidiFileException(
                "File is empty.",
                filePath,
                MidiFileErrorType.EmptyFile);
        }

        try
        {
            var midiFile = MidiFile.Read(filePath);
            return Parse(midiFile, filePath);
        }
        catch (MidiFileException)
        {
            throw; // Re-throw our own exceptions
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            throw new MidiFileException(
                $"Failed to read MIDI file: {ex.Message}",
                filePath,
                MidiFileErrorType.InvalidFormat,
                ex);
        }
    }

    /// <summary>
    /// Loads a MIDI file from a stream.
    /// </summary>
    /// <param name="stream">Stream containing MIDI data.</param>
    /// <param name="fileName">Display name for the file.</param>
    /// <returns>Parsed MIDI file data with flattened, sorted events.</returns>
    /// <exception cref="MidiFileException">If the stream cannot be parsed as MIDI.</exception>
    public static MidiFileData Load(Stream stream, string fileName = "stream")
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            var midiFile = MidiFile.Read(stream);
            return Parse(midiFile, fileName);
        }
        catch (MidiFileException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            throw new MidiFileException(
                $"Failed to read MIDI stream: {ex.Message}",
                fileName,
                MidiFileErrorType.InvalidFormat,
                ex);
        }
    }

    private static MidiFileData Parse(MidiFile midiFile, string filePath)
    {
        var format = (int)midiFile.OriginalFormat;

        // Only support Type 0 and Type 1
        if (format == 2)
        {
            throw new MidiFileException(
                "SMF Type 2 files are not supported.",
                filePath,
                MidiFileErrorType.UnsupportedFormat);
        }

        // Check for empty file
        var chunks = midiFile.GetTrackChunks().ToList();
        if (chunks.Count == 0)
        {
            throw new MidiFileException(
                "MIDI file contains no tracks.",
                filePath,
                MidiFileErrorType.EmptyFile);
        }

        var timeDivision = midiFile.TimeDivision;
        var ticksPerQuarterNote = timeDivision is TicksPerQuarterNoteTimeDivision tpqn
            ? tpqn.TicksPerQuarterNote
            : 480; // Default fallback

        var tempoMap = midiFile.GetTempoMap();
        var tracks = ExtractTrackInfo(chunks);
        var events = FlattenEvents(chunks, tempoMap);

        // Sort events according to intra-tick rules
        EventSorter.Sort(events);

        // Calculate duration
        var totalTicks = events.Count > 0 ? events[^1].AbsoluteTick : 0;
        var duration = events.Count > 0 ? events[^1].Time : TimeSpan.Zero;

        return new MidiFileData
        {
            FilePath = filePath,
            Format = format,
            TicksPerQuarterNote = ticksPerQuarterNote,
            Tracks = tracks,
            Events = events,
            Duration = duration,
            TotalTicks = totalTicks,
            TempoMap = tempoMap
        };
    }

    private static List<TrackInfo> ExtractTrackInfo(List<TrackChunk> chunks)
    {
        var tracks = new List<TrackInfo>(chunks.Count);

        for (var i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            var name = chunk.Events
                .OfType<SequenceTrackNameEvent>()
                .FirstOrDefault()?.Text;

            tracks.Add(new TrackInfo(i, name, chunk.Events.Count));
        }

        return tracks;
    }

    private static List<MidiEventBase> FlattenEvents(List<TrackChunk> chunks, TempoMap tempoMap)
    {
        // Pre-calculate capacity for performance with large files
        var totalEvents = chunks.Sum(c => c.Events.Count);
        var events = new List<MidiEventBase>(totalEvents);

        for (var trackIndex = 0; trackIndex < chunks.Count; trackIndex++)
        {
            var chunk = chunks[trackIndex];
            long absoluteTick = 0;
            var originalIndex = 0;

            foreach (var midiEvent in chunk.Events)
            {
                absoluteTick += midiEvent.DeltaTime;
                var time = TimeConverter.ConvertTo<MetricTimeSpan>(absoluteTick, tempoMap);
                var timeSpan = TimeSpan.FromMicroseconds(time.TotalMicroseconds);

                var converted = ConvertEvent(midiEvent, trackIndex, absoluteTick, originalIndex, timeSpan);
                if (converted != null)
                {
                    events.Add(converted);
                }

                originalIndex++;
            }
        }

        return events;
    }

    private static MidiEventBase? ConvertEvent(
        MidiEvent midiEvent,
        int trackIndex,
        long absoluteTick,
        int originalIndex,
        TimeSpan time)
    {
        return midiEvent switch
        {
            DryNoteOnEvent noteOn when noteOn.Velocity == 0 => new NoteOffEvent
            {
                AbsoluteTick = absoluteTick,
                TrackIndex = trackIndex,
                OriginalIndex = originalIndex,
                Time = time,
                RawBytes = GetRawBytes(midiEvent),
                Channel = (byte)noteOn.Channel,
                NoteNumber = (byte)noteOn.NoteNumber,
                Velocity = 0
            },
            DryNoteOnEvent noteOn => new NoteOnEvent
            {
                AbsoluteTick = absoluteTick,
                TrackIndex = trackIndex,
                OriginalIndex = originalIndex,
                Time = time,
                RawBytes = GetRawBytes(midiEvent),
                Channel = (byte)noteOn.Channel,
                NoteNumber = (byte)noteOn.NoteNumber,
                Velocity = (byte)noteOn.Velocity
            },
            DryNoteOffEvent noteOff => new NoteOffEvent
            {
                AbsoluteTick = absoluteTick,
                TrackIndex = trackIndex,
                OriginalIndex = originalIndex,
                Time = time,
                RawBytes = GetRawBytes(midiEvent),
                Channel = (byte)noteOff.Channel,
                NoteNumber = (byte)noteOff.NoteNumber,
                Velocity = (byte)noteOff.Velocity
            },
            DryControlChangeEvent cc => new ControlChangeEvent
            {
                AbsoluteTick = absoluteTick,
                TrackIndex = trackIndex,
                OriginalIndex = originalIndex,
                Time = time,
                RawBytes = GetRawBytes(midiEvent),
                Channel = (byte)cc.Channel,
                ControllerNumber = (byte)cc.ControlNumber,
                Value = (byte)cc.ControlValue
            },
            DryProgramChangeEvent pc => new ProgramChangeEvent
            {
                AbsoluteTick = absoluteTick,
                TrackIndex = trackIndex,
                OriginalIndex = originalIndex,
                Time = time,
                RawBytes = GetRawBytes(midiEvent),
                Channel = (byte)pc.Channel,
                ProgramNumber = (byte)pc.ProgramNumber
            },
            DryPitchBendEvent pb => new PitchBendEvent
            {
                AbsoluteTick = absoluteTick,
                TrackIndex = trackIndex,
                OriginalIndex = originalIndex,
                Time = time,
                RawBytes = GetRawBytes(midiEvent),
                Channel = (byte)pb.Channel,
                Value = pb.PitchValue
            },
            ChannelAftertouchEvent ca => new ChannelPressureEvent
            {
                AbsoluteTick = absoluteTick,
                TrackIndex = trackIndex,
                OriginalIndex = originalIndex,
                Time = time,
                RawBytes = GetRawBytes(midiEvent),
                Channel = (byte)ca.Channel,
                Pressure = (byte)ca.AftertouchValue
            },
            NoteAftertouchEvent na => new PolyPressureEvent
            {
                AbsoluteTick = absoluteTick,
                TrackIndex = trackIndex,
                OriginalIndex = originalIndex,
                Time = time,
                RawBytes = GetRawBytes(midiEvent),
                Channel = (byte)na.Channel,
                NoteNumber = (byte)na.NoteNumber,
                Pressure = (byte)na.AftertouchValue
            },
            NormalSysExEvent sysex => new SysExEvent
            {
                AbsoluteTick = absoluteTick,
                TrackIndex = trackIndex,
                OriginalIndex = originalIndex,
                Time = time,
                RawBytes = GetRawBytes(midiEvent),
                Data = [0xF0, .. sysex.Data]
            },
            BaseTextEvent textEvent => new MetaEvent
            {
                AbsoluteTick = absoluteTick,
                TrackIndex = trackIndex,
                OriginalIndex = originalIndex,
                Time = time,
                RawBytes = GetRawBytes(midiEvent),
                MetaType = GetMetaType(textEvent),
                Data = System.Text.Encoding.UTF8.GetBytes(textEvent.Text ?? "")
            },
            SetTempoEvent tempo => new MetaEvent
            {
                AbsoluteTick = absoluteTick,
                TrackIndex = trackIndex,
                OriginalIndex = originalIndex,
                Time = time,
                RawBytes = GetRawBytes(midiEvent),
                MetaType = 0x51,
                Data = [(byte)(tempo.MicrosecondsPerQuarterNote >> 16),
                        (byte)(tempo.MicrosecondsPerQuarterNote >> 8),
                        (byte)tempo.MicrosecondsPerQuarterNote]
            },
            TimeSignatureEvent timeSig => new MetaEvent
            {
                AbsoluteTick = absoluteTick,
                TrackIndex = trackIndex,
                OriginalIndex = originalIndex,
                Time = time,
                RawBytes = GetRawBytes(midiEvent),
                MetaType = 0x58,
                Data = [(byte)timeSig.Numerator,
                        (byte)Math.Log2(timeSig.Denominator),
                        (byte)timeSig.ClocksPerClick,
                        (byte)timeSig.ThirtySecondNotesPerBeat]
            },
            KeySignatureEvent keySig => new MetaEvent
            {
                AbsoluteTick = absoluteTick,
                TrackIndex = trackIndex,
                OriginalIndex = originalIndex,
                Time = time,
                RawBytes = GetRawBytes(midiEvent),
                MetaType = 0x59,
                Data = [(byte)keySig.Key, (byte)keySig.Scale]
            },
            EndOfTrackEvent => new MetaEvent
            {
                AbsoluteTick = absoluteTick,
                TrackIndex = trackIndex,
                OriginalIndex = originalIndex,
                Time = time,
                RawBytes = GetRawBytes(midiEvent),
                MetaType = 0x2F,
                Data = []
            },
            _ => null // Ignore other event types
        };
    }

    private static byte GetMetaType(BaseTextEvent textEvent) => textEvent switch
    {
        TextEvent => 0x01,
        CopyrightNoticeEvent => 0x02,
        SequenceTrackNameEvent => 0x03,
        InstrumentNameEvent => 0x04,
        LyricEvent => 0x05,
        MarkerEvent => 0x06,
        CuePointEvent => 0x07,
        _ => 0x01
    };

    private static byte[] GetRawBytes(MidiEvent midiEvent)
    {
        // Reconstruct raw bytes based on event type
        return midiEvent switch
        {
            DryNoteOnEvent noteOn => [(byte)(0x90 | (byte)noteOn.Channel), (byte)noteOn.NoteNumber, (byte)noteOn.Velocity],
            DryNoteOffEvent noteOff => [(byte)(0x80 | (byte)noteOff.Channel), (byte)noteOff.NoteNumber, (byte)noteOff.Velocity],
            DryControlChangeEvent cc => [(byte)(0xB0 | (byte)cc.Channel), (byte)cc.ControlNumber, (byte)cc.ControlValue],
            DryProgramChangeEvent pc => [(byte)(0xC0 | (byte)pc.Channel), (byte)pc.ProgramNumber],
            DryPitchBendEvent pb => [(byte)(0xE0 | (byte)pb.Channel), (byte)(pb.PitchValue & 0x7F), (byte)((pb.PitchValue >> 7) & 0x7F)],
            ChannelAftertouchEvent ca => [(byte)(0xD0 | (byte)ca.Channel), (byte)ca.AftertouchValue],
            NoteAftertouchEvent na => [(byte)(0xA0 | (byte)na.Channel), (byte)na.NoteNumber, (byte)na.AftertouchValue],
            NormalSysExEvent sysex => [0xF0, .. sysex.Data],
            _ => [] // Meta events and others
        };
    }
}
