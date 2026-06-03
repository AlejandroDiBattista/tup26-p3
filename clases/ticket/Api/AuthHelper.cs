using Agenda.Domain;

namespace Agenda.Api;

public static class AuthHelper {
    public static Usuario? UsuarioActual(this HttpContext context) {
        return context.Items["Usuario"] as Usuario;
    }
}