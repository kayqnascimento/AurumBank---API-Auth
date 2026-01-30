namespace Aurum.AuthApi.Security;

public sealed class TokenResult
{
    public string AccessToken { get; init; } = string.Empty;
    public int ExpiresInSeconds { get; init; }
}
