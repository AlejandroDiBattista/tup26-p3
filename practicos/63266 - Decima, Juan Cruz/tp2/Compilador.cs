class Compilador
{

    private readonly string _expresion;
    private int _pos;


    public static Nodo Parse(string expresion)
    {
        var compilador = new Compilador(expresion);
        var nodo = compilador.ParseExpresion();

        compilador.SaltarEspacios();
        if (compilador._pos < compilador._expresion.Length)
        {
            throw new FormatException(
                $"Token inesperado: '{compilador._expresion[compilador._pos]}'");
        }

        return nodo;
    }
}
