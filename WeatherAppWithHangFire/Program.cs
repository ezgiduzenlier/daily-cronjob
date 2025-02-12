using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args);

        builder.ConfigureServices((context, services) =>
        {
            services.AddHttpClient();
            services.AddScoped<WeatherService>();

            services.AddHangfire(config => config.UseMemoryStorage());
            services.AddHangfireServer();
        });

        var host = builder.Build();

        using (var scope = host.Services.CreateScope())
        {
            var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

            recurringJobManager.AddOrUpdate(
                "GetWeatherJob",
            () => scope.ServiceProvider
            .GetRequiredService<WeatherService>()
                    .GetWeatherAsync(),
                Cron.Minutely());
        }

        await host.RunAsync();
    }
}