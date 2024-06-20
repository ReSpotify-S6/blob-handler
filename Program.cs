using BlobHandler;
using BlobHandler.Authorization;
using BlobHandler.Messaging;

var builder = WebApplication.CreateBuilder(args);

var requiredVariables = new List<string>
{
    "AZURE_STORAGE_ACCOUNT_NAME",
    "AZURE_STORAGE_ACCOUNT_KEY",
    "AZURE_STORAGE_CONTAINER_NAME",

    "RABBITMQ_HOSTNAME",
    "RABBITMQ_USERNAME",
    "RABBITMQ_PASSWORD",

    "ALLOWED_ORIGINS",
    "REDIRECT_URI",     // For redirects to resources
    "KC_JWKS_URL",      // To validate tokens from keycloak
};

var envManager = new EnvironmentVariableManager(requiredVariables);
builder.Services.AddSingleton(envManager);

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Set an upper bound for uploading blobs
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 25 * 1024000; // 25MB
});


// CORS
string[] allowedOrigins = envManager["ALLOWED_ORIGINS"].Split(',');
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

builder.Services.AddControllers();

builder.Services.AddSingleton<IAzureBlobService, AzureBlobService>();
builder.Services.AddSingleton<IEventPublisher, EventPublisher>();
builder.Services.AddSingleton<IKeycloakJwtHandler, KeycloakJwtHandler>();

var app = builder.Build();

app.UseCors(corsPolicy);

app.MapControllers();

app.UseAuthorization();

app.UseMiddleware<AuthMiddleware>();

app.Run();
