using Hangfire.Dashboard;
using Microsoft.AspNetCore.DataProtection;

public class HangfireAuthenticationFilter : IDashboardAuthorizationFilter
{
    private readonly string _username;
    private readonly string _password;
    private readonly IDataProtector _protector;
    private const string AuthCookieName = "HangfireAuth";

    public HangfireAuthenticationFilter(
        string username,
        string password,
        IDataProtectionProvider dataProtectionProvider)
    {
        _username = username;
        _password = password;
        _protector = dataProtectionProvider.CreateProtector("HangfireAuth");
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Cookie kontrolü
        if (httpContext.Request.Cookies.TryGetValue(AuthCookieName, out string cookieValue))
        {
            try
            {
                var decryptedValue = _protector.Unprotect(cookieValue);
                if (decryptedValue == "Authenticated")
                {
                    return true;
                }
            }
            catch
            {
                // Şifre çözme başarısız olduysa cookie'yi sil
                httpContext.Response.Cookies.Delete(AuthCookieName);
            }
        }

        // Basic auth kontrolü
        string header = httpContext.Request.Headers["Authorization"].ToString();

        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Basic "))
        {
            httpContext.Response.Headers.Add("WWW-Authenticate", "Basic");
            return false;
        }

        var authValues = System.Text.Encoding.UTF8.GetString(
            Convert.FromBase64String(header.Substring("Basic ".Length))
        ).Split(':');

        if (authValues[0] == _username && authValues[1] == _password)
        {
            // Başarılı girişte şifrelenmiş cookie oluştur
            var encryptedValue = _protector.Protect("Authenticated");

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.Now.AddHours(12)
            };

            httpContext.Response.Cookies.Append(AuthCookieName, encryptedValue, cookieOptions);
            return true;
        }

        return false;
    }
}