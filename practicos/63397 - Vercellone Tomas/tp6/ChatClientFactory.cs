using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

namespace AsistenteIA.Services;

public static class ChatClientFactory
{
    public static IChatClient Create(ChatConfiguration config)
    {
        var client = new OpenAIClient(
            new ApiKeyCredential(config.ApiKey),
            new OpenAIClientOptions
            {
                Endpoint = new Uri(config.ApiUrl)
            });

        return client.GetChatClient(config.Model).AsIChatClient();
    }
}
