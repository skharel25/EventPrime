using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using EventPrime.Api.Models;

namespace EventPrime.Api.Functions;

public class HealthFunction
{
    private readonly ILogger<HealthFunction> _logger;

    public HealthFunction(ILogger<HealthFunction> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// GET /api/health
    /// Returns the current health status of the API. Useful for readiness probes and monitoring.
    /// </summary>
    [Function("Health")]
    public async Task<HttpResponseData> GetHealth(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
    {
        _logger.LogInformation("Health check requested.");

        var payload = new ApiResponse<object>
        {
            Success = true,
            Data = new
            {
                Status = "Healthy",
                Timestamp = DateTimeOffset.UtcNow,
                Version = "1.0.0",
            },
            Message = "EventPrime API is running.",
        };

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}
