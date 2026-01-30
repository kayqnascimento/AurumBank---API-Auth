namespace Aurum.AuthApi.Contracts.Auth;

public sealed class CheckCpfRequest
{
    public string Cpf { get; init; } = string.Empty;
}
