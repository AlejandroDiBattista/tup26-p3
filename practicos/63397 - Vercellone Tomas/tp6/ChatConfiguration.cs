namespace AsistenteIA.Services;

public sealed record ChatConfiguration(string Provider, string ApiUrl, string ApiKey, string Model)
{
    public static ChatConfiguration FromEnvironment(string providerName)
    {
        var provider = providerName.Trim().ToUpperInvariant();
        var apiUrl = Environment.GetEnvironmentVariable($"{provider}_API_URL");
        var apiKey = Environment.GetEnvironmentVariable($"{provider}_API_KEY");
        var model = Environment.GetEnvironmentVariable($"{provider}_MODEL");

        if (string.IsNullOrWhiteSpace(apiUrl))
            throw new InvalidOperationException($"Falta la variable {provider}_API_URL.");

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException($"Falta la variable {provider}_API_KEY.");

        if (string.IsNullOrWhiteSpace(model))
            throw new InvalidOperationException($"Falta la variable {provider}_MODEL.");

        return new ChatConfiguration(provider, NormalizeOpenAiCompatibleEndpoint(apiUrl), apiKey, model);
    }

    private static string NormalizeOpenAiCompatibleEndpoint(string apiUrl)
    {
        const string chatCompletionsSuffix = "/chat/completions";
        var trimmed = apiUrl.Trim().TrimEnd('/');

        return trimmed.EndsWith(chatCompletionsSuffix, StringComparison.OrdinalIgnoreCase)
            ? trimmed[..^chatCompletionsSuffix.Length]
            : trimmed;
    }
}
