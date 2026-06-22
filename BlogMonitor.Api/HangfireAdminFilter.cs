using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

public class HangfireAdminFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext == null) return false;
        var user = httpContext.User;
        return user?.Identity?.IsAuthenticated == true && user.IsInRole("Admin");
    }
}
