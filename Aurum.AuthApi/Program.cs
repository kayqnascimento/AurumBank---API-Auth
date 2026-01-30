using Aurum.AuthApi.Data;
using Aurum.AuthApi.Endpoints;
using Aurum.AuthApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using System.Text;
using Aurum.AuthApi.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<JwtTokenService>();

// ================================
// Database
// ================================
builder.Services.AddDbContext<AuthDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default")
    );
});

// ================================
// Services
// ================================
builder.Services.AddScoped<AuthService>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "RATE_LIMITED",
            message = "Muitas requisições. Tente novamente em instantes."
        }, cancellationToken: token);
    };

    // Helper: chave por IP (com fallback)
    static string GetClientKey(HttpContext ctx)
        => ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    // /auth/check-cpf -> 10 req / minuto
    options.AddPolicy("auth-check-cpf", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetClientKey(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // /auth/register -> 3 req / 5 minutos
    options.AddPolicy("auth-register", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetClientKey(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(5),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // /auth/login -> 5 req / minuto
    options.AddPolicy("auth-login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetClientKey(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)
            ),

            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();


// ================================
// App
// ================================
var app = builder.Build();

var rateLimitingEnabled = app.Configuration.GetValue<bool>("RateLimiting:Enabled");

if (rateLimitingEnabled)
{
    app.UseRateLimiter();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "aurum-auth" }))
   .AllowAnonymous();

app.MapGet("/health/db", async (AuthDbContext db) =>
{
    await db.Database.ExecuteSqlRawAsync("SELECT 1;");
    return Results.Ok(new { status = "ok", database = "connected" });
})
.AllowAnonymous();

app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapAuthEndpoints();

app.Run();
