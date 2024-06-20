using Microsoft.IdentityModel.JsonWebTokens;

namespace BlobHandler.Authorization;

public interface IKeycloakJwtHandler
{
    Task<bool> IsValidJWT(JsonWebToken token);
    IEnumerable<string> GetRoles(JsonWebToken token);
}