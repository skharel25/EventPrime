# EventPrime.Api

Azure Functions isolated-worker backend for the EventPrime application, targeting .NET 8 and Azure Functions v4.

## Project structure

```
EventPrime.Api/
├── Functions/
│   ├── HealthFunction.cs    – GET  /api/health
│   ├── EventsFunction.cs    – GET  /api/events
│   │                          GET  /api/events/{id}
│   │                          POST /api/events
│   ├── ContactFunction.cs   – POST /api/contact
│   └── AuthFunction.cs      – POST /api/auth/login
├── Models/
│   ├── Event.cs
│   ├── CreateEventRequest.cs
│   ├── ContactRequest.cs
│   ├── LoginRequest.cs
│   └── ApiResponse.cs
├── Services/
│   └── EventStore.cs        – In-memory data store (replace with a real DB)
├── host.json
├── local.settings.json      – Not committed; local dev only
└── Program.cs
```

## Prerequisites

| Tool | Version |
|------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0 |
| [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local) | v4 |
| [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) (local storage emulator) | latest |

Install Azure Functions Core Tools:

```bash
npm install -g azure-functions-core-tools@4 --unsafe-perm true
```

Install Azurite:

```bash
npm install -g azurite
```

## Running locally

1. Start the local storage emulator in a separate terminal:

   ```bash
   azurite --silent
   ```

2. Start the function app from the `EventPrime.Api` directory:

   ```bash
   cd EventPrime.Api
   func start
   ```

   The API will be available at `http://localhost:7071/api/`.

## Available endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/health` | Health check / readiness probe |
| `GET` | `/api/events` | List all events (supports `?category=` and `?location=` query params) |
| `GET` | `/api/events/{id}` | Get a single event by ID |
| `POST` | `/api/events` | Create a new event |
| `POST` | `/api/contact` | Submit a contact form message |
| `POST` | `/api/auth/login` | Admin login (returns a token) |

### Example requests

```bash
# Health check
curl http://localhost:7071/api/health

# Get all events
curl http://localhost:7071/api/events

# Get events filtered by category
curl "http://localhost:7071/api/events?category=Music&location=New+York"

# Get a single event
curl http://localhost:7071/api/events/1

# Create an event
curl -X POST http://localhost:7071/api/events \
  -H "Content-Type: application/json" \
  -d '{
    "title": "My Event",
    "description": "A great event",
    "category": "Music",
    "location": "New York",
    "date": "2025-09-01T18:00:00Z",
    "price": 50.00,
    "capacity": 200,
    "organizerName": "John Doe"
  }'

# Submit a contact form
curl -X POST http://localhost:7071/api/contact \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Jane Smith",
    "email": "jane@example.com",
    "subject": "General Inquiry",
    "message": "Hello, I have a question about EventPrime."
  }'

# Admin login
curl -X POST http://localhost:7071/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "admin@eventprime.com", "password": "admin123"}'
```

## Deploying to Azure

### 1. Provision resources (one-time)

```bash
# Set variables
RESOURCE_GROUP="rg-eventprime"
LOCATION="eastus"
STORAGE_ACCOUNT="steventprime$RANDOM"
FUNCTION_APP="func-eventprime-api"

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create storage account
az storage account create \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --sku Standard_LRS

# Create the Function App
az functionapp create \
  --resource-group $RESOURCE_GROUP \
  --consumption-plan-location $LOCATION \
  --runtime dotnet-isolated \
  --runtime-version 8 \
  --functions-version 4 \
  --name $FUNCTION_APP \
  --storage-account $STORAGE_ACCOUNT
```

### 2. Publish

```bash
cd EventPrime.Api
dotnet publish -c Release
func azure functionapp publish $FUNCTION_APP
```

## Next steps

- Replace `InMemoryEventStore` with a proper database (Azure Cosmos DB, Azure SQL, etc.)
- Replace the mock credentials in `AuthFunction` with a real identity provider and issue a signed JWT
- Add Application Insights for telemetry (set `APPLICATIONINSIGHTS_CONNECTION_STRING` in App Settings)
- Add CORS settings in `host.json` or the Azure Portal so the Blazor frontend can call the API
