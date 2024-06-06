namespace Voting.Grafana;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

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
        if ( app.Environment.IsDevelopment() )
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        //configure the app to use HTTPS redirection and authorization
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        //run the app
        app.Run();
    }
}
