using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Voting.Grafana.Services.OpenTelemetry;

/// <summary>
/// A base class for OpenTelemetry instrumentation services.
/// </summary>
public abstract class OpenTelemetryInstrumentationBase : IDisposable
{
    private const string DEFAULT_SERVICE_NAME = "unspecified-service";
    private const string DEFAULT_SERVICE_VERSION = "unspecified-version";

    protected readonly Meter _meter;
    private Dictionary<string, string> _openTelemetryResourceAttributes = new();

    #region Properties

    /// <summary>
    /// Stores the ActivitySource object for the application.
    /// </summary>
    public ActivitySource ActivitySource { get; private set; }

    /// <summary>
    /// A read-only dictionary of OpenTelemetry resource attributes from the OTEL_RESOURCE_ATTRIBUTES environment/appSettings variable.
    /// </summary>
    public ReadOnlyDictionary<string, string> OpenTelemetryResourceAttributes { get; private set; }

    /// <summary>
    /// The name of the meter used for the metrics.
    /// </summary>
    public string MeterName { get; private set; }

    #endregion Properties

    #region Constructors

    public OpenTelemetryInstrumentationBase()
    {
        //set the resource attributes
        _openTelemetryResourceAttributes = OpenTelemetryUtilities.GetOpenTelemetryResourceAttributesFromEnvironment();
        OpenTelemetryResourceAttributes = new ReadOnlyDictionary<string, string>(_openTelemetryResourceAttributes);

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
            Log.Warning("Required Open Telemetry Resource Attributes were not found (attributes=[service.name,service.version]). Using default values.");
            ActivitySource = new ActivitySource(name: DEFAULT_SERVICE_NAME,
                                                version: DEFAULT_SERVICE_VERSION);
            _meter = new Meter(name: DEFAULT_SERVICE_NAME,
                               version: DEFAULT_SERVICE_VERSION);
            MeterName = _meter.Name;
        }
    }

    public OpenTelemetryInstrumentationBase(string serviceName, 
                                            string? serviceVersion = null)
    {
        //set meter and activity data
        ActivitySource = new ActivitySource(name: serviceName,
                                            version: serviceVersion);
        _meter = new Meter(name: serviceName,
                           version: serviceVersion);
        MeterName = _meter.Name;

        //try get otel variables and set the attributes 
        _openTelemetryResourceAttributes = OpenTelemetryUtilities.GetOpenTelemetryResourceAttributesFromEnvironment();
        OpenTelemetryResourceAttributes = new ReadOnlyDictionary<string, string>(_openTelemetryResourceAttributes);
    }

    #endregion Constructors

    #region Public Methods

    public virtual void Dispose()
    {
        ActivitySource.Dispose();
        _meter.Dispose();
    }

    #endregion Public Methods
}
