using EventPrime.Api.Models;

namespace EventPrime.Api.Services;

public interface IEventStore
{
    IEnumerable<Event> GetAll(string? category = null, string? location = null);
    Event? GetById(string id);
    Event Add(Event ev);
}

public class InMemoryEventStore : IEventStore
{
    private readonly List<Event> _events;

    public InMemoryEventStore()
    {
        // Seed with sample data so the API returns something useful out of the box
        _events = new List<Event>
        {
            new Event
            {
                Id = "1",
                Title = "Summer Music Festival",
                Description = "A three-day outdoor music festival featuring top artists from around the world.",
                Category = "Music",
                Location = "New York",
                Date = DateTimeOffset.UtcNow.AddDays(30),
                Price = 149.99m,
                Capacity = 5000,
                OrganizerName = "EventPrime Productions",
            },
            new Event
            {
                Id = "2",
                Title = "Tech Startup Summit",
                Description = "Connect with founders, investors, and innovators shaping the future of technology.",
                Category = "Business",
                Location = "San Francisco",
                Date = DateTimeOffset.UtcNow.AddDays(15),
                Price = 299.00m,
                Capacity = 800,
                OrganizerName = "StartupHub Inc.",
            },
            new Event
            {
                Id = "3",
                Title = "Modern Art Exhibition",
                Description = "Explore cutting-edge works from emerging and established artists across multiple mediums.",
                Category = "Arts",
                Location = "Chicago",
                Date = DateTimeOffset.UtcNow.AddDays(7),
                Price = 25.00m,
                Capacity = 300,
                OrganizerName = "City Arts Council",
            },
        };
    }

    public IEnumerable<Event> GetAll(string? category = null, string? location = null)
    {
        IEnumerable<Event> query = _events;

        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All Categories", StringComparison.OrdinalIgnoreCase))
            query = query.Where(e => e.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(location) && !location.Equals("All Locations", StringComparison.OrdinalIgnoreCase))
            query = query.Where(e => e.Location.Equals(location, StringComparison.OrdinalIgnoreCase));

        return query.OrderBy(e => e.Date).ToList();
    }

    public Event? GetById(string id) =>
        _events.FirstOrDefault(e => e.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public Event Add(Event ev)
    {
        _events.Add(ev);
        return ev;
    }
}
