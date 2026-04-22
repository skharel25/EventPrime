using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using EventPrime.Api.Models;
using EventPrime.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace EventPrime.Api.Functions;

public class AuthFunction
{
    private readonly ILogger<AuthFunction> _logger;
    private readonly IUserStore _userStore;
    private readonly IConfiguration _configuration;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    // Computed once at class initialization so a lookup for a non-existent user still performs
    // a full BCrypt verification, preventing timing-based user enumeration.
    private static readonly string DummyPasswordHash =
        BCrypt.Net.BCrypt.HashPassword("dummy_constant_value", workFactor: 11);

    public AuthFunction(ILogger<AuthFunction> logger, IUserStore userStore, IConfiguration configuration)
    {
        _logger = logger;
        _userStore = userStore;
        _configuration = configuration;
    }

    /// <summary>
    /// POST /api/auth/login
    /// Validates credentials against the Azure Table Storage users table and returns a signed JWT.
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

        var user = await _userStore.GetByEmailAsync(body.Email);

        // DummyPasswordHash is used when the user does not exist so that BCrypt work is always
        // performed, preventing timing-based user enumeration.
        var hashToVerify = user?.PasswordHash ?? DummyPasswordHash;
        var passwordValid = BCrypt.Net.BCrypt.Verify(body.Password, hashToVerify);

        if (user is null || !passwordValid)
        {
            _logger.LogWarning("Failed login attempt for {Email}.", body.Email);
            var unauthorized = req.CreateResponse(HttpStatusCode.Unauthorized);
            await unauthorized.WriteAsJsonAsync(ApiResponse<LoginResponse>.Fail("Invalid email or password."));
            return unauthorized;
        }

        var token = GenerateJwt(user);

        var loginResponse = new LoginResponse
        {
            Success = true,
            Token = token,
            Message = "Login successful.",
        };

        _logger.LogInformation("Login successful for {Email}.", user.Email);
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(ApiResponse<LoginResponse>.Ok(loginResponse, "Login successful."));
        return response;
    }

    private string GenerateJwt(UserEntity user)
    {
        var secret = _configuration["JwtSecret"]
            ?? throw new InvalidOperationException("JwtSecret is not configured.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: "EventPrime.Api",
            audience: "EventPrime",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
