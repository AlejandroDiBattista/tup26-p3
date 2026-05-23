// agenda.cs - Commit 2 (agregar después del Commit 1)
using Microsoft.Data.Sqlite;
using Dapper;

namespace AgendaTrabajoPracticoTres;

public sealed class RepositorioContactosSqlite : IDisposable
{
    private readonly string _cadenaDeConexionConBaseDatos;
    private SqliteConnection _conexionActiva;
    private bool _recursosDelRepositorioLiberados;

    private const string NOMBRE_TABLA_CONTACTOS = "Contactos";
    private const string INSTRUCCION_CREAR_TABLA = @"
        CREATE TABLE IF NOT EXISTS Contactos (
            Identificador INTEGER PRIMARY KEY AUTOINCREMENT,
            NombreCompleto TEXT NOT NULL,
            ListaDeTelefonos TEXT NOT NULL DEFAULT '',
            CorreoElectronico TEXT NOT NULL DEFAULT '',
            NotasAdicionales TEXT NOT NULL DEFAULT '',
            EsFavorito INTEGER NOT NULL DEFAULT 0
        );
        
        CREATE INDEX IF NOT EXISTS idx_contactos_nombre ON Contactos(NombreCompleto);
        CREATE INDEX IF NOT EXISTS idx_contactos_email ON Contactos(CorreoElectronico);
    ";

    public RepositorioContactosSqlite(string rutaDelArchivoBaseDatos)
    {
        _cadenaDeConexionConBaseDatos = $"Data Source={rutaDelArchivoBaseDatos}";
        AsegurarQueLaBaseDatosEsteInicializada();
    }

    private void AsegurarQueLaBaseDatosEsteInicializada()
    {
        using SqliteConnection conexionTemporal = new SqliteConnection(_cadenaDeConexionConBaseDatos);
        conexionTemporal.Open();
        conexionTemporal.Execute(INSTRUCCION_CREAR_TABLA);
    }

    private SqliteConnection ObtenerConexionAbierta()
    {
        bool recursosDelRepositorioEstanLiberados = _recursosDelRepositorioLiberados;
        
        if (recursosDelRepositorioEstanLiberados)
        {
            throw new ObjectDisposedException(nameof(RepositorioContactosSqlite));
        }
        
        SqliteConnection nuevaConexion = new SqliteConnection(_cadenaDeConexionConBaseDatos);
        nuevaConexion.Open();
        return nuevaConexion;
    }

    public async Task<List<Contacto>> ObtenerTodosLosContactosAsync()
    {
        using SqliteConnection conexion = ObtenerConexionAbierta();
        IEnumerable<Contacto> contactosObtenidos = await conexion.GetAllAsync<Contacto>();
        return contactosObtenidos.ToList();
    }

    public async Task<int> InsertarContactoAsync(Contacto contacto)
    {
        using SqliteConnection conexion = ObtenerConexionAbierta();
        long identificadorGenerado = await conexion.InsertAsync(contacto);
        return (int)identificadorGenerado;
    }

    public async Task<bool> ActualizarContactoAsync(Contacto contacto)
    {
        using SqliteConnection conexion = ObtenerConexionAbierta();
        bool actualizacionExitosa = await conexion.UpdateAsync(contacto);
        return actualizacionExitosa;
    }

    public async Task<bool> EliminarContactoPorIdentificadorAsync(int identificador)
    {
        using SqliteConnection conexion = ObtenerConexionAbierta();
        Contacto contactoParaEliminar = new Contacto { Identificador = identificador };
        bool eliminacionExitosa = await conexion.DeleteAsync(contactoParaEliminar);
        return eliminacionExitosa;
    }

    public void Dispose()
    {
        bool recursosAunNoLiberados = !_recursosDelRepositorioLiberados;
        
        if (recursosAunNoLiberados)
        {
            _conexionActiva?.Dispose();
            _recursosDelRepositorioLiberados = true;
        }
        
        GC.SuppressFinalize(this);
    }
}