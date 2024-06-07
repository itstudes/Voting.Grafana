namespace Voting.Grafana;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            //configure serilog
            Log.Logger = new LoggerConfiguration()
                           .MinimumLevel.Debug()
                           .WriteTo.OpenTelemetry()
                           .CreateLogger();

            //create the app builder
            var builder = WebApplication.CreateBuilder(args);

            //add diagnostic instrumentation for app (for open telemetry)
            builder.Services.AddSingleton<AppInstrumentation>();

            //add open telemetry
            builder.ConfigureOpenTelemetry();            

            //add voting services
            builder.Services.AddSingleton<RegisteredPartiesService>();
            builder.Services.AddSingleton<VotingRoundManagementService>();

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

            //configure the app to use the OpenTelemetry middleware
            app.MapPrometheusScrapingEndpoint();

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
