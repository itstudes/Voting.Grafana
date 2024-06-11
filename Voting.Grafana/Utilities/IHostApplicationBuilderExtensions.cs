using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Voting.Grafana.Utilities;

/// <summary>
/// A collection of extension methods for the IHostApplicationBuilder interface.
/// </summary>
public static class IHostApplicationBuilderExtensions
{
    #region Public Methods

    /// <summary>
    /// Configures the OpenTelemetry logging, metrics, and tracing for the host application builder.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns>returns the updated IHostApplicationBuilder object</returns>
    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        //get AppInstrumentation service
        var appInstrumentation = builder.Services.BuildServiceProvider().GetRequiredService<AppInstrumentation>();

        //logging
        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
        });

        //metrics and tracing
        builder.Services.AddOpenTelemetry()
            .WithMetrics(options =>
            {
                //add dotnet instrumentation
                options.AddRuntimeInstrumentation()
                       .AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation();

                //add custom metering
                options.AddMeter(appInstrumentation.MeterName);
            })
            .WithTracing(options =>
            {
                //high speed sampling in dev
                if (builder.Environment.IsDevelopment())
                {
                    options.SetSampler<AlwaysOnSampler>();
                }

                //add dotnet instrumentation
                options.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation();
            });

        //exporters
        builder.AddOpenTelemetryExporters();

        //log that open telemetry has been configured
        Log.Information("OpenTelemetry configuration completed.");

        return builder;
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Adds OpenTelemetry exporters to the host application builder.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns>returns the updated IHostApplicationBuilder object</returns>
    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        //OTLP exporter if defined in env variables
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        if (useOtlpExporter)
        {
            builder.Services.Configure<OpenTelemetryLoggerOptions>(options => options.AddOtlpExporter());
            builder.Services.ConfigureOpenTelemetryMeterProvider(options => options.AddOtlpExporter());
            builder.Services.ConfigureOpenTelemetryTracerProvider(options => options.AddOtlpExporter());
        }

        //console exporters in dev
        if (builder.Environment.IsDevelopment())
        {
            //builder.Services.Configure<OpenTelemetryLoggerOptions>(options => options.AddConsoleExporter());
            //builder.Services.ConfigureOpenTelemetryMeterProvider(options => options.AddConsoleExporter());
            //builder.Services.ConfigureOpenTelemetryTracerProvider(options => options.AddConsoleExporter());
        }

        //prometheus exporter for metrics
        builder.Services.AddOpenTelemetry()
                        .WithMetrics(m => m.AddPrometheusExporter());

        //jaeger exporter for tracing
        //builder.Services.AddOpenTelemetry()
        //                .WithTracing(t => t.AddOtlpExporter(options =>
        //                {
        //                    options.
        //                }));

        return builder;
    }

    #endregion Private Methods
}
