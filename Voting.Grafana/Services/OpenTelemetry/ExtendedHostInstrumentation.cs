using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.InteropServices;

namespace Voting.Grafana.Services.OpenTelemetry;

/// <summary>
/// A class for instrumenting the host metrics with OpenTelemetry.
/// </summary>
public sealed class ExtendedHostInstrumentation : OpenTelemetryInstrumentationBase
{
    private PerformanceCounter _cpuCounter;
    private PerformanceCounter _memoryCounter;
    private PerformanceCounter _ioReadCounter;
    private PerformanceCounter _ioWriteCounter;
    private PerformanceCounter _networkReceiveCounter;
    private PerformanceCounter _networkSendCounter;

    #region Properties

    /// <summary>
    /// A flag indicating if the host OS is Windows.
    /// </summary>
    public bool IsWindows { get; private set; }

    /// <summary>
    /// A flag indicating if the host OS is Linux.
    /// </summary>
    public bool IsLinux { get; private set; }

    /// <summary>
    /// A gauge for the system CPU usage.
    /// </summary>
    public ObservableGauge<double> SystemCpuUsageGauge { get; private set; }

    /// <summary>
    /// A gauge for the system memory availability.
    /// </summary>
    public ObservableGauge<double> SystemMemoryAvailabilityGauge { get; private set; }

    /// <summary>
    /// A gauge for the system IO read bytes.
    /// </summary>
    public ObservableGauge<long> SystemIoReadBytesGauge { get; private set; }

    /// <summary>
    /// A gauge for the system IO write bytes.
    /// </summary>
    public ObservableGauge<long> SystemIoWriteBytesGauge { get; private set; }

    /// <summary>
    /// A gauge for the system network receive bytes.
    /// </summary>
    public ObservableGauge<long> SystemNetworkReceiveBytesGauge { get; private set; }

    /// <summary>
    /// A gauge for the system network send bytes.
    /// </summary>
    public ObservableGauge<long> SystemNetworkSendBytesGauge { get; private set; }

    #endregion Properties

    #region Constructors

    public ExtendedHostInstrumentation() : base()
    {
        //get OS
        GetOsInformation();

        //initialize metrics
        InitializeExtendedHostMetrics();

        //log initialization
        Log.Information("Extended host instrumentation initialized.");
    }

    public ExtendedHostInstrumentation(string serviceName,
                                       string? serviceVersion = null) :
           base(serviceName, serviceVersion)
    {
        //get OS
        GetOsInformation();

        //initialize custom metrics
        InitializeExtendedHostMetrics();

        //log initialization
        Log.Information("Extended host instrumentation initialized.");
    }

    #endregion Constructors

    #region Public Methods

    public override void Dispose()
    {
        //dispose performance counters
        if (IsWindows)
        {
            _cpuCounter?.Dispose();
            _memoryCounter?.Dispose();
            _ioReadCounter?.Dispose();
            _ioWriteCounter?.Dispose();
            _networkReceiveCounter?.Dispose();
            _networkSendCounter?.Dispose();
        }

        //dispose base (this is done last to ensure the meter is disposed last)
        base.Dispose();
    }

    #endregion Public Methods

    #region Private Methods

