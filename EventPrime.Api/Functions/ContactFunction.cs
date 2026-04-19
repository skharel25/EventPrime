using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using EventPrime.Api.Models;

namespace EventPrime.Api.Functions;

public class ContactFunction
{
    private readonly ILogger<ContactFunction> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public ContactFunction(ILogger<ContactFunction> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// POST /api/contact
    /// Accepts a contact form submission and acknowledges it.
    /// Replace the body with real email or notification logic as needed.
    /// </summary>
    [Function("Contact")]
    public async Task<HttpResponseData> SubmitContact(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "contact")] HttpRequestData req)
    {
        _logger.LogInformation("POST /api/contact");

        ContactRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<ContactRequest>(req.Body, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON in Contact request.");
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(ApiResponse<object>.Fail("Invalid JSON payload."));
            return badRequest;
        }

        if (body is null)
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(ApiResponse<object>.Fail("Request body is required."));
            return badRequest;
        }

        var errors = ValidateRequest(body);
        if (errors.Count > 0)
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(ApiResponse<object>.Fail("Validation failed.", errors));
            return badRequest;
        }

        // TODO: Forward to email provider (e.g. SendGrid / Communication Services)
        _logger.LogInformation(
            "Contact form submitted – from: {Email}, subject: {Subject}",
            body.Email,
            body.Subject);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(
            ApiResponse<object>.Ok(new { ReceivedAt = DateTimeOffset.UtcNow },
            "Thank you for reaching out! We'll get back to you shortly."));
        return response;
    }

    private static List<string> ValidateRequest(ContactRequest body)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(body.Name))
            errors.Add("Name is required.");

        if (string.IsNullOrWhiteSpace(body.Email))
            errors.Add("Email is required.");

        if (string.IsNullOrWhiteSpace(body.Subject))
            errors.Add("Subject is required.");

        if (string.IsNullOrWhiteSpace(body.Message))
            errors.Add("Message is required.");

        return errors;
    }
}
