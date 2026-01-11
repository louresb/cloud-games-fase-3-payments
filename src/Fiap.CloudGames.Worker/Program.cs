using System.Reflection;
using Fiap.CloudGames.Application.Payments.Consumers;
using Fiap.CloudGames.Application.Payments.Dtos;
using Fiap.CloudGames.Application.Payments.Services;
using Fiap.CloudGames.Infrastructure;
using Fiap.CloudGames.Infrastructure.Persistence;
using Fiap.CloudGames.Worker.Filters;
using Fiap.CloudGames.Worker.Middlewares;
using FluentValidation;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuração do Serilog (Console + Loki)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Fiap.CloudGames.Payments.Worker")
    .WriteTo.Console()
    .WriteTo.GrafanaLoki(
        uri: builder.Configuration["Loki:Url"] ?? "http://localhost:3100",
        labels: new[]
        {
            new LokiLabel { Key = "service", Value = "payments-svc" },
            new LokiLabel { Key = "env", Value = builder.Environment.EnvironmentName.ToLower() }
        }
    )
    .CreateLogger();

// Substitui o logger padrão do .NET pelo Serilog
builder.Host.UseSerilog();

builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddValidatorsFromAssemblyContaining<Fiap.CloudGames.Application.Payments.Validators.PaymentGatewayCallbackValidator>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	var xmlFile = Path.ChangeExtension(Assembly.GetEntryAssembly()?.Location, ".xml");
	if (File.Exists(xmlFile)) c.IncludeXmlComments(xmlFile);

	c.SwaggerDoc("v1", new OpenApiInfo { Title = "Fiap.CloudGames Payment API", Version = "v1" });

	// Define o esquema de segurança (Tipo: ApiKey)
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Name = ApiKeyEndpointFilter.ApiKeyHeaderName,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme",
        In = ParameterLocation.Header,
        Description = "Insira sua API Key no formato: {key}",
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                },
                In = ParameterLocation.Header
            },
			Array.Empty<string>()
        }
    });
});

builder.Services.AddInfrastructure(
    builder.Configuration, 
    typeof(InitiatePaymentConsumer).Assembly,
    consumerCommandTypes: 
    [
        typeof(InitiatePaymentConsumer),
        typeof(RefundPaymentConsumer)
    ]);

var app = builder.Build();

// ---- Migrate ----
using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	await db.Database.MigrateAsync();
}

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Liveness: Só diz que o processo está de pé
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

// Readiness: Diz se as dependências estão OK
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapPost("/api/webhooks/gateway-notification", async (
    PaymentGatewayCallbackDto dto, 
    IPaymentService service,
    CancellationToken ct) =>
{
    var message = await service.ProcessTransactionAsync(dto, ct);
    return Results.Ok(new { Message = message });
})
.WithName("GatewayWebhook")
.Produces<object>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Accepts<PaymentGatewayCallbackDto>("application/json")
.AddEndpointFilter<ApiKeyEndpointFilter>()
.AddEndpointFilter<ValidationFilter<PaymentGatewayCallbackDto>>();

app.Run();