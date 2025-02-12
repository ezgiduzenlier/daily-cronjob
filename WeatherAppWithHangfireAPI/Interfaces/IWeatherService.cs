using Hangfire;
using WeatherAppWithHangFire.Models;

namespace WeatherAppWithHangfireAPI.Interfaces
{
    public interface IWeatherService
    {
        [Queue("weather-current")]
        Task GetWeatherAsync();

        [Queue("weather-feature")]
        Task GetForecastAsync();
    }
}