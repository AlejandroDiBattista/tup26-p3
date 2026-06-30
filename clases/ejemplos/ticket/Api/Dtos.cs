using Agenda.Domain;

namespace Agenda.Api;

public record RegistroDto(string Nombre, string Email, string Password, TipoUsuario Tipo);
public record LoginDto(string Email, string Password);
public record CrearTicketDto(string Titulo, string? Descripcion, int? ResponsableId);
public record CambiarEstadoDto(EstadoTicket Estado);
public record AsignarResponsableDto(int ResponsableId);
public record RegistrarAccionDto(string Descripcion, DateTime Fecha);