namespace Structa.Rendering;

/// <summary>
/// FPS suavizado por média móvel sobre os últimos frames, calculado a partir do delta time.
/// </summary>
public sealed class FrameStats
{
    private const int SampleWindow = 30;

    private readonly Queue<double> _samples = new(SampleWindow);
    private double _accumulatedSeconds;

    public double FramesPerSecond { get; private set; }

    public double FrameTimeMilliseconds { get; private set; }

    public void Tick(double deltaSeconds)
    {
        if (deltaSeconds <= 0)
        {
            return;
        }

        FrameTimeMilliseconds = deltaSeconds * 1000.0;

        _samples.Enqueue(deltaSeconds);
        _accumulatedSeconds += deltaSeconds;

        if (_samples.Count > SampleWindow)
        {
            _accumulatedSeconds -= _samples.Dequeue();
        }

        var averageSeconds = _accumulatedSeconds / _samples.Count;
        FramesPerSecond = averageSeconds > 0 ? 1.0 / averageSeconds : 0;
    }
}
