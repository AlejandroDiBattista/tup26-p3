enum TipoToken
{
    Numero,
    OperadorSuma,
    OperadorResta,
    OperadorMultiplicacion,
    OperadorDivision,
    OperadorModulo,
    ParentesisApertura,
    ParentesisCierre
}

class ElementoToken
{
    public TipoToken Tipo;
    public string Contenido;

    public ElementoToken(TipoToken tipo, string contenido = "")
    {
        Tipo = tipo;
        Contenido = contenido;
    }
}
