namespace Aurum.AuthApi.Contracts.Auth;

public sealed class LoginRequest
{
    public string Cpf { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
