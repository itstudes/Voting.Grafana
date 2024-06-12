using System.Diagnostics.Metrics;

namespace Voting.Grafana.Services.OpenTelemetry;

/// <summary>
/// A service for instrumenting custom application metrics with OpenTelemetry.
/// </summary>
public class AppCustomInstrumentation : AppProcessInstrumentation
{


    #region Properties

    public Counter<long> VotesTotalCounter { get; private set; }
    public Histogram<double> VoteDuration_ms { get; private set; }

    #endregion Properties

    #region Constructors

    public AppCustomInstrumentation() : base()
    {
        //initialise custom metrics
        InitializeCustomMetrics();

        //log initialization
        Log.Information("Custom application instrumentation initialized.");
    }

    public AppCustomInstrumentation(string serviceName,
                                    string serviceVersion) : base(serviceName, serviceVersion)
    {
        //initialise custom metrics
        InitializeCustomMetrics();

        //log initialization
        Log.Information("Custom application instrumentation initialized.");
    }

    #endregion Constructors

    #region Public Functions

    public TrackedDurationMetric MeasureVoteDuration_ms() =>
        new(VoteDuration_ms);

    public void Dispose()
    {
        //log disposal
        Log.Information("Custom application instrumentation disposed.");
    }

    #endregion Public Functions

    #region Private Functions

    private void InitializeCustomMetrics()
    {
        //counters
        VotesTotalCounter = _meter.CreateCounter<long>(name: "votes.total",
                                                       description: "Total number of votes cast");

        //histograms
        VoteDuration_ms = _meter.CreateHistogram<double>(name: "votes.duration",
                                                         description: "Duration of time taken to vote in milliseconds",
                                                         unit: "ms");
    }

    #endregion Private Functions

}
