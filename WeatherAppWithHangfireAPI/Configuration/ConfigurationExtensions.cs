
namespace WeatherAppWithHangfireAPI.Configuration
    {
        public static class ConfigurationExtensions
        {
            public static HangfireSettings GetHangfireSettings(this IConfiguration configuration)
            {
                var settings = configuration.GetSection("HangfireSettings").Get<HangfireSettings>();

                if (string.IsNullOrEmpty(settings?.Username) || string.IsNullOrEmpty(settings?.Password))
                {
                    throw new InvalidOperationException("Hangfire kimlik bilgileri yapılandırılmamış!");
                }
                return settings;
            }
        }
    }

