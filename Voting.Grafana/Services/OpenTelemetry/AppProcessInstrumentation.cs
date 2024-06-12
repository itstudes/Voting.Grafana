using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.InteropServices;

namespace Voting.Grafana.Services.OpenTelemetry;

/// <summary>
/// A service for instrumenting the application process with OpenTelemetry. 
/// This can be extended to include custom instrumentation.
/// </summary>
public class AppProcessInstrumentation : OpenTelemetryInstrumentationBase
{
    private readonly Process _currentProcess;
    private PerformanceCounter _cpuCounter;
    private bool _isWindows;
    private TimeSpan _lastTotalProcessorTime;
    private DateTime _lastTime;

    #region Properties

    /// <summary>
    /// A gauge for the application's CPU usage.
    /// </summary>
    public ObservableGauge<double> AppCpuUsageGauge { get; private set; }

    /// <summary>
    /// A gauge for the application's memory usage.
    /// </summary>
    public ObservableGauge<long> AppMemoryUsageGauge { get; private set; }

    #endregion Properties

    #region Constructors

    public AppProcessInstrumentation() : base()
    {
        //get OS info
        GetOsInformation();

        //get the current process
        _currentProcess = Process.GetCurrentProcess();
        
        //init date variables
        _lastTotalProcessorTime = _currentProcess.TotalProcessorTime;
        _lastTime = DateTime.UtcNow;

        //initialize process metrics
        InitializeProcessMetrics();
    }

    public AppProcessInstrumentation(string serviceName,
                                     string serviceVersion) : base(serviceName, serviceVersion)
    {
        //get OS info
        GetOsInformation();

        //get the current process
        _currentProcess = Process.GetCurrentProcess();

        //init date variables
        _lastTotalProcessorTime = _currentProcess.TotalProcessorTime;
        _lastTime = DateTime.UtcNow;

        //initialize process metrics
        InitializeProcessMetrics();
    }

    #endregion Constructors

    #region Private Methods

    private void InitializeProcessMetrics()
    {
        //initialize gauge metrics
        AppCpuUsageGauge = _meter.CreateObservableGauge<double>(name: "app.cpu.usage",
                                                                observeValue: () => GetCpuUsage(),
                                                                description: "Application CPU usage percentage", 
                                                                unit: "percentage");
        AppMemoryUsageGauge = _meter.CreateObservableGauge<long>(name: "app.memory.usage",
                                                                 observeValue: () => GetMemoryUsageMb(),
                                                                 description: "Application memory usage in megabytes",
                                                                 unit: "mbytes");
    }

    private void GetOsInformation()
    {
        //get OS platform
        var osPlatform = RuntimeInformation.OSDescription;
        _isWindows = false;

        //check if Windows
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            //initialise performance counters
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _isWindows = true;
        }
        //check if Linux
        else if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) 
                 && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException($"OS platform not supported for application process metrics (OS = {osPlatform})");
        }

    }

    #region process metrics

    private double GetCpuUsage()
    {
        double returnVal = 0;
        _currentProcess.Refresh();
        //check if Windows
        if (_isWindows)
        {            
            returnVal = _currentProcess.TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount;
        }
        //linux
        else
        {
            try
            {
                var procStat = File.ReadAllText($"/proc/{_currentProcess.Id}/stat").Split(' ');
                var utime = double.Parse(procStat[13]);
                var stime = double.Parse(procStat[14]);
                var totalCpuTime = utime + stime;

                var procUptime = File.ReadAllText("/proc/uptime").Split(' ')[0];
                var uptime = double.Parse(procUptime);

                returnVal = 100.0 * (totalCpuTime / uptime) / Environment.ProcessorCount;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error reading application CPU usage [Linux].");
                returnVal = 0;
            }
        }
        return returnVal;
    }

    private long GetMemoryUsageMb()
    {
        _currentProcess.Refresh();
        return _currentProcess.WorkingSet64 / (1024 * 1024);
    }

    #endregion process metrics

    #endregion Private Methods
}
