using FastEndpoints;
using FastEndpoints.Swagger;
using Medika.Api;
using Medika.Api.Middleware;
using Medika.Infrastructure;
using Medika.Infrastructure.Auth;
using Medika.Infrastructure.Persistence;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

// Bootstrap logger — replaced by the full DI-aware configuration in UseSerilog below.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// Application Insights — reads APPLICATIONINSIGHTS_CONNECTION_STRING (Azure app setting)
// or "ApplicationInsights:ConnectionString". Empty connection string = telemetry disabled (safe locally).
// Auto-collects requests, dependencies (MongoDB/HTTP), exceptions and performance counters.
builder.Services.AddApplicationInsightsTelemetry();

builder.Host.UseSerilog((ctx, services, lc) => lc
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/medika-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
    .WriteTo.Sentry(
    o =>
    {
        o.InitializeSdk = false;                                // UseSentry() owns init + appsettings binding
        o.MinimumEventLevel = Serilog.Events.LogEventLevel.Error;       // Error+ become Sentry events
        o.MinimumBreadcrumbLevel = Serilog.Events.LogEventLevel.Information; // optional
    })
    // Serilog logs (Information+) also flow to App Insights as trace telemetry,
    // correlated with the originating request. No-op when AI is not configured.
    .WriteTo.ApplicationInsights(
        services.GetRequiredService<TelemetryConfiguration>(),
        TelemetryConverter.Traces));

// Sentry — binds the "Sentry" config section (Dsn, Environment, TracesSampleRate, ...).
// Empty Dsn (local dev default) makes the SDK a no-op, so this is safe everywhere.
// This is the single init point; UseSentry owns config binding + the OTel-style logs feature.
builder.WebHost.UseSentry(o =>
{
    o.EnableLogs = true; // forward structured logs to Sentry's Logs product
});

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var jwtSettings = builder.Configuration.GetSection(JwtSettings.Section).Get<JwtSettings>()!;
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        };
    });

builder.Services.AddAuthorization();

builder.Services
    .AddFastEndpoints()
    .SwaggerDocument(o =>
    {
        o.DocumentSettings = s =>
        {
            s.Title = "Medika API";
            s.Version = "v1";
        };
        o.EnableJWTBearerAuth = true;
    });

builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p => p
        .WithOrigins(
            builder.Configuration["Cors:AllowedOrigins"]?.Split(',') ?? ["http://localhost:5173"])
        .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<MongoContext>();
    await MongoDbInitializer.InitializeAsync(ctx);
    await MongoDbInitializer.SeedAsync(ctx);
}

app.UseExceptionHandler();
app.UseCors();
app.UseMiddleware<HttpDetailLoggingMiddleware>(); // full HTTP detail → App Insights + Serilog (captures 401s too)
app.UseMiddleware<ApiKeyMiddleware>(); // X-API-KEY + anti-replay timestamp — BFF-only access (eGestion ADR-008)
app.UseAuthentication();
app.UseAuthorization();

app.UseFastEndpoints(c =>
{
    c.Errors.UseProblemDetails();
    c.Serializer.Options.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    // Endpoints gate access with Permissions(...) — the JWT carries one "permissions" claim
    // per granted permission (Doctor → all; staff → their customisable set). Issue #24.
    c.Security.PermissionsClaimType = "permissions";
}).UseSwaggerGen();

app.Run();
