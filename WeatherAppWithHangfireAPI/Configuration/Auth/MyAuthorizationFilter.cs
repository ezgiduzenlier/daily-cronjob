using Hangfire.Dashboard;

public class MyAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // Geliştirme aşamasında tüm erişimlere izin verir
        return true;
    }
}