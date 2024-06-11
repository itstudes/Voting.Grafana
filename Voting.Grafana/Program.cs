using Serilog.Sinks.Grafana.Loki;

namespace Voting.Grafana;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            // Read OTEL_RESOURCE_ATTRIBUTES environment variable
            var resourceAttributes = OpenTelemetryUtilities.GetOpenTelemetryResourceAttributesFromEnvironment();
            resourceAttributes.TryGetValue("service.name", out var serviceName);
            resourceAttributes.TryGetValue("service.version", out var serviceVersion);

            //configure serilog
            Log.Logger = new LoggerConfiguration()
                           .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                           .Enrich.WithProperty("ServiceName", serviceName ?? "UnknownService")
                           .Enrich.WithProperty("ServiceVersion", serviceVersion ?? "UnknownVersion")
                           .WriteTo.Console()
                           .WriteTo.GrafanaLoki(uri: "http://loki:3100",
                                                labels:
                                                [
                                                    new() { Key="ServiceName", Value= serviceName ?? "UnknownService" },
                                                    new() { Key="ServiceVersion", Value= serviceVersion ?? "UnknownVersion" },
                                                    new() { Key="env", Value=Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "UnknownEnvironment" }
                                                ])
                           .CreateLogger();

            //create the app builder
            var builder = WebApplication.CreateBuilder(args);

            //configure the app to use serilog
            builder.Services.AddSerilog();
            //Uncomment the following line to enable Serilog debug logging
            //Serilog.Debugging.SelfLog.Enable(msg => Console.WriteLine(msg));

            //add diagnostic instrumentation for app (for open telemetry)
            builder.Services.AddSingleton<AppInstrumentation>();

            //add open telemetry
            builder.ConfigureOpenTelemetry();

            //add voting services
            builder.Services.AddSingleton<RegisteredPartiesService>();
            builder.Services.AddSingleton<VotingRoundManagementService>();

            //add k6 test manager
            builder.Services.AddSingleton<K6TestManager>();

            //add asp.net core services to the container
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            //build the app
            var app = builder.Build();

            //configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            //configure the app to use HTTPS redirection and authorization
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            //add serilog request logging
            app.UseSerilogRequestLogging();

            //configure the app to use the OpenTelemetry middleware
            app.MapPrometheusScrapingEndpoint();

            //test log message
            Log.Information("This is a test log message to verify Loki connection.");

            //run the app
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "An unhandled top-level exception occurred while running the application.");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
