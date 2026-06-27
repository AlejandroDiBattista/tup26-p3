using Microsoft.Extensions.AI;
using System.Text;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace AsistenteIA.UI;

public sealed class MainWindow : Window
{
    private readonly IChatClient _chatClient;
    private readonly ChatOptions _chatOptions;
    private readonly List<ChatMessage> _messages;
    private readonly Markdown _conversationView;
    private readonly TextField _input;
    private readonly Button _sendButton;
    private readonly StringBuilder _conversationMarkdown = new();
    private bool _isSending;
    private string _currentAssistantResponse = string.Empty;

    public MainWindow(
        string title,
        IChatClient chatClient,
        ChatOptions chatOptions,
        List<ChatMessage> messages)
    {
        _chatClient = chatClient;
        _chatOptions = chatOptions;
        _messages = messages;

        Title = title;
        Width = Dim.Fill();
        Height = Dim.Fill();

        _conversationView = new Markdown
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(3),
        };

        _conversationMarkdown.Append(MarkdownFormatter.FormatTurn(
            "Asistente",
            "Hola. Soy tu asistente de programacion. Escribi tu consulta y presiona Enter."));
        _conversationView.Text = _conversationMarkdown.ToString();

        _input = new TextField
        {
            X = 0,
            Y = Pos.AnchorEnd(2),
            Width = Dim.Fill(12),
            Height = 1
        };

        _sendButton = new Button
        {
            X = Pos.AnchorEnd(12),
            Y = Pos.AnchorEnd(2),
            Width = 12,
            Height = 1,
            Text = "Enviar",
            IsDefault = true
        };

        Add(_conversationView, _input, _sendButton);
        WireEvents();
    }

    private void WireEvents()
    {
        _sendButton.Accepting += (_, _) => StartSend();

        _input.KeyDown += (_, args) =>
        {
            if (args.KeyCode == KeyCode.Enter)
            {
                StartSend();
                args.Handled = true;
            }
        };

        KeyDown += (_, args) =>
        {
            if (args.KeyCode == KeyCode.Esc)
            {
                Application.RequestStop();
                args.Handled = true;
            }
        };
    }

    private void StartSend()
    {
        if (_isSending)
            return;

        var userText = _input.Text?.ToString()?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userText))
            return;

        _input.Text = string.Empty;
        SetInputEnabled(false);
        AppendToConversation(MarkdownFormatter.FormatTurn("Vos", userText));

        _messages.Add(new ChatMessage(ChatRole.User, userText));
        _currentAssistantResponse = string.Empty;
        AppendToConversation("## Asistente\n\n_Consultando al modelo..._\n\n");

        _ = Task.Run(() => SendMessageAsync(userText));
    }

    private async Task SendMessageAsync(string userText)
    {
        var response = new StringBuilder();

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));

            await foreach (var update in _chatClient.GetStreamingResponseAsync(_messages, _chatOptions, timeout.Token))
            {
                var text = update.Text;
                if (string.IsNullOrEmpty(text))
                    continue;

                response.Append(text);
                ReplaceCurrentAssistantResponse(response.ToString());
            }

            var finalResponse = response.ToString();
            if (string.IsNullOrWhiteSpace(finalResponse))
                finalResponse = "No llego contenido de texto desde el modelo. Revisa el modelo configurado y si el proveedor soporta streaming.";

            _messages.Add(new ChatMessage(ChatRole.Assistant, finalResponse));
            ReplaceCurrentAssistantResponse(finalResponse);
        }
        catch (OperationCanceledException)
        {
            RemoveLastUserMessage(userText);
            ReplaceCurrentAssistantResponse("Error: la consulta supero 60 segundos sin respuesta. Revisa la API key, el modelo y la conexion.");
        }
        catch (Exception ex)
        {
            RemoveLastUserMessage(userText);
            ReplaceCurrentAssistantResponse($"Error: {ex.Message}");
        }
        finally
        {
            SetInputEnabled(true);
        }
    }

    private void RemoveLastUserMessage(string userText)
    {
        var last = _messages.LastOrDefault();
        if (last?.Role == ChatRole.User && last.Text == userText)
            _messages.RemoveAt(_messages.Count - 1);
    }

    private void AppendToConversation(string text)
    {
        Application.Invoke(() =>
        {
            _conversationMarkdown.Append(text);
            _conversationView.Text = _conversationMarkdown.ToString();
            _conversationView.SetNeedsDraw();
            Application.LayoutAndDraw();
        });
    }

    private void ReplaceCurrentAssistantResponse(string text)
    {
        Application.Invoke(() =>
        {
            if (!string.IsNullOrEmpty(_currentAssistantResponse))
            {
                var previous = _conversationMarkdown.ToString();
                var index = previous.LastIndexOf(_currentAssistantResponse, StringComparison.Ordinal);

                if (index >= 0)
                    _conversationMarkdown.Remove(index, _currentAssistantResponse.Length);
            }

            _currentAssistantResponse = text.TrimEnd() + Environment.NewLine + Environment.NewLine;
            _conversationMarkdown.Append(_currentAssistantResponse);
            _conversationView.Text = _conversationMarkdown.ToString();
            _conversationView.SetNeedsDraw();
            Application.LayoutAndDraw();
        });
    }

    private void SetInputEnabled(bool enabled)
    {
        Application.Invoke(() =>
        {
            _isSending = !enabled;
            _input.Enabled = enabled;
            _sendButton.Enabled = enabled;

            if (enabled)
                _input.SetFocus();

            SetNeedsDraw();
            Application.LayoutAndDraw();
        });
    }
}
