namespace Fiap.CloudGames.Worker.Filters;

public class ApiKeyEndpointFilter : IEndpointFilter
{
    public const string ApiKeyHeaderName = "X-WEBHOOK-API-KEY"; // O nome do Header padrão
    private const string ApiKeyConfigName = "PaymentSettings:WebhookApiKey"; // Onde estará no appsettings

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // 1. Tenta pegar o valor do Header
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            return Results.Json(new { error = "API Key ausente" }, statusCode: 401);
        }

        // 2. Pega a chave configurada no appsettings.json
        // Usamos o RequestServices para acessar a configuração sem injeção no construtor do atributo
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var apiKey = configuration.GetValue<string>(ApiKeyConfigName) ?? string.Empty;

        // 3. Compara (simples e direto)
        if (!apiKey.Equals(extractedApiKey))
        {
            return Results.Json(new { error = "Acesso negado" }, statusCode: 403);
        }

        return await next(context);
    }
}