    private void GetOsInformation()
    {
        //get OS platform
        var osPlatform = RuntimeInformation.OSDescription;

        //check if Windows
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            IsWindows = true;

            //initialise performance counters
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
            _ioReadCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
            _ioWriteCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
            _networkReceiveCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", "Ethernet");
            _networkSendCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", "Ethernet");            
        }
        //check if Linux
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            IsLinux = true;
        }
        //log if not supported
        else
        {
            //throw exception
            throw new PlatformNotSupportedException($"OS platform not supported (OS = {osPlatform})");
        }

        //log OS
        Log.Information("OS platform: {osPlatform}", osPlatform);
    }

    private void InitializeExtendedHostMetrics()
    {
        //initialize gauge metrics
        SystemCpuUsageGauge = _meter.CreateObservableGauge<double>(name: "system.cpu.usage",
                                                                   observeValue: () => IsWindows ?
                                                                                       GetCpuUsage_Windows() :
                                                                                       GetCpuUsage_Linux(),
                                                                   description: "System CPU usage percentage",
                                                                   unit:"percentage");
        SystemMemoryAvailabilityGauge = _meter.CreateObservableGauge<double>(name: "system.memory.availability",
                                                                             observeValue: () => IsWindows ?
                                                                                                 GetMemoryAvailability_Windows() :
                                                                                                 GetMemoryAvailability_Linux(),
                                                                             description: "System memory availability in megabytes",
                                                                             unit: "mbytes");
        SystemIoReadBytesGauge = _meter.CreateObservableGauge<long>(name: "system.io.read",
                                                                    observeValue: () => IsWindows ?
                                                                                        GetIoReadBytes_Windows() :
                                                                                        GetIoReadBytes_Linux(),
                                                                    description: "System IO read in bytes", 
                                                                    unit: "bytes");
        SystemIoWriteBytesGauge = _meter.CreateObservableGauge<long>(name: "system.io.write",
                                                                     observeValue: () => IsWindows ?
                                                                                         GetIoWriteByte_Windows() :
                                                                                         GetIoWriteBytes_Linux(),
                                                                     description: "System IO write in bytes",
                                                                     unit: "bytes");
        SystemNetworkReceiveBytesGauge = _meter.CreateObservableGauge<long>(name: "system.network.receive",
                                                                            observeValue: () => IsWindows ?
                                                                                                GetNetworkReceiveBytes_Windows() :
                                                                                                GetNetworkReceiveBytes_Linux(),
                                                                            description: "System network receive in bytes", 
                                                                            unit: "bytes");
        SystemNetworkSendBytesGauge = _meter.CreateObservableGauge<long>(name: "system.network.send",
                                                                         observeValue: () => IsWindows ?
                                                                                            GetNetworkSendBytes_Windows() :
                                                                                            GetNetworkSendBytes_Linux(),
                                                                         description: "System network send in bytes",
                                                                         unit: "bytes");
    }

    #region windows metrics

    private double GetCpuUsage_Windows() =>
        _cpuCounter.NextValue();

    private double GetMemoryAvailability_Windows() =>
        _memoryCounter.NextValue();

    private long GetIoReadBytes_Windows() =>
        (long)_ioReadCounter.NextValue();

    private long GetIoWriteByte_Windows() =>
        (long)_ioWriteCounter.NextValue();

    private long GetNetworkReceiveBytes_Windows() =>
        (long)_networkReceiveCounter.NextValue();

    private long GetNetworkSendBytes_Windows() =>
        (long)_networkSendCounter.NextValue();

    #endregion windows metrics

    #region linux metrics

    private static double GetCpuUsage_Linux()
    {
        try
        {
            var cpuInfo = File.ReadAllText("/proc/stat").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)[0].Split(' ');
            var idleTime = double.Parse(cpuInfo[4]);
            var totalTime = 0.0;
            foreach (var time in cpuInfo)
            {
                if (double.TryParse(time, out var parsedTime))
                {
                    totalTime += parsedTime;
                }
            }
            return 100.0 - (idleTime / totalTime * 100.0);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error reading CPU usage [Linux].");
            return 0;
        }
    }

    private static double GetMemoryAvailability_Linux()
    {
        try
        {
            var memInfo = File.ReadAllLines("/proc/meminfo");
            var availableMemoryLine = Array.Find(memInfo, line => line.StartsWith("MemAvailable:"));
            var availableMemory = double.Parse(availableMemoryLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1]);
            return availableMemory / 1024; // Convert from KB to MB
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error reading memory availability [Linux].");
            return 0;
        }
    }

    private static long GetIoReadBytes_Linux()
    {
        try
        {
            var ioStats = File.ReadAllLines("/proc/diskstats");
            long totalReadBytes = 0;
            foreach (var line in ioStats)
            {
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 5)
                {
                    totalReadBytes += long.Parse(parts[5]) * 512; // Sectors to bytes
                }
            }
            return totalReadBytes;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error reading IO read bytes [Linux].");
            return 0;
        }
    }

    private static long GetIoWriteBytes_Linux()
    {
        try
        {
            var ioStats = File.ReadAllLines("/proc/diskstats");
            long totalWriteBytes = 0;
            foreach (var line in ioStats)
            {
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 9)
                {
                    totalWriteBytes += long.Parse(parts[9]) * 512; // Sectors to bytes
                }
            }
            return totalWriteBytes;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error reading IO write bytes [Linux].");
            return 0;
        }
    }

    private static long GetNetworkReceiveBytes_Linux()
    {
        try
        {
            var networkStats = File.ReadAllLines("/proc/net/dev");
            long totalReceiveBytes = 0;
            foreach (var line in networkStats)
            {
                if (line.Contains(":"))
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        totalReceiveBytes += long.Parse(parts[1]);
                    }
                }
            }
            return totalReceiveBytes;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error reading network RX bytes [Linux].");
            return 0;
        }
    }

    private static long GetNetworkSendBytes_Linux()
    {
        try
        {
            var networkStats = File.ReadAllLines("/proc/net/dev");
            long totalSendBytes = 0;
            foreach (var line in networkStats)
            {
                if (line.Contains(":"))
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 9)
                    {
                        totalSendBytes += long.Parse(parts[9]);
                    }
                }
            }
            return totalSendBytes;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error reading network TX bytes [Linux].");
            return 0;
        }
    }

    #endregion linux metrics

    #endregion Private Methods

}