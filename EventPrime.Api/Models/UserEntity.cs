using Azure;
using Azure.Data.Tables;

namespace EventPrime.Api.Models;

public class UserEntity : ITableEntity
{
    /// <summary>Partition key – always "users" for this table.</summary>
    public string PartitionKey { get; set; } = "users";

    /// <summary>Row key – the user's email address (lower-cased).</summary>
    public string RowKey { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "admin";

    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
