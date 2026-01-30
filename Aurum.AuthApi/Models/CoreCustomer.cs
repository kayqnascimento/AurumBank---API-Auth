using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aurum.AuthApi.Models;

[Table("customers", Schema = "core")]
public class CoreCustomer
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    [Required]
    public DateOnly BirthDate { get; set; }

    [Required]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = "PENDING";

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
