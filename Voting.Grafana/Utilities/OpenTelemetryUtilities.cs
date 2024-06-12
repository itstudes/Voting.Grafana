namespace Voting.Grafana.Utilities;

/// <summary>
/// A collection of utility methods for working with OpenTelemetry.
/// </summary>
public static class OpenTelemetryUtilities
{
    /// <summary>
    /// Gets the OpenTelemetry resource attributes from the OTEL_RESOURCE_ATTRIBUTES variable.
    /// </summary>
    public static Dictionary<string, string> GetOpenTelemetryResourceAttributesFromEnvironment()
    {
        var openTelemetryResourceAttributes = new Dictionary<string, string>();

        //try to get the OTEL_RESOURCE_ATTRIBUTES environment variable
        var openTelemetryResourceAttributesString = Environment.GetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES");
        if (string.IsNullOrWhiteSpace(openTelemetryResourceAttributesString))
        {
            Log.Warning("OTEL_RESOURCE_ATTRIBUTES environment variable is not set.");
            return openTelemetryResourceAttributes;
        }

        //split on comma to get key value pairs
        var keyValuePairs = openTelemetryResourceAttributesString.Split(',');

        //then split on equals to get key and value
        foreach (var keyValuePair in keyValuePairs)
        {
            var keyAndValue = keyValuePair.Split('=');
            if (keyAndValue.Length != 2)
            {
                Log.Warning("Invalid key value pair in OTEL_RESOURCE_ATTRIBUTES environment variable.");
                continue;
            }
            openTelemetryResourceAttributes.Add(keyAndValue[0], keyAndValue[1]);
        }

        return openTelemetryResourceAttributes;
    }

}
