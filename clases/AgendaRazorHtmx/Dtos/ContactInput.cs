namespace AgendaRazorHtmx.Dtos;

public record ContactInput(
    int Id,
    string Nombre,
    string Apellido,
    string Telefono,
    string Email,
    string Notas
);
