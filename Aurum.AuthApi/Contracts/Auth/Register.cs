namespace Aurum.AuthApi.Contracts.Auth;

public sealed class RegisterRequest
{
    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Cpf { get; init; } = string.Empty;

    public DateOnly BirthDate { get; init; }

    public string Phone { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string ConfirmPassword { get; init; } = string.Empty;
}
