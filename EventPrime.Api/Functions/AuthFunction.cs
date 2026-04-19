using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using EventPrime.Api.Models;

namespace EventPrime.Api.Functions;

public class AuthFunction
{
    private readonly ILogger<AuthFunction> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public AuthFunction(ILogger<AuthFunction> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// POST /api/auth/login
    /// Validates admin credentials and returns a token placeholder.
    /// TODO: Replace mock credentials with a real identity provider (e.g. Azure AD B2C, ASP.NET Identity).
    /// TODO: Issue a real JWT instead of the placeholder token.
    /// </summary>
    [Function("Login")]
    public async Task<HttpResponseData> Login(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/login")] HttpRequestData req)
    {
        _logger.LogInformation("POST /api/auth/login");

        LoginRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<LoginRequest>(req.Body, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON in Login request.");
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(ApiResponse<LoginResponse>.Fail("Invalid JSON payload."));
            return badRequest;
        }

        if (body is null || string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Password))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(ApiResponse<LoginResponse>.Fail("Email and password are required."));
            return badRequest;
        }

        // TODO: Replace with real credential validation (hashed passwords, identity provider, etc.)
        const string MockEmail = "admin@eventprime.com";
        const string MockPassword = "admin123";

        if (!body.Email.Equals(MockEmail, StringComparison.OrdinalIgnoreCase) || body.Password != MockPassword)
        {
            _logger.LogWarning("Failed login attempt for {Email}.", body.Email);
            var unauthorized = req.CreateResponse(HttpStatusCode.Unauthorized);
            await unauthorized.WriteAsJsonAsync(ApiResponse<LoginResponse>.Fail("Invalid email or password."));
            return unauthorized;
        }

        // TODO: Generate a proper JWT signed with a secret key stored in Azure Key Vault / App Settings
        var loginResponse = new LoginResponse
        {
            Success = true,
            Token = $"placeholder-token-{Guid.NewGuid()}",
            Message = "Login successful.",
        };

        _logger.LogInformation("Admin login successful for {Email}.", body.Email);
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(ApiResponse<LoginResponse>.Ok(loginResponse, "Login successful."));
        return response;
    }
}
