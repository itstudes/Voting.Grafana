using NLog;
using NLog.Web;

namespace Voting.VoterInfo.Api;

public class Program
{
    public static void Main(string[] args)
    {
        //init NLog
        var logger = LogManager.Setup()
                               .LoadConfigurationFromAppSettings()
                               .GetCurrentClassLogger();
        try
        {
            //create the builder
            var builder = WebApplication.CreateBuilder(args);

            //configure services
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            //setup NLog for DI
            builder.Logging.ClearProviders();
            builder.Host.UseNLog();

            //build
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();
            app.MapControllers();

            //run
            app.Run();
        }
        catch (Exception ex)
        {
            logger.Error(ex, 
                         "An error occurred while starting the application.");
            throw;
        }
        finally
        {
            LogManager.Shutdown();
        }
    }
}
