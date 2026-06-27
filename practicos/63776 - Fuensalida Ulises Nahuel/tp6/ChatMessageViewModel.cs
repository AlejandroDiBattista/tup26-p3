internal sealed record ChatMessageViewModel(string Role, string Content)
{
    public string Heading => Role switch
    {
        "user" => "Usuario",
        "assistant" => "Asistente",
        "system" => "Sistema",
        _ => Role
    };
}
