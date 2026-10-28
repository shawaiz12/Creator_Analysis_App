using System.ClientModel;
using CreatorAnalytics.Strategy;
using Microsoft.Extensions.AI;
using OpenAI;

namespace CreatorAnalytics.Api.Ai;

public static class AiServiceCollectionExtensions
{
    private const string GeminiEndpoint = "https://generativelanguage.googleapis.com/v1beta/openai/";

    /// <summary>
    /// Registers a chat model when Ai:Provider is set ("Gemini" or "OpenAICompatible").
    /// With nothing configured, the deterministic template writer stays in use.
    /// </summary>
    public static IServiceCollection AddConfiguredAi(
        this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Ai:Provider"];

        if (string.IsNullOrWhiteSpace(provider))
            return services;

        var isGemini = string.Equals(provider, "Gemini", StringComparison.OrdinalIgnoreCase);
        var isGeneric = string.Equals(provider, "OpenAICompatible", StringComparison.OrdinalIgnoreCase);

        if (!isGemini && !isGeneric)
        {
            throw new InvalidOperationException(
                $"Unsupported Ai:Provider '{provider}'. Use 'Gemini' or 'OpenAICompatible'.");
        }

        var apiKey = Required(configuration, "Ai:ApiKey");
        var model = Required(configuration, "Ai:Model");
        var endpoint = isGemini
            ? configuration["Ai:Endpoint"] ?? GeminiEndpoint
            : Required(configuration, "Ai:Endpoint");
        var timeoutSeconds = int.TryParse(configuration["Ai:TimeoutSeconds"], out var parsed) ? parsed : 90;

        var client = new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions
            {
                Endpoint = new Uri(endpoint),
                NetworkTimeout = TimeSpan.FromSeconds(timeoutSeconds)
            });

        IChatClient chat = new StatusMappingChatClient(client.GetChatClient(model).AsIChatClient());

        services.AddSingleton<IChatClient>(chat);
        services.UseChatInsightGenerator();

        return services;
    }

    private static string Required(IConfiguration configuration, string key)
    {
        var value = configuration[key];

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{key} is required when Ai:Provider is set.");

        return value;
    }
}