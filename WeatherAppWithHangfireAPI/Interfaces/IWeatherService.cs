using WeatherAppWithHangFire.Models;

namespace WeatherAppWithHangfireAPI.Interfaces
{
    public interface IWeatherService
    {
        Task<WeatherData> GetWeatherAsync();
        Task<WeatherForecast[]> GetForecastAsync();
    }
}