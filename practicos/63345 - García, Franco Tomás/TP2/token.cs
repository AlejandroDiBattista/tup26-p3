enum TipoToken
{
    Numero,
    Suma,
    Resta,
    Multiplicacion,
    Division,
    Modulo,
    ParentesisIzquierdo,
    ParentesisDerecho
}

class Token
{
    public TipoToken Tipo;
    public string Contenido;

    public Token(TipoToken tipo, string contenido = "")
    {
        Tipo = tipo;
        Contenido = contenido;
    }
}