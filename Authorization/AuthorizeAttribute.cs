using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace BlobHandler.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class Allow(params string[] roles) : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles = roles;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var roles = context.HttpContext.Items["roles"] as IEnumerable<string> ?? [];
        if (!_roles.All(r => roles.Contains(r)))
        {
            Console.WriteLine(string.Join(',', roles));
            context.Result = new StatusCodeResult(403);
        }
    }
}