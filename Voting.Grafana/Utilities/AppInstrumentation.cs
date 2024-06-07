using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Voting.Grafana.Utilities;

/// <summary>
/// Stores the necessary OpenTelemetry resources and provides an ActivitySource object for the application.
/// </summary>
public class AppInstrumentation : IDisposable
{
    private const string DEFAULT_SERVICE_NAME = "unspecified-service";
    private const string DEFAULT_SERVICE_VERSION = "unspecified-version";

    private const string VOTES_TOTAL_METRIC_NAME = "votes.total";
    private const string VOTES_DURATION_METRIC_NAME = "votes.duration";

    private readonly ILogger<AppInstrumentation> _logger;
    private readonly Meter _meter;
    private Dictionary<string, string> _openTelemetryResourceAttributes = new();

    #region Properties

    public ActivitySource ActivitySource { get; private set; }
    public ReadOnlyDictionary<string, string> OpenTelemetryResourceAttributes { get; private set; }
    public string MeterName { get; private set; }
    public Counter<long> VotesTotalCounter { get; private set; }
    public Histogram<double> VoteDuration_ms { get; private set; }

    #endregion Properties

    #region Constructors

    public AppInstrumentation(ILogger<AppInstrumentation> logger)
    {
        _logger = logger;

        //set the resource attributes
        GetOpenTelemetryResourceAttributesFromEnvironment();

        //initialize the ActivitySource
        if (_openTelemetryResourceAttributes.Count != 0
           && _openTelemetryResourceAttributes.TryGetValue("service.name", out string? serviceName)
           && !string.IsNullOrEmpty(serviceName)
           && _openTelemetryResourceAttributes.TryGetValue("service.version", out string? serviceVersion)
           && !string.IsNullOrEmpty(serviceVersion))
        {
            ActivitySource = new ActivitySource(name: serviceName,
                                                version: serviceVersion);
            _meter = new Meter(name: serviceName, 
                               version: serviceVersion);
            MeterName = _meter.Name;
        }
        else
        {
            _logger.LogWarning("Required Open Telemetry Resource Attributes were not found (attributes=[service.name,service.version]). Using default values.");
            ActivitySource = new ActivitySource(name: DEFAULT_SERVICE_NAME,
                                                version: DEFAULT_SERVICE_VERSION);
            _meter = new Meter(name: DEFAULT_SERVICE_NAME,
                               version: DEFAULT_SERVICE_VERSION);
            MeterName = _meter.Name;
        }

        //initialise custom metrics
        InitializeCustomMetrics();

        //log initialization
        _logger.LogInformation("AppInstrumentation initialized.");
    }

    #endregion Constructors

    #region Public Functions

    public TrackedDurationMetric MeasureVoteDuration_ms() =>
        new(VoteDuration_ms);

    public void Dispose()
    {
        ActivitySource.Dispose();
        _meter.Dispose();

        //log disposal
        _logger.LogInformation("AppInstrumentation disposed.");
    }

    #endregion Public Functions

    #region Private Functions

    /// <summary>
    /// Gets the necessary OpenTelemetry resource attributes to construct an ActivitySource object from the environment variables.
    /// </summary>
    private void GetOpenTelemetryResourceAttributesFromEnvironment()
    {
        var openTelemetryResourceAttributesString = Environment.GetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES");
        if (string.IsNullOrWhiteSpace(openTelemetryResourceAttributesString))
        {
            _logger.LogWarning("OTEL_RESOURCE_ATTRIBUTES environment variable is not set.");
            return;
        }

        //split on comma to get key value pairs
        var keyValuePairs = openTelemetryResourceAttributesString.Split(',');

        //then split on equals to get key and value
        foreach (var keyValuePair in keyValuePairs)
        {
            var keyAndValue = keyValuePair.Split('=');
            if (keyAndValue.Length != 2)
            {
                _logger.LogWarning("Invalid key value pair in OTEL_RESOURCE_ATTRIBUTES environment variable.");
                continue;
            }
            _openTelemetryResourceAttributes.Add(keyAndValue[0], keyAndValue[1]);
        }

        //initialize the read only dictionary
        OpenTelemetryResourceAttributes = new ReadOnlyDictionary<string, string>(_openTelemetryResourceAttributes);
    }

    private void InitializeCustomMetrics()
    {
        //counters
        VotesTotalCounter = _meter.CreateCounter<long>(name: VOTES_TOTAL_METRIC_NAME,
                                                       description: "Total number of votes cast");

        //histograms
        VoteDuration_ms = _meter.CreateHistogram<double>(name: VOTES_DURATION_METRIC_NAME,
                                                         description: "Duration of time taken to vote in milliseconds",
                                                         unit: "ms");
    }

    #endregion Private Functions

}
