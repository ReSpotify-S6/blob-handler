using BlobHandler;
using BlobHandler.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// max body request size
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 25 * 1024000; // 25MB
});

// CORS
string[] allowedOrigins = GetEnvVar("ALLOWED_ORIGINS").Split(',');
string corsPolicy = "frontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        name: corsPolicy,
        policy => {
            policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
        });
});


static string GetEnvVar(string name)
{
    return Environment.GetEnvironmentVariable(name)
        ?? throw new Exception($"Environment variable {name} is not set.");
}
string accountName   = GetEnvVar("AZURE_STORAGE_ACCOUNT_NAME");
string accountKey    = GetEnvVar("AZURE_STORAGE_ACCOUNT_KEY");
string containerName = GetEnvVar("AZURE_STORAGE_CONTAINER_NAME");
string _             = GetEnvVar("REDIRECT_URI"); // For redirects to resources
string __            = GetEnvVar("KC_JWKS_URL"); // To validate tokens from keycloak

var connectionString = $"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={accountKey};EndpointSuffix=core.windows.net";

// Services
builder.Services.AddSingleton<IAzureBlobService>(new AzureBlobService(connectionString, containerName));
builder.Services.AddSingleton<IKeycloakJwtHandler, KeycloakJwtHandler>();

var app = builder.Build();

app.UseCors(corsPolicy);

app.MapControllers();

app.UseAuthorization();

app.UseMiddleware<AuthMiddleware>();

app.Run();
