using System.ComponentModel.DataAnnotations;

namespace EventPrime.Api.Models;

public class CreateEventRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = string.Empty;

    [Required]
    public string Location { get; set; } = string.Empty;

    [Required]
    public DateTimeOffset Date { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(1, int.MaxValue)]
    public int Capacity { get; set; } = 100;

    public string ImageUrl { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string OrganizerName { get; set; } = string.Empty;
}
