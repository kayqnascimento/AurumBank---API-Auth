using Aurum.AuthApi.Data;
using Aurum.AuthApi.Endpoints;
using Aurum.AuthApi.Security;
using Aurum.AuthApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
using static System.Net.Mime.MediaTypeNames;

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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () =>
{
    var asm = Assembly.GetExecutingAssembly();

    var serviceName =
        asm.GetName().Name
        ?? Environment.GetEnvironmentVariable("SERVICE_NAME")
        ?? "aurum-auth";

    var informational =
        asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

    var version =
        !string.IsNullOrWhiteSpace(informational)
            ? informational
            : asm.GetName().Version?.ToString() ?? "0.0.0";

    var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";

    var html = """
<!doctype html>
<html lang="pt-br">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>_SERVICE_</title>
  <style>
    body { font-family: system-ui, -apple-system, Segoe UI, Roboto, Arial, sans-serif; margin:0; background:#0b0f19; color:#e6e8ee; }
    .wrap { min-height:100vh; display:flex; align-items:center; justify-content:center; padding:24px; }
    .card { width:min(780px, 100%); background:#121a2a; border:1px solid #24304a; border-radius:16px; padding:28px; box-shadow: 0 10px 30px rgba(0,0,0,.35); }
    .badge { display:inline-flex; align-items:center; gap:10px; padding:6px 10px; border-radius:999px; background:#0f2f1d; border:1px solid #1f6a3b; color:#7dffb2; font-weight:700; font-size:12px; letter-spacing:.2px; }
    .dot { width:10px; height:10px; border-radius:50%; background:#22c55e; box-shadow:0 0 0 4px rgba(34,197,94,.15); display:inline-block; }
    h1 { margin:14px 0 8px; font-size:28px; }
    p { margin:0 0 18px; color:#b6bdd0; line-height:1.5; }
    .grid { display:grid; grid-template-columns: 1fr 1fr; gap:12px; margin-top:18px; }
    .tile { padding:14px; border-radius:12px; background:#0e1524; border:1px solid #22304b; }
    .k { font-size:12px; color:#93a0ba; margin-bottom:6px; }
    .v { font-weight:800; }
    .btns { display:flex; gap:12px; margin-top:22px; flex-wrap:wrap; }
    a.btn { text-decoration:none; display:inline-flex; align-items:center; justify-content:center; padding:12px 16px; border-radius:12px; font-weight:800; }
    a.primary { background:#3b82f6; color:white; }
    a.secondary { background:#0e1524; color:#e6e8ee; border:1px solid #22304b; }
    code { background:#0e1524; border:1px solid #22304b; padding:2px 6px; border-radius:8px; color:#d7def0; }
    @media (max-width: 540px){ .grid { grid-template-columns:1fr; } }
  </style>
</head>
<body>
  <div class="wrap">
    <div class="card">
      <span class="badge"><span class="dot"></span> ONLINE</span>
      <h1>WebApi: AurumBank Api Auth</h1>
      <p>API est&aacute funcionando. Ambiente: <code>_ENV_</code></p>

      <div class="grid">
        <div class="tile">
          <div class="k">Servi&ccedil;o</div>
          <div class="v">_SERVICE_</div>
        </div>
        <div class="tile">
          <div class="k">Vers&atilde;o</div>
          <div class="v">_VERSION_</div>
        </div>
        <div class="tile">
          <div class="k">Sa&uacute;de</div>
          <div class="v"><code>/health</code></div>
        </div>
        <div class="tile">
          <div class="k">Banco</div>
          <div class="v"><code>/health/db</code></div>
        </div>
      </div>

      <div class="btns">
        <a class="btn primary" href="/swagger">Abrir Swagger</a>
        <a class="btn secondary" href="/health">Ver Health</a>
        <a class="btn secondary" href="/health/db">Ver Health DB</a>
      </div>
    </div>
  </div>
</body>
</html>
""";

    html = html
        .Replace("_SERVICE_", serviceName)
        .Replace("_VERSION_", version)
        .Replace("_ENV_", environment);

    return Results.Content(html, "text/html", System.Text.Encoding.UTF8);
});

app.Run();
