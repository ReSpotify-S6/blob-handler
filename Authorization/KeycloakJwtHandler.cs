using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BlobHandler.Authorization;

public class KeycloakJwtHandler(ILogger<KeycloakJwtHandler> logger, EnvironmentVariableManager envManager) : IKeycloakJwtHandler
{
    private readonly TokenValidationParameters _tokenValidationParameters = new()
    {
        ValidateIssuerSigningKey = true,
        ValidateIssuer = false,
        ValidateAudience = false,
    };

    private readonly JsonWebTokenHandler tokenHandler = new();

    public async Task<bool> IsValidJWT(JsonWebToken token)
    {
        _tokenValidationParameters.IssuerSigningKeys ??= await FetchKeysAsync();

        var result = await tokenHandler.ValidateTokenAsync(token, _tokenValidationParameters);
        
        if (!result.IsValid)
        {
            logger.LogWarning("Token invalidated: {}", result.Exception);
        }

        return result.IsValid;
    }

    public IEnumerable<string> GetRoles(JsonWebToken token)
    {
        var realmAccessJson = token.GetPayloadValue<string>("realm_access");
        var realmAccess = JsonSerializer.Deserialize<RealmAccess>(realmAccessJson)
            ?? throw new JsonException($"Could not deserialize json: {realmAccessJson}");

        return realmAccess.roles;
    }

    private async Task<IEnumerable<SecurityKey>> FetchKeysAsync()
    {
        var handler = new HttpClientHandler();
        var httpClient = new HttpClient(handler);
        var jwksUrl = envManager["KC_JWKS_URL"];

        var response = await httpClient.GetAsync(jwksUrl);
        response.EnsureSuccessStatusCode();
        var jwksJson = await response.Content.ReadAsStringAsync();

        // Parse JWKS
        var jwks = JsonSerializer.Deserialize<Jwks>(jwksJson)
            ?? throw new JsonException($"Could not deserialize json: {jwksJson}");

        return jwks.keys.Select(k => new X509SecurityKey(new X509Certificate2(Convert.FromBase64String(k.x5c.First()))));
    }

    private class RealmAccess
    {
        public List<string> roles { get; set; } = [];
    }


    private class Jwks
    {
        public IEnumerable<Jwk> keys { get; set; } = [];

        public class Jwk
        {
            public List<string> x5c { get; set; } = [];
        }
    }
}