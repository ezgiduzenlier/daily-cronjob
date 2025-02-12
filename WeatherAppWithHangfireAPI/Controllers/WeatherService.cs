using WeatherAppWithHangFire.Models;
using WeatherAppWithHangfireAPI.Interfaces;
using System.Text.Json;
using WeatherAppWithHangfireAPI;
using System.ComponentModel;
using Hangfire;
using Microsoft.Extensions.Logging;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherService> _logger;
    private const string API_KEY = "4576a64499cc4aa49d895435250602";
    private const string CITY = "Istanbul";

    public WeatherService(HttpClient httpClient, ILogger<WeatherService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("http://api.weatherapi.com/v1/");
    }


    [Queue("weather-current")]
    [DisplayName("İstanbul Hava Durumu Raporu - {0:dd.MM.yyyy HH:mm}")]
    public async Task GetWeatherAsync()
    {
        try
        {
            _logger.LogInformation("GetWeatherAsync başladı");
            var response = await MakeApiRequest("current.json");
            var result = await ParseWeatherData(response);
            _logger.LogInformation($"Hava durumu alındı: {result.Name}, Sıcaklık: {result.Main.Temp}°C, Nem: {result.Main.Humidity}%");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hava durumu verisi alınırken hata oluştu");
            throw new Exception($"Hava durumu verisi alınırken hata oluştu: {ex.Message}");
        }
    }

    [Queue("weather-feature")]
    [DisplayName("İstanbul 5 Günlük Hava Durumu Tahmini")]
    public async Task GetForecastAsync()
    {
        try
        {
            _logger.LogInformation("GetForecastAsync başladı");
            var response = await MakeApiRequest("forecast.json", additionalParams: "&days=5");
            var result = await ParseForecastData(response);
            _logger.LogInformation($"Tahmin alındı: {result.Length} gün için veri başarıyla çekildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tahmin verisi alınırken hata oluştu");
            throw new Exception($"Tahmin verisi alınırken hata oluştu: {ex.Message}");
        }
    }

    //(api isteği)
    private async Task<string> MakeApiRequest(string endpoint, string additionalParams = "")
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{endpoint}?key={API_KEY}&q={CITY}&lang=tr&aqi=no{additionalParams}");

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"API isteği sırasında hata oluştu: {ex.Message}");
        }
    }

    //(anşık hava durumu için)
    private async Task<WeatherData> ParseWeatherData(string content)
    {
        try
        {
            var weatherData = new WeatherData
            {
                Name = CITY,
                Main = new MainData(),
                Weather = new WeatherDescription[] { new WeatherDescription() }
            };

            var apiResponse = JsonSerializer.Deserialize<JsonElement>(content);

            if (apiResponse.TryGetProperty("current", out JsonElement current))
            {
                weatherData.Main.Temp = GetJsonValue(current, "temp_c", 0f);
                weatherData.Main.Humidity = GetJsonValue(current, "humidity", 0);

                if (current.TryGetProperty("condition", out JsonElement condition))
                {
                    weatherData.Weather[0].Desc = GetJsonValue(condition, "text", string.Empty);
                }
            }

            if (apiResponse.TryGetProperty("location", out JsonElement location))
            {
                weatherData.Name = GetJsonValue(location, "name", CITY);
            }

            return weatherData;
        }
        catch (Exception ex)
        {
            throw new Exception($"Hava durumu verisi işlenirken hata oluştu: {ex.Message}");
        }
    }

    //(5 günlük hava durumu için)
    private async Task<WeatherForecast[]> ParseForecastData(string content)
    {
        try
        {
            var apiResponse = JsonSerializer.Deserialize<JsonElement>(content);
            var forecasts = new List<WeatherForecast>();

            if (apiResponse.TryGetProperty("forecast", out JsonElement forecast) &&
                forecast.TryGetProperty("forecastday", out JsonElement forecastDays))
            {
                foreach (var day in forecastDays.EnumerateArray())
                {
                    if (day.TryGetProperty("date", out JsonElement date) &&
                        day.TryGetProperty("day", out JsonElement dayData))
                    {
                        forecasts.Add(new WeatherForecast
                        {
                            Date = DateOnly.Parse(GetJsonValue(day, "date", DateTime.Now.ToString("yyyy-MM-dd"))),
                            TemperatureC = (int)GetJsonValue(dayData, "avgtemp_c", 0.0),
                            Summary = GetJsonValue(dayData.GetProperty("condition"), "text", string.Empty)
                        });
                    }
                }
            }

            return forecasts.ToArray();
        }
        catch (Exception ex)
        {
            throw new Exception($"Tahmin verisi işlenirken hata oluştu: {ex.Message}");
        }
    }

    //(null ve tip kontrolü)
    private T GetJsonValue<T>(JsonElement element, string propertyName, T defaultValue)
    {
        if (element.TryGetProperty(propertyName, out JsonElement value))
        {
            try
            {
                return (T)Convert.ChangeType(value.GetRawText().Trim('"'), typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }
}