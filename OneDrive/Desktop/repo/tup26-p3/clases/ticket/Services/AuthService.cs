using System.Security.Cryptography;
using Agenda.Domain;
using Agenda.Repositories;
using Agenda.Security;

namespace Agenda.Services;

public class AuthService {
    private readonly IUsuarioRepository repositorio;
    private static readonly TimeSpan DuracionSesion = TimeSpan.FromHours(8);

    public AuthService(IUsuarioRepository repositorio) {
        this.repositorio = repositorio;
    }

    public async Task<Usuario> RegistrarAsync(string nombre, string email, string password, TipoUsuario tipo) {
        var existente = await this.repositorio.ObtenerPorEmailAsync(email);
        if (existente is not null) {
            throw new InvalidOperationException("Ya existe un usuario con ese email.");
        }

        var (hash, salt) = PasswordHasher.Hashear(password);
        var usuario = new Usuario {
            Nombre = nombre,
            Email = email,
            Tipo = tipo,
            PasswordHash = hash,
            PasswordSalt = salt
        };

        await this.repositorio.AgregarAsync(usuario);
        await this.repositorio.GuardarCambiosAsync();
        return usuario;
    }

    public async Task<string?> LoginAsync(string email, string password) {
        var usuario = await this.repositorio.ObtenerPorEmailAsync(email);
        if (usuario is null) {
            return null;
        }

        var valido = PasswordHasher.Verificar(password, usuario.PasswordHash, usuario.PasswordSalt);
        if (!valido) {
            return null;
        }

        usuario.Token = GenerarToken();
        usuario.TokenExpira = DateTime.Now.Add(DuracionSesion);
        await this.repositorio.GuardarCambiosAsync();

        return usuario.Token;
    }

    public async Task<Usuario?> ValidarTokenAsync(string token) {
        var usuario = await this.repositorio.ObtenerPorTokenAsync(token);
        if (usuario is null) {
            return null;
        }

        if (usuario.TokenExpira is null || usuario.TokenExpira < DateTime.Now) {
            return null;
        }

        return usuario;
    }

    public async Task LogoutAsync(string token) {
        var usuario = await this.repositorio.ObtenerPorTokenAsync(token);
        if (usuario is null) {
            return;
        }

        usuario.Token = null;
        usuario.TokenExpira = null;
        await this.repositorio.GuardarCambiosAsync();
    }

    private static string GenerarToken() {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}