using Aurum.AuthApi.Contracts.Auth;
using Aurum.AuthApi.Security;
using Aurum.AuthApi.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Aurum.AuthApi.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var rlEnabled = app.Configuration.GetValue<bool>("RateLimiting:Enabled");

        var checkCpf = app.MapPost("/auth/check-cpf", CheckCpf).AllowAnonymous();
        if (rlEnabled) checkCpf.RequireRateLimiting("auth-check-cpf");

        var register = app.MapPost("/auth/register", Register).AllowAnonymous();
        if (rlEnabled) register.RequireRateLimiting("auth-register");

        var login = app.MapPost("/auth/login", Login).AllowAnonymous();
        if (rlEnabled) login.RequireRateLimiting("auth-login");

        app.MapGet("/auth/me", Me).RequireAuthorization();
    }

    private static IResult Me(ClaimsPrincipal user)
    {
        var customerId = user.GetCustomerIdOrNull();

        return Results.Ok(new
        {
            customerId,
            cpfLast4 = user.FindFirstValue("cpf_last4"),
            status = user.FindFirstValue("customer_status")
        });
    }

    private static async Task<IResult> CheckCpf(CheckCpfRequest req, AuthService service)
    {
        try
        {
            var result = await service.CheckCpfAsync(req);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> Register(RegisterRequest req, AuthService service)
    {
        try
        {
            var result = await service.RegisterAsync(req);
            return Results.Created($"/customers/{result.CustomerId}", result);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> Login(LoginRequest req, AuthService service, JwtTokenService jwt)
    {
        try
        {
            var result = await service.LoginAsync(req, jwt);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    // ================================
    // Helpers
    // ================================
    private static string? GetCustomerIdOrNull(this ClaimsPrincipal user)
    {
        // 1) padrão JWT: sub
        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? user.FindFirstValue("sub");
        if (!string.IsNullOrWhiteSpace(sub))
            return sub;

        // 2) mapeamentos comuns
        var nameId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(nameId))
            return nameId;

        return null;
    }
}
