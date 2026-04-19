using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using EventPrime.Api.Models;
using EventPrime.Api.Services;

namespace EventPrime.Api.Functions;

public class EventsFunction
{
    private readonly ILogger<EventsFunction> _logger;
    private readonly IEventStore _store;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public EventsFunction(ILogger<EventsFunction> logger, IEventStore store)
    {
        _logger = logger;
        _store = store;
    }

    /// <summary>
    /// GET /api/events[?category=Music&amp;location=New+York]
    /// Returns all events, optionally filtered by category and/or location.
    /// </summary>
    [Function("GetEvents")]
    public async Task<HttpResponseData> GetEvents(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "events")] HttpRequestData req)
    {
        _logger.LogInformation("GET /api/events");

        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var category = query["category"];
        var location = query["location"];

        var events = _store.GetAll(category, location);
        var payload = ApiResponse<IEnumerable<Event>>.Ok(events);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(payload);
        return response;
    }

    /// <summary>
    /// GET /api/events/{id}
    /// Returns a single event by its ID.
    /// </summary>
    [Function("GetEventById")]
    public async Task<HttpResponseData> GetEventById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "events/{id}")] HttpRequestData req,
        string id)
    {
        _logger.LogInformation("GET /api/events/{Id}", id);

        var ev = _store.GetById(id);
        if (ev is null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(ApiResponse<Event>.Fail($"Event '{id}' not found."));
            return notFound;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(ApiResponse<Event>.Ok(ev));
        return response;
    }

    /// <summary>
    /// POST /api/events
    /// Creates a new event. Returns the created event with its generated ID.
    /// </summary>
    [Function("CreateEvent")]
    public async Task<HttpResponseData> CreateEvent(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "events")] HttpRequestData req)
    {
        _logger.LogInformation("POST /api/events");

        CreateEventRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<CreateEventRequest>(req.Body, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON in CreateEvent request.");
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(ApiResponse<Event>.Fail("Invalid JSON payload."));
            return badRequest;
        }

        if (body is null)
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(ApiResponse<Event>.Fail("Request body is required."));
            return badRequest;
        }

        var validationErrors = ValidateRequest(body);
        if (validationErrors.Count > 0)
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(ApiResponse<Event>.Fail("Validation failed.", validationErrors));
            return badRequest;
        }

        var newEvent = new Event
        {
            Title = body.Title,
            Description = body.Description,
            Category = body.Category,
            Location = body.Location,
            Date = body.Date,
            Price = body.Price,
            Capacity = body.Capacity,
            ImageUrl = body.ImageUrl,
            OrganizerName = body.OrganizerName,
        };

        _store.Add(newEvent);

        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(ApiResponse<Event>.Ok(newEvent, "Event created successfully."));
        return response;
    }

    private static List<string> ValidateRequest(CreateEventRequest body)
    {
        var context = new ValidationContext(body);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(body, context, results, validateAllProperties: true);
        return results.Select(r => r.ErrorMessage ?? "Validation error.").ToList();
    }
}
