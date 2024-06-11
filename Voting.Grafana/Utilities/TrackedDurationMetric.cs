using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Voting.Grafana.Utilities;

public class TrackedDurationMetric : IDisposable
{
    private readonly long _startTimestamp;
    private readonly Histogram<double> _histogram;

    public TrackedDurationMetric(Histogram<double> histogram,
                                 long? optionalStartTimestamp = null)
    {
        _histogram = histogram;
        _startTimestamp = Stopwatch.GetTimestamp();
    }


    public void Dispose()
    {
        var elapsed = TimeProvider.System.GetElapsedTime(_startTimestamp);
        _histogram.Record(elapsed.TotalMilliseconds);
    }
}
