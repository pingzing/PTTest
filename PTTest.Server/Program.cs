using Serilog;

namespace PTTest.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Set up logging
        builder.Host.UseSerilog(
            (context, config) => config.ReadFrom.Configuration(context.Configuration)
        );

        // Allow ALL THE THINGS
        // Hilariously insecure, but it's throwaway code.
        builder.Services.AddCors(
            options =>
                options.AddPolicy(
                    "AllowAllPolicy",
                    builder =>
                    {
                        builder
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .SetIsOriginAllowed((host) => true)
                            .AllowCredentials();
                    }
                )
        );

        builder.Services.AddSignalR().AddMessagePackProtocol();
        builder.Services.AddSingleton<IPositionService, PositionService>();
        builder.Services.AddSingleton<TestModeService>();

        // Use DI container to retrieve IPositionService registered above, so we don't register the same instance twice.
        builder.Services.AddHostedService(
            services => (PositionService)services.GetRequiredService<IPositionService>()
        );
        builder.Services.AddHostedService<TickService>();

        var app = builder.Build();

        app.UseSerilogRequestLogging();
        app.UseCors("AllowAllPolicy");
        app.MapHub<PositionApiHub>("/position");

        // Register endpoints to control test mode.
        app.MapGet(
            "/testmode",
            (TestModeService testService) =>
            {
                testService.EngageTestMode(1000);
            }
        );
        app.MapGet(
            "/endtestmode",
            (TestModeService testService) =>
            {
                testService.EndTestMode();
            }
        );

        app.Run();
    }
}
