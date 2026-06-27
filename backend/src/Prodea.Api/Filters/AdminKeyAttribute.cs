using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Prodea.Api.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AdminKeyAttribute : TypeFilterAttribute
{
    public AdminKeyAttribute() : base(typeof(AdminKeyFilter)) { }
}

[AttributeUsage(AttributeTargets.Method)]
public class SkipAdminKeyAttribute : Attribute { }

public class AdminKeyFilter(IWebHostEnvironment env) : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ActionDescriptor.EndpointMetadata.Any(m => m is SkipAdminKeyAttribute))
            return;

        if (env.IsDevelopment()) return;

        var adminKey = context.HttpContext.Request.Headers["X-Admin-Key"].FirstOrDefault()
            ?? context.HttpContext.Request.Query["adminKey"].FirstOrDefault();

        var expectedKey = Environment.GetEnvironmentVariable("ADMIN_KEY");
        if (expectedKey == null || adminKey != expectedKey)
            context.Result = new ForbidResult();
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
