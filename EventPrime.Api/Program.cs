using EventPrime.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<IEventStore, InMemoryEventStore>();
        services.AddSingleton<IUserStore, UserStore>();
    })
    .Build();

// Ensure the users table exists and the initial admin user is seeded.
await host.Services.GetRequiredService<IUserStore>().SeedAdminUserAsync();

host.Run();
