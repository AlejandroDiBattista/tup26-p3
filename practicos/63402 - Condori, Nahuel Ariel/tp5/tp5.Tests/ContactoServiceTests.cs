using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using tp5.Data;
using tp5.Models;
using tp5.Services;
using Xunit;

namespace tp5.Tests;

/// <summary>
/// Verifica el contrato del servicio contra SQLite real en archivos temporales.
/// Cada prueba usa su propia base para mantenerse aislada de contactos.db.
/// </summary>
public sealed class ContactoServiceTests
{
    [Fact]
    public async Task CrudCompletoPersisteLosCambios()
    {
        await using var database = await TestDatabase.CreateAsync();
        var contacto = ContactoValido();

        await database.Service.AddContactoAsync(contacto);
        Assert.True(contacto.Id > 0);

        contacto.Cargo = "  Analista funcional  ";
        await database.Service.UpdateContactoAsync(contacto);

        var guardado = Assert.Single(await database.Service.GetContactosAsync());
        Assert.Equal("Analista funcional", guardado.Cargo);

        await database.Service.DeleteContactoAsync(contacto.Id);
        Assert.Empty(await database.Service.GetContactosAsync());
    }

    [Fact]
    public async Task BusquedaFiltraYOrdenaPorApellidoYNombre()
    {
        await using var database = await TestDatabase.CreateAsync();
        var zeta = ContactoValido(nombre: "Zoe", apellido: "Álvarez", empresa: "Norte");
        var ana = ContactoValido(nombre: "Ana", apellido: "Álvarez", empresa: "Sur");
        var otro = ContactoValido(nombre: "Bruno", apellido: "Benítez", empresa: "Norte");

        await database.Service.AddContactoAsync(zeta);
        await database.Service.AddContactoAsync(ana);
        await database.Service.AddContactoAsync(otro);

        var resultado = await database.Service.GetContactosAsync("Álvarez");

        Assert.Equal(["Ana", "Zoe"], resultado.Select(c => c.Nombre));
    }

    [Fact]
    public async Task AltaNormalizaCamposYRechazaDatosInvalidos()
    {
        await using var database = await TestDatabase.CreateAsync();
        var contacto = ContactoValido(nombre: "  Ana  ", apellido: "  Pérez  ");

        await database.Service.AddContactoAsync(contacto);

        Assert.Equal("Ana", contacto.Nombre);
        Assert.Equal("Pérez", contacto.Apellido);

        var invalido = ContactoValido(nombre: "   ");
        await Assert.ThrowsAsync<ValidationException>(
            () => database.Service.AddContactoAsync(invalido));
    }

    [Fact]
    public async Task ModificacionInexistenteInformaElError()
    {
        await using var database = await TestDatabase.CreateAsync();
        var contacto = ContactoValido();
        contacto.Id = 999;

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => database.Service.UpdateContactoAsync(contacto));
    }

    private static Contacto ContactoValido(
        string nombre = "Ada",
        string apellido = "Lovelace",
        string empresa = "Analytical Engines") => new()
    {
        Nombre = nombre,
        Apellido = apellido,
        Telefono = "381-555-0100",
        Email = "ada@example.com",
        Empresa = empresa
    };

    /// <summary>
    /// Encapsula el ciclo de vida del archivo y la creación de contextos para
    /// reproducir el mismo patrón IDbContextFactory usado por la aplicación.
    /// </summary>
    private sealed class TestDatabase : IAsyncDisposable, IDbContextFactory<AgendaContext>
    {
        private readonly string databasePath;
        private readonly DbContextOptions<AgendaContext> options;

        private TestDatabase(string databasePath)
        {
            this.databasePath = databasePath;
            options = new DbContextOptionsBuilder<AgendaContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;
            Service = new ContactoService(this);
        }

        public ContactoService Service { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var path = Path.Combine(Path.GetTempPath(), $"agenda-{Guid.NewGuid():N}.db");
            var database = new TestDatabase(path);

            await using var context = database.CreateDbContext();
            await context.Database.EnsureCreatedAsync();

            return database;
        }

        public AgendaContext CreateDbContext() => new(options);

        public Task<AgendaContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());

        public ValueTask DisposeAsync()
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }

            return ValueTask.CompletedTask;
        }
    }
}
