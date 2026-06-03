namespace RazorHolaMundo.Services;

public record ContactoInput(
    string Nombre,
    string Apellido,
    string Telefono,
    string? Email,
    bool EsFavorito
);
