using Aurum.AuthApi.Contracts.Auth;
using Aurum.AuthApi.Data;
using Aurum.AuthApi.Models;
using Aurum.AuthApi.Security;
using Microsoft.EntityFrameworkCore;

namespace Aurum.AuthApi.Services;

public sealed class AuthService
{
    private readonly AuthDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AuthDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<CheckCpfResponse> CheckCpfAsync(CheckCpfRequest req)
    {
        var pepper = _config["Cpf:Pepper"];
        if (string.IsNullOrWhiteSpace(pepper))
            throw new Exception("Cpf:Pepper não configurado");

        var cpfDigits = CpfUtils.Normalize(req.Cpf);
        if (!CpfUtils.IsValidLength(cpfDigits))
            return new CheckCpfResponse { Exists = false };

        var cpfHash = CpfUtils.HashWithPepper(cpfDigits, pepper);

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.CpfHash == cpfHash);

        if (user is null)
            return new CheckCpfResponse { Exists = false };

        var customer = await _db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == user.Id);

        return new CheckCpfResponse
        {
            Exists = true,
            CustomerStatus = customer?.Status
        };
    }

public async Task<LoginResponse> LoginAsync(LoginRequest req, JwtTokenService jwt)
{
    var pepper = _config["Cpf:Pepper"];
    if (string.IsNullOrWhiteSpace(pepper))
        throw new Exception("Cpf:Pepper não configurado");

    // ================================
    // 1) Validar entrada
    // ================================
    if (string.IsNullOrWhiteSpace(req.Cpf))
        throw new Exception("CPF é obrigatório");

    if (string.IsNullOrWhiteSpace(req.Password))
        throw new Exception("Senha é obrigatória");

    // ================================
    // 2) Normalizar CPF e gerar hash
    // ================================
    var cpfDigits = CpfUtils.Normalize(req.Cpf);

    if (!CpfUtils.IsValidLength(cpfDigits))
        throw new Exception("CPF ou senha inválidos");

    var cpfHash = CpfUtils.HashWithPepper(cpfDigits, pepper);

    // ================================
    // 3) Buscar usuário
    // ================================
    var user = await _db.Users
        .FirstOrDefaultAsync(u => u.CpfHash == cpfHash);

    if (user is null)
        throw new Exception("CPF ou senha inválidos");

    // ================================
    // 4) Validar senha
    // ================================
    var passwordOk = PasswordUtils.Verify(req.Password, user.PasswordHash);

    if (!passwordOk)
        throw new Exception("CPF ou senha inválidos");

    // ================================
    // 5) Buscar status do customer
    // ================================
    var customer = await _db.Customers
        .AsNoTracking()
        .FirstOrDefaultAsync(c => c.Id == user.Id);

    var status = customer?.Status ?? "PENDING";

    // ================================
    // 6) Gerar JWT
    // ================================
    var (token, expiresIn) =
        jwt.CreateToken(user.Id, user.CpfLast4, status);

    // ================================
    // 7) Retornar resposta
    // ================================
    return new LoginResponse
    {
        AccessToken = token,
        TokenType = "Bearer",
        ExpiresIn = expiresIn
    };
}

public async Task<RegisterResponse> RegisterAsync(RegisterRequest req)
    {
        var pepper = _config["Cpf:Pepper"];
        if (string.IsNullOrWhiteSpace(pepper))
            throw new Exception("Cpf:Pepper não configurado");

        if (string.IsNullOrWhiteSpace(req.FullName))
            throw new Exception("Nome é obrigatório");

        if (string.IsNullOrWhiteSpace(req.Cpf))
            throw new Exception("CPF é obrigatório");

        if (string.IsNullOrWhiteSpace(req.Phone))
            throw new Exception("Telefone é obrigatório");

        if (string.IsNullOrWhiteSpace(req.Password))
            throw new Exception("Senha é obrigatória");

        if (req.Password != req.ConfirmPassword)
            throw new Exception("Senhas não conferem");

        var cpfDigits = CpfUtils.Normalize(req.Cpf);
        if (!CpfUtils.IsValidLength(cpfDigits))
            throw new Exception("CPF inválido (precisa ter 11 dígitos)");

        var cpfHash = CpfUtils.HashWithPepper(cpfDigits, pepper);

        var alreadyExists = await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.CpfHash == cpfHash);

        if (alreadyExists)
            throw new Exception("CPF já cadastrado");

        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var user = new IdentityUser
        {
            Id = userId,
            Cpf = cpfDigits,
            CpfHash = cpfHash,
            CpfLast4 = CpfUtils.Last4(cpfDigits),
            PasswordHash = PasswordUtils.Hash(req.Password),
            Status = "ACTIVE",
            CreatedAt = now
        };

        var customer = new CoreCustomer
        {
            Id = userId,
            FullName = req.FullName.Trim(),
            Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim(),
            BirthDate = req.BirthDate,
            Phone = req.Phone.Trim(),
            Status = "PENDING",
            CreatedAt = now,
            UpdatedAt = now
        };

        await using var tx = await _db.Database.BeginTransactionAsync();

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        await tx.CommitAsync();

        return new RegisterResponse
        {
            CustomerId = userId,
            Status = customer.Status,
            Message = "Cadastro criado com sucesso. Sua conta está em análise."
        };
    }
}
