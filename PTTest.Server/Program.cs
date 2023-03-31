namespace PTTest.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSignalR().AddMessagePackProtocol();
        builder.Services.AddSingleton<IPositionService, PositionService>();

        // Use DI container to retrieve IPositionService registered above, so we don't register the same instance twice.
        builder.Services.AddHostedService(
            services => (PositionService)services.GetRequiredService<IPositionService>()
        );
        builder.Services.AddHostedService<TickService>();

        var app = builder.Build();

        app.MapHub<PositionHub>("/position");

        app.Run();
    }
}
