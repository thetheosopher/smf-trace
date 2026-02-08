using System.Globalization;
using System.IO;
using System.Linq;
using SMFTrace.Core.Models;
using SMFTrace.Core.Sequencer;

namespace SMFTrace.Wpf.ViewModels;

public sealed record PlaylistMetadata(
    string Title,
    TimeSpan Duration,
    double TempoMin,
    double TempoMax,
    string TimeSignature,
    string KeySignature,
    string SmfType,
    int TrackCount,
    bool HasSysExEvents,
    bool HasLyrics);

public static class PlaylistMetadataParser
{
    public static PlaylistMetadata Parse(string filePath)
    {
        var data = MidiFileLoader.Load(filePath);
        return Parse(data, Path.GetFileNameWithoutExtension(filePath));
    }

    public static PlaylistMetadata Parse(MidiFileData data, string fallbackTitle)
    {
        var title = GetTitle(data) ?? fallbackTitle;
        var (tempoMin, tempoMax) = GetTempoRange(data);
        var timeSignature = GetTimeSignature(data) ?? "4/4";
        var keySignature = GetKeySignature(data) ?? "-";
        var smfType = data.Format switch
        {
            0 => "Type 0",
            1 => "Type 1",
            2 => "Type 2",
            _ => $"Type {data.Format}"
        };

        var hasSysExEvents = data.Events.OfType<SysExEvent>().Any();
        var hasLyrics = data.Events.OfType<MetaEvent>().Any(evt => evt.MetaType == 0x05);

        return new PlaylistMetadata(
            title,
            data.Duration,
            tempoMin,
            tempoMax,
            timeSignature,
            keySignature,
            smfType,
            data.Tracks.Count,
            hasSysExEvents,
            hasLyrics);
    }

    public static string FormatTempoDisplay(double tempoMin, double tempoMax)
    {
        var min = Math.Round(tempoMin, 1, MidpointRounding.AwayFromZero);
        var max = Math.Round(tempoMax, 1, MidpointRounding.AwayFromZero);

        if (Math.Abs(min - max) < 0.01)
        {
            return $"{min:0.#} BPM";
        }

        return $"{min:0.#}-{max:0.#} BPM";
    }

    public static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
        {
            return duration.ToString("hh\\:mm\\:ss", CultureInfo.InvariantCulture);
        }

        return duration.ToString("mm\\:ss", CultureInfo.InvariantCulture);
    }

    private static string? GetTitle(MidiFileData data)
    {
        if (data.Events.Count == 0)
        {
            return null;
        }

        var titleEvent = data.Events
            .OfType<MetaEvent>()
            .Where(evt => evt.IsTrackName && evt.TrackIndex == 0)
            .OrderBy(evt => evt.AbsoluteTick)
            .ThenBy(evt => evt.OriginalIndex)
            .FirstOrDefault();

        return titleEvent?.TextContent;
    }

    private static (double Min, double Max) GetTempoRange(MidiFileData data)
    {
        var tempos = data.Events
            .OfType<MetaEvent>()
            .Where(evt => evt.IsSetTempo)
            .Select(evt => evt.Bpm)
            .ToList();

        if (tempos.Count == 0)
        {
            return (120.0, 120.0);
        }

        return (tempos.Min(), tempos.Max());
    }

    private static string? GetTimeSignature(MidiFileData data)
    {
        var timeSig = data.Events
            .OfType<MetaEvent>()
            .FirstOrDefault(evt => evt.IsTimeSignature && evt.Data.Length >= 2);

        if (timeSig == null)
        {
            return null;
        }

        var numerator = timeSig.Data[0];
        var denominator = 1 << timeSig.Data[1];
        return $"{numerator}/{denominator}";
    }

    private static string? GetKeySignature(MidiFileData data)
    {
        var keySig = data.Events
            .OfType<MetaEvent>()
            .FirstOrDefault(evt => evt.MetaType == 0x59 && evt.Data.Length >= 2);

        if (keySig == null)
        {
            return null;
        }

        var sf = unchecked((sbyte)keySig.Data[0]);
        var isMinor = keySig.Data[1] != 0;
        var key = KeySignatureFromSharps(sf, isMinor);
        return string.IsNullOrEmpty(key) ? null : key;
    }

    private static string KeySignatureFromSharps(int sharps, bool isMinor)
    {
        var majorKeys = new[]
        {
            "Cb", "Gb", "Db", "Ab", "Eb", "Bb", "F", "C", "G", "D", "A", "E", "B", "F#", "C#"
        };
        var minorKeys = new[]
        {
            "Abm", "Ebm", "Bbm", "Fm", "Cm", "Gm", "Dm", "Am", "Em", "Bm", "F#m", "C#m", "G#m", "D#m", "A#m"
        };

        var index = sharps + 7;
        if (index < 0 || index >= majorKeys.Length)
        {
            return string.Empty;
        }

        return isMinor ? minorKeys[index] : majorKeys[index];
    }
}
