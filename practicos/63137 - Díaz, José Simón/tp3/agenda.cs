namespace AgendaT;

[Table("Contactos")]
public sealed class Contacto
{
    [Key]
    public int Id { get; set; }
    
    public string Nombre { get; set; } = string.Empty;
    public string Telefonos { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Notas { get; set; } = string.Empty;
    public bool Favorito { get; set; }

    public Contacto Clone()
    {
        return new Contacto
        {
            Id = this.Id,
            Nombre = this.Nombre,
            Telefonos = this.Telefonos,
            Email = this.Email,
            Notas = this.Notas,
            Favorito = this.Favorito
        };
    }
}
