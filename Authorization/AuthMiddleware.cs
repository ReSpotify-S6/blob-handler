using Microsoft.IdentityModel.JsonWebTokens;
using System.Net;

namespace BlobHandler.Authorization;

public class AuthMiddleware(RequestDelegate next, IKeycloakJwtHandler keycloakJwtHandler)
{
    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("Authorization", out var authorization))
        {
            JsonWebToken token;
            try
            {
                token = new JsonWebToken(authorization[0]![7..]);
            }
            catch
            {
                context.Response.Clear();
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsync("Malformed token.");
                return;
            }

            if (await keycloakJwtHandler.IsValidJWT(token))
            {
                context.Items["roles"] = keycloakJwtHandler.GetRoles(token);
            }
            else
            {
                context.Response.Clear();
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await context.Response.WriteAsync("Invalid token.");
                return;
            }
        }

        await next(context);
    }
}

