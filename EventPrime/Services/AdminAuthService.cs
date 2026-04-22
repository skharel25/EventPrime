using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventPrime.Services;

/// <summary>
/// Authenticates admin users by calling the EventPrime API login endpoint.
/// The received JWT is stored in memory for the lifetime of the Blazor circuit.
/// </summary>
public class AdminAuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private string? _token;
    private DateTimeOffset _tokenExpiry;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public AdminAuthService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public bool IsAuthenticated =>
        !string.IsNullOrEmpty(_token) && DateTimeOffset.UtcNow < _tokenExpiry;

    public string? Token => _token;

    /// <summary>
    /// Calls POST /api/auth/login and stores the returned JWT on success.
    /// Returns true on success, false on invalid credentials or network errors.
    /// </summary>
    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("EventPrimeApi");
            var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });

            if (!response.IsSuccessStatusCode)
                return false;

            var json = await response.Content.ReadFromJsonAsync<ApiLoginResponse>(JsonOptions);
            if (json?.Data?.Token is null)
                return false;

            _token = json.Data.Token;
            // JWT tokens issued by the API expire in 8 hours; expire locally slightly earlier to be safe.
            _tokenExpiry = DateTimeOffset.UtcNow.AddHours(7).AddMinutes(55);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Logout()
    {
        _token = null;
        _tokenExpiry = DateTimeOffset.MinValue;
    }

    // Minimal DTOs for deserialising the API response.
    private sealed class ApiLoginResponse
    {
        [JsonPropertyName("data")]
        public LoginData? Data { get; set; }
    }

    private sealed class LoginData
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }
    }
}
