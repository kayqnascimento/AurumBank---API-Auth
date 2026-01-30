namespace Aurum.AuthApi.Contracts.Auth;

public sealed class CheckCpfResponse
{
    public bool Exists { get; init; }

    // Status do cliente se já existir
    public string? CustomerStatus { get; init; }
}
