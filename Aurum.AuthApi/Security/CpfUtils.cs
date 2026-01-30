using System.Security.Cryptography;
using System.Text;

namespace Aurum.AuthApi.Security;

public static class CpfUtils
{
    public static string Normalize(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return string.Empty;

        var sb = new StringBuilder();

        foreach (var ch in cpf)
        {
            if (char.IsDigit(ch))
                sb.Append(ch);
        }

        return sb.ToString();
    }

    // ✅ Este é o método que seu AuthService está chamando
    public static bool IsValidLength(string cpfDigits)
        => cpfDigits.Length == 11;

    public static string Last4(string cpfDigits)
        => cpfDigits.Length >= 4 ? cpfDigits[^4..] : cpfDigits;

    public static string HashWithPepper(string cpfDigits, string pepper)
    {
        var raw = $"{pepper}{cpfDigits}";
        var bytes = Encoding.UTF8.GetBytes(raw);

        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
