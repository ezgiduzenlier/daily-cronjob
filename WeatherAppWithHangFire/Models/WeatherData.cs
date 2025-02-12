using System;
namespace WeatherAppWithHangFire.Models
{
    public class WeatherData
    {
        public string Name { get; set; }
        public MainData Main { get; set; }
        public WeatherDescription[] Weather { get; set; }
    }
}

