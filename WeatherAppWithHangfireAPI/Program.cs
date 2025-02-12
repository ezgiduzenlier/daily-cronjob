using Hangfire;
using Hangfire.Common;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.DataProtection;
using WeatherAppWithHangfireAPI.Configuration;
using WeatherAppWithHangfireAPI.Interfaces;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Cookie şifrelemesi için
builder.Services.AddDataProtection()
    .SetApplicationName("WeatherApp");

// HTTP Client ve Weather Service'i ekle
builder.Services.AddHttpClient();
builder.Services.AddScoped<IWeatherService, WeatherService>();

// Hangfire'ı ekle
builder.Services.AddHangfire(config =>
{
    config.UseMemoryStorage();
});
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
});
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 1; // Worker sayısı
    options.Queues = new[] { "weather-current", "weather-feature" }; // Queue'ları belirt
    options.ServerName = "WeatherServer"; // Sunucu adı
});
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.Strict;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var hangfireSettings = builder.Configuration.GetSection("HangfireSettings").Get<HangfireSettings>();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new MyAuthorizationFilter() }
});

//app.UseHangfireDashboard("/hangfire", new DashboardOptions
//{
//    Authorization = new[]
//    {
//        new HangfireAuthenticationFilter(
//            hangfireSettings.Username,
//            hangfireSettings.Password,
//            app.Services.GetRequiredService<IDataProtectionProvider>())
//    },
//    DashboardTitle = "Hava Durumu Takip Sistemi"
//});

app.UseHttpsRedirection();
app.UseCookiePolicy();

// Job'ı başlat
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    // Current weather job
    recurringJobManager.AddOrUpdate<IWeatherService>(
        "Report-Istanbul-Weather",
        service => service.GetWeatherAsync(),
        Cron.Minutely(),
        new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Local
        });

    // Forecast weather job
    recurringJobManager.AddOrUpdate<IWeatherService>(
        "Forecast-Istanbul-Weather",
        service => service.GetForecastAsync(),
        Cron.MinuteInterval(5),
        new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Local
        });
}
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();