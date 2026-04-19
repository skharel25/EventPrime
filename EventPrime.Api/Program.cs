using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using EventPrime.Api.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        // Register in-memory data store (replace with a real database later)
        services.AddSingleton<IEventStore, InMemoryEventStore>();
    })
    .Build();

host.Run();
