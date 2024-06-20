using Microsoft.IdentityModel.JsonWebTokens;
using System.Net;

namespace BlobHandler.Authorization;

public class AuthMiddleware(RequestDelegate next, IKeycloakJwtHandler keycloakJwtHandler)
{
    public async Task Invoke(HttpContext context)
    {
        JsonWebToken? jwt = null;

        // Either get the token from the Authorization header
        context.Request.Headers.TryGetValue("Authorization", out var authorization);

        // Or from the query parameter
        // This is helpful in case the headers cannot be modified, e.g., HTML <audio> tag
        var token = context.Request.Query["token"];

        try
        {
            if (authorization.Count == 1)
            {
                jwt = new JsonWebToken(authorization[0]![7..]);
            }
            else if (token.Count == 1)
            {
                jwt = new JsonWebToken(token[0]);
            }
        }
        catch
        {
            context.Response.Clear();
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsync("Malformed token.");
            return;
        }

        if(jwt != null)
        {
            if (await keycloakJwtHandler.IsValidJWT(jwt))
            {
                context.Items["roles"] = keycloakJwtHandler.GetRoles(jwt);
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

