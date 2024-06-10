namespace Voting.Grafana.Models;

/// <summary>
/// A web model that represents the status of a k6 test.
/// </summary>
public class K6TestStatus
{
    public bool IsTestRunning { get; set; }
    public Guid? OngoingTestId { get; set; } = null;
}

/// <summary>
/// A web model that represents the results of a k6 test.
/// </summary>
public class K6TestResult
{
    public Guid TestId { get; set; }
    public string Results { get; set; } = string.Empty;
}

/// <summary>
/// A web model that represents a request to run a k6 test.
/// <summary/>
public class K6TestRequest
{
    public string ScriptName { get; set; } = string.Empty;
}
