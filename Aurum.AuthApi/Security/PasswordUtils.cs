using Microsoft.AspNetCore.Identity;

namespace Aurum.AuthApi.Security;

public static class PasswordUtils
{
    private static readonly PasswordHasher<string> _hasher = new();

    /// <summary>
    /// Gera hash seguro da senha.
    /// </summary>
    public static string Hash(string password)
    {
        return _hasher.HashPassword("user", password);
    }

    /// <summary>
    /// Verifica se a senha digitada corresponde ao hash armazenado.
    /// </summary>
    public static bool Verify(string password, string passwordHash)
    {
        var result = _hasher.VerifyHashedPassword("user", passwordHash, password);

        return result == PasswordVerificationResult.Success;
    }
}
