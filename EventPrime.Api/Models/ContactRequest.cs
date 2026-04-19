using System.ComponentModel.DataAnnotations;

namespace EventPrime.Api.Models;

public class ContactRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string Message { get; set; } = string.Empty;
}
