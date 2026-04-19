using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text;
using System.Text.Json;

namespace EventPrime.Api.Functions;

/// <summary>
/// Serves the OpenAPI 3.0 specification and Swagger UI for the EventPrime API.
///
/// Endpoints exposed:
///   GET /api/swagger/ui   – Interactive Swagger UI (HTML, loaded from CDN)
///   GET /api/swagger.json – OpenAPI 3.0 specification (JSON)
/// </summary>
public class OpenApiFunction
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    // ---------------------------------------------------------------------------
    // Swagger UI
    // ---------------------------------------------------------------------------

    /// <summary>
    /// GET /api/swagger/ui
    /// Returns an HTML page that loads the Swagger UI from CDN and points it at
    /// /api/swagger.json so it renders the live spec without any extra build step.
    /// </summary>
    [Function("SwaggerUI")]
    public async Task<HttpResponseData> GetSwaggerUi(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "swagger/ui")] HttpRequestData req)
    {
        const string html = """
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="UTF-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>EventPrime API – Swagger UI</title>
              <link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist@5/swagger-ui.css" />
            </head>
            <body>
              <div id="swagger-ui"></div>
              <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
              <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-standalone-preset.js"></script>
              <script>
                window.onload = function () {
                  SwaggerUIBundle({
                    url: "/api/swagger.json",
                    dom_id: "#swagger-ui",
                    presets: [SwaggerUIBundle.presets.apis, SwaggerUIStandalonePreset],
                    layout: "StandaloneLayout",
                    deepLinking: true,
                    displayRequestDuration: true,
                    filter: true,
                  });
                };
              </script>
            </body>
            </html>
            """;

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/html; charset=utf-8");
        await response.WriteStringAsync(html, Encoding.UTF8);
        return response;
    }

    // ---------------------------------------------------------------------------
    // OpenAPI 3.0 specification
    // ---------------------------------------------------------------------------

    /// <summary>
    /// GET /api/swagger.json
    /// Returns the OpenAPI 3.0 specification that describes all API endpoints.
    /// </summary>
    [Function("OpenApiSpec")]
    public async Task<HttpResponseData> GetOpenApiSpec(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "swagger.json")] HttpRequestData req)
    {
        var spec = BuildSpec();
        var json = JsonSerializer.Serialize(spec, JsonOptions);

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(json, Encoding.UTF8);
        return response;
    }

    // ---------------------------------------------------------------------------
    // Spec builder
    // ---------------------------------------------------------------------------

    private static object BuildSpec() => new
    {
        openapi = "3.0.3",
        info = new
        {
            title = "EventPrime API",
            description = "REST API for managing events, authentication, and contact form submissions.",
            version = "1.0.0",
        },
        servers = new[]
        {
            new { url = "/", description = "Current host" },
        },
        tags = new[]
        {
            new { name = "Health",  description = "Readiness probes and status checks." },
            new { name = "Events",  description = "CRUD operations for events." },
            new { name = "Contact", description = "Contact form submissions." },
            new { name = "Auth",    description = "Authentication (placeholder – replace before production)." },
        },
        paths = new Dictionary<string, object>
        {
            ["/api/health"] = new
            {
                get = new
                {
                    tags = new[] { "Health" },
                    operationId = "Health.Get",
                    summary = "Health check",
                    description = "Returns the current health status of the API. Use as a readiness probe.",
                    responses = new Dictionary<string, object>
                    {
                        ["200"] = Response("API is healthy.", Schema(new
                        {
                            type = "object",
                            properties = new
                            {
                                success   = new { type = "boolean", example = true },
                                message   = new { type = "string",  example = "EventPrime API is running." },
                                data      = new
                                {
                                    type = "object",
                                    properties = new
                                    {
                                        status    = new { type = "string", example = "Healthy" },
                                        timestamp = new { type = "string", format = "date-time" },
                                        version   = new { type = "string", example = "1.0.0" },
                                    },
                                },
                            },
                        })),
                    },
                },
            },

            ["/api/events"] = new
            {
                get = new
                {
                    tags = new[] { "Events" },
                    operationId = "Events.GetAll",
                    summary = "List events",
                    description = "Returns all events. Optionally filter by category and/or location.",
                    parameters = new[]
                    {
                        QueryParam("category", "string", "Filter by event category (e.g. Music, Sports)."),
                        QueryParam("location", "string", "Filter by event location (e.g. New York)."),
                    },
                    responses = new Dictionary<string, object>
                    {
                        ["200"] = Response("List of matching events.", Schema(new
                        {
                            type = "object",
                            properties = new
                            {
                                success = new { type = "boolean", example = true },
                                data    = new { type = "array", items = EventSchema() },
                            },
                        })),
                    },
                },
                post = new
                {
                    tags = new[] { "Events" },
                    operationId = "Events.Create",
                    summary = "Create event",
                    description = "Creates a new event and returns it with its generated ID.",
                    requestBody = RequestBody("Event details.", new
                    {
                        type = "object",
                        required = new[] { "title", "description", "category", "location", "date", "organizerName" },
                        properties = new
                        {
                            title         = new { type = "string", maxLength = 200,  example = "Summer Jazz Festival" },
                            description   = new { type = "string", maxLength = 2000, example = "An evening of live jazz." },
                            category      = new { type = "string",                  example = "Music" },
                            location      = new { type = "string",                  example = "Central Park, NY" },
                            date          = new { type = "string", format = "date-time", example = "2025-07-04T19:00:00Z" },
                            price         = new { type = "number", format = "decimal", minimum = 0, example = 25.00 },
                            capacity      = new { type = "integer", minimum = 1, example = 500 },
                            imageUrl      = new { type = "string",                  example = "https://example.com/image.jpg" },
                            organizerName = new { type = "string", maxLength = 200,  example = "Jazz Society NYC" },
                        },
                    }),
                    responses = new Dictionary<string, object>
                    {
                        ["201"] = Response("Event created successfully.", Schema(new
                        {
                            type = "object",
                            properties = new
                            {
                                success = new { type = "boolean", example = true },
                                message = new { type = "string",  example = "Event created successfully." },
                                data    = EventSchema(),
                            },
                        })),
                        ["400"] = Response("Validation failed or invalid JSON.", Schema(new
                        {
                            type = "object",
                            properties = new
                            {
                                success = new { type = "boolean", example = false },
                                message = new { type = "string",  example = "Validation failed." },
                                errors  = new { type = "array", items = new { type = "string" } },
                            },
                        })),
                    },
                },
            },

            ["/api/events/{id}"] = new
            {
                get = new
                {
                    tags = new[] { "Events" },
                    operationId = "Events.GetById",
                    summary = "Get event by ID",
                    description = "Returns a single event identified by its unique ID.",
                    parameters = new[]
                    {
                        PathParam("id", "The unique event ID (GUID string)."),
                    },
                    responses = new Dictionary<string, object>
                    {
                        ["200"] = Response("The requested event.", Schema(new
                        {
                            type = "object",
                            properties = new
                            {
                                success = new { type = "boolean", example = true },
                                data    = EventSchema(),
                            },
                        })),
                        ["404"] = Response("Event not found.", Schema(new
                        {
                            type = "object",
                            properties = new
                            {
                                success = new { type = "boolean", example = false },
                                message = new { type = "string",  example = "Event 'abc' not found." },
                            },
                        })),
                    },
                },
            },

            ["/api/contact"] = new
            {
                post = new
                {
                    tags = new[] { "Contact" },
                    operationId = "Contact.Submit",
                    summary = "Submit contact form",
                    description = "Accepts a contact form submission and returns an acknowledgement.",
                    requestBody = RequestBody("Contact form details.", new
                    {
                        type = "object",
                        required = new[] { "name", "email", "subject", "message" },
                        properties = new
                        {
                            name    = new { type = "string", maxLength = 200,  example = "Jane Doe" },
                            email   = new { type = "string", format = "email", maxLength = 320, example = "jane@example.com" },
                            subject = new { type = "string", maxLength = 200,  example = "Event inquiry" },
                            message = new { type = "string", maxLength = 4000, example = "I'd like more information about the Jazz Festival." },
                        },
                    }),
                    responses = new Dictionary<string, object>
                    {
                        ["200"] = Response("Submission received.", Schema(new
                        {
                            type = "object",
                            properties = new
                            {
                                success = new { type = "boolean", example = true },
                                message = new { type = "string",  example = "Thank you for reaching out! We'll get back to you shortly." },
                                data    = new
                                {
                                    type = "object",
                                    properties = new
                                    {
                                        receivedAt = new { type = "string", format = "date-time" },
                                    },
                                },
                            },
                        })),
                        ["400"] = Response("Validation failed or invalid JSON.", Schema(new
                        {
                            type = "object",
                            properties = new
                            {
                                success = new { type = "boolean", example = false },
                                message = new { type = "string",  example = "Validation failed." },
                                errors  = new { type = "array", items = new { type = "string" } },
                            },
                        })),
                    },
                },
            },

            ["/api/auth/login"] = new
            {
                post = new
                {
                    tags = new[] { "Auth" },
                    operationId = "Auth.Login",
                    summary = "Admin login",
                    description = "Validates admin credentials and returns a token placeholder. **Replace with a real identity provider before production.**",
                    requestBody = RequestBody("Admin credentials.", new
                    {
                        type = "object",
                        required = new[] { "email", "password" },
                        properties = new
                        {
                            email    = new { type = "string", format = "email", example = "admin@eventprime.com" },
                            password = new { type = "string", format = "password", example = "admin123" },
                        },
                    }),
                    responses = new Dictionary<string, object>
                    {
                        ["200"] = Response("Login successful.", Schema(new
                        {
                            type = "object",
                            properties = new
                            {
                                success = new { type = "boolean", example = true },
                                message = new { type = "string",  example = "Login successful." },
                                data    = new
                                {
                                    type = "object",
                                    properties = new
                                    {
                                        success = new { type = "boolean", example = true },
                                        token   = new { type = "string",  example = "placeholder-token-xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" },
                                        message = new { type = "string",  example = "Login successful." },
                                    },
                                },
                            },
                        })),
                        ["400"] = Response("Missing or invalid request body.", Schema(new
                        {
                            type = "object",
                            properties = new
                            {
                                success = new { type = "boolean", example = false },
                                message = new { type = "string",  example = "Email and password are required." },
                            },
                        })),
                        ["401"] = Response("Invalid credentials.", Schema(new
                        {
                            type = "object",
                            properties = new
                            {
                                success = new { type = "boolean", example = false },
                                message = new { type = "string",  example = "Invalid email or password." },
                            },
                        })),
                    },
                },
            },
        },
    };

    // ---------------------------------------------------------------------------
    // Spec helpers
    // ---------------------------------------------------------------------------

    private static object EventSchema() => new
    {
        type = "object",
        properties = new
        {
            id            = new { type = "string", format = "uuid",      example = "3fa85f64-5717-4562-b3fc-2c963f66afa6" },
            title         = new { type = "string",                       example = "Summer Jazz Festival" },
            description   = new { type = "string",                       example = "An evening of live jazz." },
            category      = new { type = "string",                       example = "Music" },
            location      = new { type = "string",                       example = "Central Park, NY" },
            date          = new { type = "string", format = "date-time", example = "2025-07-04T19:00:00Z" },
            price         = new { type = "number", format = "decimal",   example = 25.00 },
            capacity      = new { type = "integer",                      example = 500 },
            imageUrl      = new { type = "string",                       example = "https://example.com/image.jpg" },
            organizerName = new { type = "string",                       example = "Jazz Society NYC" },
            createdAt     = new { type = "string", format = "date-time", example = "2025-01-01T00:00:00Z" },
        },
    };

    private static object QueryParam(string name, string type, string description) => new
    {
        name,
        @in = "query",
        required = false,
        description,
        schema = new { type },
    };

    private static object PathParam(string name, string description) => new
    {
        name,
        @in = "path",
        required = true,
        description,
        schema = new { type = "string" },
    };

    private static object RequestBody(string description, object schemaContent) => new
    {
        required = true,
        description,
        content = new Dictionary<string, object>
        {
            ["application/json"] = new { schema = schemaContent },
        },
    };

    private static object Response(string description, object content) => new
    {
        description,
        content,
    };

    private static object Schema(object schema) => new Dictionary<string, object>
    {
        ["application/json"] = new { schema },
    };
}
