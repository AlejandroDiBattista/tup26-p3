using Microsoft.Extensions.AI;
using System.Text;

internal sealed class ChatService
{
    private readonly IChatClient _chatClient;
    private readonly List<ChatMessage> _history;
    private readonly ChatOptions _chatOptions;

    public ChatService(IChatClient chatClient, string systemPrompt, IReadOnlyList<AITool> tools)
    {
        _chatClient = chatClient;
        _history = [new ChatMessage(ChatRole.System, systemPrompt)];
        _chatOptions = new ChatOptions { Tools = [.. tools] };
    }

    public async Task<string> SendAsync(string userMessage, Action<string> onDelta, CancellationToken cancellationToken = default)
    {
        var cleanMessage = userMessage.Trim();
        if (cleanMessage.Length == 0)
        {
            return string.Empty;
        }

        _history.Add(new ChatMessage(ChatRole.User, cleanMessage));

        var response = new StringBuilder();
        try
        {
            await foreach (var update in _chatClient.GetStreamingResponseAsync(_history, _chatOptions, cancellationToken))
            {
                if (string.IsNullOrEmpty(update.Text))
                {
                    continue;
                }

                response.Append(update.Text);
                onDelta(update.Text);
            }

            var assistantMessage = response.ToString();
            _history.Add(new ChatMessage(ChatRole.Assistant, assistantMessage));
            return assistantMessage;
        }
        catch
        {
            _history.RemoveAt(_history.Count - 1);
            throw;
        }
    }
}
