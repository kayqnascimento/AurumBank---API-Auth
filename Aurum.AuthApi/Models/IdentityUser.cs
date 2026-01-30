using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aurum.AuthApi.Models;

[Table("users", Schema = "identity")]
public class IdentityUser
{
    [Key]
    public Guid Id { get; set; }

    // CPF aberto (por enquanto)
    [Required]
    [MaxLength(11)]
    public string Cpf { get; set; } = string.Empty;

    // Hash para lookup futuro (opcional, mas bom)
    [Required]
    public string CpfHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(4)]
    public string CpfLast4 { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = "ACTIVE";

    public DateTime CreatedAt { get; set; }
}
