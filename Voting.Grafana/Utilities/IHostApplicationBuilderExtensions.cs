using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Voting.Grafana.Services.OpenTelemetry;

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
        //get AppCustomInstrumentation service
        var extendedHostInstrumention = builder.Services.BuildServiceProvider()
                                                        .GetService<ExtendedHostInstrumentation>();
        var appInstrumentation = builder.Services.BuildServiceProvider()
                                                 .GetRequiredService<AppCustomInstrumentation>();


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

                //add extended host instrumentation
                if(extendedHostInstrumention is not null)
                {
                    //add custom metrics
                    options.AddMeter(extendedHostInstrumention.MeterName);
                }

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
        //get appSettings.json configuration
        var configuration = builder.Configuration;

        //prometheus exporter for metrics
        builder.Services.AddOpenTelemetry()
                        .WithMetrics(m => m.AddPrometheusExporter());

        //standard OTLP exporter for tracing
        //- configured to point to tempo instance
        var otlpEndpoint = configuration.GetValue<string>("OpenTelemetry:Exporters:TracingExporter") ??
                           "http://localhost:4317";
        builder.Services.AddOpenTelemetry()
                        .WithTracing(t => t.AddOtlpExporter(options =>
                        {
                            options.Endpoint = new Uri(otlpEndpoint);
                        }));

        //OTLP exporter if defined in env variables
        //var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        //if (useOtlpExporter)
        //{
        //    builder.Services.Configure<OpenTelemetryLoggerOptions>(options => options.AddOtlpExporter());
        //    builder.Services.ConfigureOpenTelemetryMeterProvider(options => options.AddOtlpExporter());
        //    builder.Services.ConfigureOpenTelemetryTracerProvider(options => options.AddOtlpExporter());
        //}

        //console exporters in dev
        //if (builder.Environment.IsDevelopment())
        //{
        //    builder.Services.Configure<OpenTelemetryLoggerOptions>(options => options.AddConsoleExporter());
        //    builder.Services.ConfigureOpenTelemetryMeterProvider(options => options.AddConsoleExporter());
        //    builder.Services.ConfigureOpenTelemetryTracerProvider(options => options.AddConsoleExporter());
        //}

        return builder;
    }

    #endregion Private Methods
}
