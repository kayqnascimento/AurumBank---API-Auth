using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Aurum.AuthApi.Security;

public sealed class JwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config)
    {
        _config = config;
    }

    public (string Token, int ExpiresInSeconds) CreateToken(
        Guid customerId,
        string cpfLast4,
        string customerStatus)
    {
        // ================================
        // Config values
        // ================================
        var issuer = _config["Jwt:Issuer"];
        var audience = _config["Jwt:Audience"];
        var secret = _config["Jwt:Secret"];
        var expiresMinutes = int.Parse(_config["Jwt:ExpiresMinutes"] ?? "240");

        if (string.IsNullOrWhiteSpace(secret))
            throw new Exception("Jwt:Secret não configurado");

        if (secret.Length < 32)
            throw new Exception("Jwt:Secret precisa ter pelo menos 32 caracteres");

        if (string.IsNullOrWhiteSpace(issuer))
            throw new Exception("Jwt:Issuer não configurado");

        if (string.IsNullOrWhiteSpace(audience))
            throw new Exception("Jwt:Audience não configurado");

        // ================================
        // Signing key
        // ================================
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secret)
        );

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        // ================================
        // Token dates
        // ================================
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(expiresMinutes);

        // ================================
        // Claims (payload)
        // ================================
        var claims = new List<Claim>
        {
            // Main identity
            new(JwtRegisteredClaimNames.Sub, customerId.ToString()),

            // UX-safe info
            new("cpf_last4", cpfLast4),

            // Business status
            new("customer_status", customerStatus),

            // Standard JWT metadata
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // ================================
        // Create token
        // ================================
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials
        );

        // Serialize
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return (jwt, (int)(expires - now).TotalSeconds);
    }
}
