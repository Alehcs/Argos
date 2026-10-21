namespace Argos.Engine;

/// <summary>
/// Whether the recording loses frames: late to render or skipped by the encoder, over 2% of a minute's frames and at
/// least 30 of them. The first 30 seconds after the buffer or a recording starts are left out. Once seen it stays until
/// the pipeline is built again, so the Status page can still suggest the lighter mode after the game.
/// </summary>
public sealed class FrameHealth
{
    static readonly TimeSpan Window = TimeSpan.FromSeconds(60);
    static readonly TimeSpan Settle = TimeSpan.FromSeconds(30);
    const double MaxLostShare = 0.02;
    const long MinLost = 30;

    readonly Queue<(DateTime At, long Total, long Lost)> _samples = new();
    DateTime? _activeFrom;

    public bool Losing { get; private set; }

    /// <param name="active">The replay buffer or a recording runs: frames are being encoded.</param>
    /// <param name="total">Frames made so far; with <paramref name="lost"/>, counters that only grow until a restart.</param>
    /// <param name="lost">Frames late to render plus frames the encoder skipped.</param>
    public void Sample(DateTime now, bool active, long total, long lost)
    {
        if (!active)
        {
            _activeFrom = null;
            _samples.Clear();
            return;
        }
        _activeFrom ??= now;
        if (now - _activeFrom.Value < Settle)
            return;
        if (_samples.Count > 0 && (total < _samples.Last().Total || lost < _samples.Last().Lost))
            _samples.Clear(); // the counters started over
        _samples.Enqueue((now, total, lost));
        while (now - _samples.Peek().At > Window)
            _samples.Dequeue();
        var (_, firstTotal, firstLost) = _samples.Peek();
        long frames = total - firstTotal, dropped = lost - firstLost;
        if (dropped >= MinLost && dropped > frames * MaxLostShare)
            Losing = true;
    }
}
