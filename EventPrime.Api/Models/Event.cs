namespace EventPrime.Api.Models;

public class Event
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; }
    public decimal Price { get; set; }
    public int Capacity { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string OrganizerName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
