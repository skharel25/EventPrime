using Azure;
using Azure.Data.Tables;
using EventPrime.Api.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EventPrime.Api.Services;

public class UserStore : IUserStore
{
    private const string TableName = "users";
    private const string PartitionKey = "users";

    // Seeded on first startup if no admin user exists.
    private const string AdminEmail = "admin@eventprime.com";
    private const string AdminFirstName = "Admin";
    private const string AdminLastName = "EventPrime";
    private const string AdminInitialPassword = "EventPrime@2024!";
    private const string AdminRole = "admin";

    private readonly TableClient _tableClient;
    private readonly ILogger<UserStore> _logger;

    public UserStore(IConfiguration configuration, ILogger<UserStore> logger)
    {
        _logger = logger;
        var connectionString = configuration["AzureStorageConnectionString"]
            ?? throw new InvalidOperationException("AzureStorageConnectionString is not configured.");

        var serviceClient = new TableServiceClient(connectionString);
        _tableClient = serviceClient.GetTableClient(TableName);
    }

    public async Task<UserEntity?> GetByEmailAsync(string email)
    {
        var rowKey = email.ToLowerInvariant();
        try
        {
            var response = await _tableClient.GetEntityAsync<UserEntity>(PartitionKey, rowKey);
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task CreateAsync(UserEntity user)
    {
        user.PartitionKey = PartitionKey;
        user.RowKey = user.Email.ToLowerInvariant();
        await _tableClient.AddEntityAsync(user);
    }

    public async Task SeedAdminUserAsync()
    {
        try
        {
            await _tableClient.CreateIfNotExistsAsync();

            var existing = await GetByEmailAsync(AdminEmail);
            if (existing is not null)
            {
                _logger.LogInformation("Admin user already exists – skipping seed.");
                return;
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(AdminInitialPassword);
            var admin = new UserEntity
            {
                FirstName = AdminFirstName,
                LastName = AdminLastName,
                Email = AdminEmail,
                PasswordHash = passwordHash,
                Role = AdminRole,
            };

            await CreateAsync(admin);
            _logger.LogInformation("Seeded initial admin user {Email}.", AdminEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed admin user. Ensure AzureStorageConnectionString is set correctly.");
        }
    }
}
