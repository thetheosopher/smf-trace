namespace SMFTrace.Core.Sequencer;

/// <summary>
/// Playback state of the sequencer.
/// </summary>
public enum PlaybackState
{
    /// <summary>Playback is stopped (position at start).</summary>
    Stopped,

    /// <summary>Playback is in progress.</summary>
    Playing,

    /// <summary>Playback is paused at current position.</summary>
    Paused,

    /// <summary>User is scrubbing (seeking).</summary>
    Scrubbing
}
