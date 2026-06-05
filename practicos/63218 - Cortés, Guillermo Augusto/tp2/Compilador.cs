class Compilador {
    private readonly string texto;
    private int posicion;

    private Compilador(string texto)
    {
        this.texto = texto;
        posicion = 0;
    }

    public static Nodo Parse(string expresion) {
        if (string.IsNullOrWhiteSpace(expresion))
        {
            throw new FormatException("Token inesperado");
        }

        var compilador = new Compilador(expresion);

        return compilador.ParsearExpresion();
    }

    private Nodo ParsearExpresion()
    {
        throw new NotImplementedException();
    }
}
