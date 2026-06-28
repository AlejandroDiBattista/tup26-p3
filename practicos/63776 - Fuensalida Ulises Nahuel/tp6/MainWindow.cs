using System.Text;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

internal sealed class MainWindow : Window
{
    private readonly ChatService _chatService;
    private readonly IApplication _app;
    private readonly List<ChatMessageViewModel> _messages = [];
    private readonly TextView _conversationView;
    private readonly TextField _input;
    private readonly Button _sendButton;
    private readonly Label _status;
    private bool _isResponding;
    private bool _autoScroll = true;

    public MainWindow(IApplication app, ChatService chatService, string model)
    {
        _app = app;
        _chatService = chatService;
        Title = $"AsistenteIA - {model}  (Enter envia, Esc sale)";
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        _conversationView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(4),
            ReadOnly = true,
            WordWrap = true,
            CanFocus = true
        };

        var inputPanel = new FrameView
        {
            Title = "Mensaje",
            X = 0,
            Y = Pos.Bottom(_conversationView),
            Width = Dim.Fill(),
            Height = 4
        };

        _input = new TextField
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(14),
            Height = 1,
            CanFocus = true
        };

        _sendButton = new Button
        {
            Text = "Enviar",
            X = Pos.Right(_input) + 1,
            Y = 1,
            Width = 10,
            IsDefault = true
        };

        _status = new Label
        {
            Text = "Listo",
            X = 1,
            Y = 2,
            Width = Dim.Fill(),
            Height = 1
        };

        inputPanel.Add(_input, _sendButton, _status);
        Add(_conversationView, inputPanel);

        _messages.Add(new ChatMessageViewModel("assistant", "Hola. Soy tu asistente de consola. En que te ayudo?"));
        RenderConversation();

        _sendButton.Accepting += (_, args) =>
        {
            args.Handled = true;
            _ = SendCurrentMessageAsync();
        };

        _input.KeyDown += (_, key) =>
        {
            if (key == Key.Enter)
            {
                key.Handled = true;
                _ = SendCurrentMessageAsync();
            }
        };

        _conversationView.KeyDown += (_, _) => UpdateAutoScrollPreference();
        _conversationView.MouseEvent += (_, _) => UpdateAutoScrollPreference();

        KeyDown += (_, key) =>
        {
            if (key == Key.Esc)
            {
                key.Handled = true;
                _app.RequestStop();
            }
        };
    }

    private async Task SendCurrentMessageAsync()
    {
        if (_isResponding)
        {
            return;
        }

        var userText = _input.Text?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userText))
        {
            _status.Text = "Escribi un mensaje antes de enviar.";
            return;
        }

        SetResponding(true);
        _input.Text = string.Empty;

        var assistantIndex = -1;
        var assistantBuilder = new StringBuilder();

        try
        {
            _messages.Add(new ChatMessageViewModel("user", userText.Trim()));
            _messages.Add(new ChatMessageViewModel("assistant", string.Empty));
            assistantIndex = _messages.Count - 1;
            RenderConversation();

            await _chatService.SendAsync(userText, delta =>
            {
                _app.Invoke(() =>
                {
                    assistantBuilder.Append(delta);
                    _messages[assistantIndex] = new ChatMessageViewModel("assistant", assistantBuilder.ToString());
                    RenderConversation();
                });
            });

            _app.Invoke(() =>
            {
                _messages[assistantIndex] = new ChatMessageViewModel("assistant", assistantBuilder.ToString());
                _status.Text = "Listo";
                RenderConversation();
            });
        }
        catch (Exception ex)
        {
            _app.Invoke(() =>
            {
                var error = $"No pude completar la respuesta: {ex.Message}";
                if (assistantIndex >= 0)
                {
                    _messages[assistantIndex] = new ChatMessageViewModel("assistant", error);
                }
                else
                {
                    _messages.Add(new ChatMessageViewModel("assistant", error));
                }

                _status.Text = "Error";
                RenderConversation();
            });
        }
        finally
        {
            _app.Invoke(() => SetResponding(false));
        }
    }

    private void SetResponding(bool responding)
    {
        _isResponding = responding;
        _input.Enabled = !responding;
        _sendButton.Enabled = !responding;
        _status.Text = responding ? "El asistente esta respondiendo..." : "Listo";

        if (!responding)
        {
            _input.SetFocus();
        }
    }

    private void RenderConversation()
    {
        _conversationView.Text = MarkdownRenderer.Render(_messages);

        if (_autoScroll)
        {
            ScrollToBottom();
        }

        _conversationView.SetNeedsDraw();
    }

    private void UpdateAutoScrollPreference()
    {
        // Terminal.Gui 2.4 no expone TopRow; si el usuario interactua con el panel,
        // detenemos el autoscroll hasta que se envie o llegue otro turno.
        _autoScroll = false;
    }

    private void ScrollToBottom()
    {
        var totalRows = Math.Max(1, _conversationView.Text?.ToString()?.Split('\n').Length ?? 1);
        _conversationView.ScrollTo(new System.Drawing.Point(0, totalRows));
    }
}
