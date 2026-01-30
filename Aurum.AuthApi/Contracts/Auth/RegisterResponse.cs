namespace Aurum.AuthApi.Contracts.Auth;

public sealed class RegisterResponse
{
    public Guid CustomerId { get; init; }

    public string Status { get; init; } = "PENDING";

    public string Message { get; init; } = string.Empty;
}
