using System.Collections.Concurrent;
using System.Diagnostics;

namespace Voting.Grafana.Services;

/// <summary>
/// A singleton service that manages the running of k6 tests and the retrieval of their results.
/// </summary>
public sealed class K6TestManager
{
    private static readonly ConcurrentDictionary<Guid, string> _testResults = new();
    private static bool _isTestRunning = false;
    private static Guid _ongoingTestId = Guid.Empty;
    private static readonly object _singleTestLock = new();
    private readonly ILogger<K6TestManager> _logger;

    public bool IsTestRunning => _isTestRunning;
    public Guid OngoingTestId => _ongoingTestId;

    public K6TestManager(ILogger<K6TestManager> logger) =>
        _logger = logger;

    #region Public Functions

    /// <summary>
    /// Tries to run a k6 test with the given script name. Outputs the unique test ID and results.
    /// </summary>
    /// <param name="scriptName"></param>
    /// <param name="testId"></param>
    /// <param name="results"></param>
    /// <returns>returns true if the test could be run, otherwise returns false</returns>
    public bool TryRunK6Test(string scriptName, 
                             out Guid testId, 
                             out string results)
    {
        //init values
        results = string.Empty;
        testId = Guid.Empty;

        //ensure only one test is running at a time
        lock (_singleTestLock)
        {
            if (_isTestRunning)
            {
                results = $"A test is already running (testId = {_ongoingTestId})";
                return false;
            }

            //start the test
            _isTestRunning = true;
        }

        //setup the test process data
        testId = Guid.NewGuid();
        _ongoingTestId = testId;
        //var dockerPath = GetDockerExecutablePath();
        var dockerPath = GetDockerExecutablePathInsideContainer();
        var workingDirectory = Directory.GetCurrentDirectory();
        var processInfo = new ProcessStartInfo(dockerPath, $"exec k6 k6 run /scripts/{scriptName}")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };

        //configure the process to run the test
        var process = new Process { StartInfo = processInfo };
        //configure event handlers for process
        DataReceivedEventHandler outputHandler = (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                _testResults.AddOrUpdate(_ongoingTestId, args.Data + Environment.NewLine, (key, existingValue) => existingValue + args.Data + Environment.NewLine);
                _logger.LogInformation("Test Results for testId={testId}: \n{testResults}", 
                                       _ongoingTestId, 
                                       args.Data);
            }
        };
        DataReceivedEventHandler errorHandler = (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                _testResults.AddOrUpdate(_ongoingTestId, args.Data + Environment.NewLine, (key, existingValue) => existingValue + args.Data + Environment.NewLine);
                _logger.LogInformation("Test Results for testId={testId}: \n{testResults}",
                                       _ongoingTestId,
                                       args.Data);
            }
        };
        process.OutputDataReceived += outputHandler;
        process.ErrorDataReceived += errorHandler;
        process.Exited += (sender, args) =>
        {
            _logger.LogInformation("Completed k6 test with test ID (testId={testId})", _ongoingTestId);
            process.OutputDataReceived -= outputHandler;
            process.ErrorDataReceived -= errorHandler;
            lock (_singleTestLock)
            {
                _isTestRunning = false;
                _ongoingTestId = Guid.Empty;
            }
        };

        //start the process
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        //return success
        return true;
    }

    /// <summary>
    /// Tries to get the results for a given test
    /// </summary>
    /// <param name="testId"></param>
    /// <returns>returns the results as a string or an empty string if no results could be found for the ID</returns>
    public string GetTestResults(Guid testId) =>
        _testResults.TryGetValue(testId, out var result) ? 
        result : 
        string.Empty;

    #endregion Public Functions

    #region Private Functions

    /// <summary>
    /// Get the path to the docker executable
    /// </summary>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    private static string GetDockerExecutablePath()
    {
        var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator);
        if (paths != null)
        {
            foreach (var path in paths)
            {
                var dockerPath = Path.Combine(path, "docker");
                if (File.Exists(dockerPath))
                {
                    return dockerPath;
                }
            }
        }
        throw new FileNotFoundException("Docker executable not found in PATH.");
    }

    /// <summary>
    /// Get the path to the docker executable inside the container
    /// </summary>
    /// <returns></returns>
    private static string GetDockerExecutablePathInsideContainer() =>
        "/usr/bin/docker";

    #endregion Private Functions
}
