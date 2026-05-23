using System.ComponentModel.DataAnnotations.Schema;
using Dapper.Contrib.Extensions;

namespace AgendaTrabajoPracticoTres;

[Table("Contactos")]
public sealed class Contacto
{
    [Key]
    public int Identificador { get; set; }
    
    public string NombreCompleto { get; set; } = string.Empty;
    public string ListaDeTelefonos { get; set; } = string.Empty;
    public string CorreoElectronico { get; set; } = string.Empty;
    public string NotasAdicionales { get; set; } = string.Empty;
    public bool EsFavorito { get; set; }

    public Contacto ObtenerCopia()
    {
        return new Contacto
        {
            Identificador = this.Identificador,
            NombreCompleto = this.NombreCompleto,
            ListaDeTelefonos = this.ListaDeTelefonos,
            CorreoElectronico = this.CorreoElectronico,
            NotasAdicionales = this.NotasAdicionales,
            EsFavorito = this.EsFavorito
        };
    }
}