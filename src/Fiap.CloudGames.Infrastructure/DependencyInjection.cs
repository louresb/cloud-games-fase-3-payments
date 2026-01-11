using System.Reflection;
using Fiap.CloudGames.Domain.Payments.Contracts;
using Fiap.CloudGames.Domain.Payments.Repositories;
using Fiap.CloudGames.Infrastructure.Payments.Repositories;
using Fiap.CloudGames.Infrastructure.Payments.Services;
using Fiap.CloudGames.Infrastructure.Persistence;
using MassTransit;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fiap.CloudGames.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration, 
        Assembly applicationAssembly,
        Type[]? consumerCommandTypes = null,
        Type[]? consumerEventTypes = null)
    {
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentGateway, PaymentGateway>();

        var paymentsCommandsQueue = configuration["Queues:Payments:Commands"] ?? throw new InvalidOperationException("Payments commands queue not configured.");
        var paymentsEventsQueue = configuration["Queues:Payments:Events"] ?? throw new InvalidOperationException("Payments events queue not configured.");

        consumerCommandTypes = consumerCommandTypes?.Where(t => typeof(IConsumer).IsAssignableFrom(t)).ToArray() ?? [];
        consumerEventTypes = consumerEventTypes?.Where(t => typeof(IConsumer).IsAssignableFrom(t)).ToArray() ?? [];

        services.AddMassTransit(x =>
        {
            // Descobre automaticamente os Consumers na camada de Application
            x.AddConsumers(applicationAssembly);

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitHost = configuration["RabbitMq:HostName"] ?? "localhost";
                var rabbitUser = configuration["RabbitMq:UserName"] ?? "guest";
                var rabbitPass = configuration["RabbitMq:Password"] ?? "guest";

                cfg.Host(rabbitHost, "/", h =>
                {
                    h.ConnectionName("Fiap.CloudGames.Payments.Worker");
                    h.Username(rabbitUser);
                    h.Password(rabbitPass);
                });

                cfg.ReceiveEndpoint(paymentsCommandsQueue, e =>
                {
                    foreach (var consumerType in consumerCommandTypes)
                    {
                        if (!typeof(IConsumer).IsAssignableFrom(consumerType))
                            throw new InvalidOperationException($"Type {consumerType.FullName} is not a MassTransit consumer.");

                        e.ConfigureConsumer(context, consumerType);
                    }
                });
                
                cfg.ReceiveEndpoint(paymentsEventsQueue, e =>
                {
                    foreach (var consumerType in consumerEventTypes)
                    {
                        if (!typeof(IConsumer).IsAssignableFrom(consumerType))
                            throw new InvalidOperationException($"Type {consumerType.FullName} is not a MassTransit consumer.");

                        e.ConfigureConsumer(context, consumerType);
                    }
                });
            });
        });

        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name);

                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            });
        });

        services.AddDataProtection()
            .SetApplicationName("Fiap.CloudGames")
            .PersistKeysToDbContext<AppDbContext>();

        services.AddHealthChecks()
            .AddSqlServer(
                connectionString: connectionString, 
                name: "sqlserver",
                tags: new[] { "db", "data" });
    }
}
