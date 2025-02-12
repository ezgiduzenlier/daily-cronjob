using System.Net.Http.Json;
using System.Text.Json;
using WeatherAppWithHangFire.Models;

public class WeatherService
{
    private readonly HttpClient _httpClient;
    private const string API_KEY = "4576a64499cc4aa49d895435250602";
    private const string CITY = "Istanbul";

    public WeatherService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("http://api.weatherapi.com/v1/");
    }

    public async Task GetWeatherAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"current.json?key={API_KEY}&q={CITY}&lang=tr&aqi=no");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Hata Detayı: {errorContent}");
                Console.WriteLine($"Status Code: {response.StatusCode}");
                return;
            }

            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"API Yanıtı: {content}"); // Gelen veriyi görmek için

            var weatherData = new WeatherData
            {
                Name = CITY,
                Main = new MainData(),
                Weather = new WeatherDescription[] { new WeatherDescription() }
            };

            // API'den gelen veriyi kendi modelimize dönüştürüyoruz
            var apiResponse = JsonSerializer.Deserialize<JsonElement>(content);

            if (apiResponse.TryGetProperty("current", out JsonElement current))
            {
                if (current.TryGetProperty("temp_c", out JsonElement temp))
                {
                    weatherData.Main.Temp = temp.GetSingle();
                }

                if (current.TryGetProperty("humidity", out JsonElement humidity))
                {
                    weatherData.Main.Humidity = humidity.GetInt32();
                }

                if (current.TryGetProperty("condition", out JsonElement condition) &&
                    condition.TryGetProperty("text", out JsonElement text))
                {
                    weatherData.Weather[0].Desc = text.GetString();
                }
            }

            if (apiResponse.TryGetProperty("location", out JsonElement location) &&
                location.TryGetProperty("name", out JsonElement name))
            {
                weatherData.Name = name.GetString();
            }

            Console.Clear();
            Console.WriteLine($"Son Güncelleme: {DateTime.Now:HH:mm:ss}");
            Console.WriteLine($"Şehir: {weatherData.Name}");
            Console.WriteLine($"Sıcaklık: {weatherData.Main.Temp:F1}°C");
            Console.WriteLine($"Nem: %{weatherData.Main.Humidity}");
            Console.WriteLine($"Durum: {weatherData.Weather[0].Desc}");
            Console.WriteLine("----------------------------------------");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Hava durumu bilgisi alınırken hata oluştu: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }
    }
}