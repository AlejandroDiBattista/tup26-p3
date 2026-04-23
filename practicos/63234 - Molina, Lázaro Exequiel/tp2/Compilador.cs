class Compilador
{
    private string texto;
    private int posicion;

    private Compilador(string expresion)
    {
        texto = expresion.Replace(" ", "");
        posicion = 0;
    }
public static Nodo Parse(string expresion)
{
    var parser = new Compilador(expresion);

    Nodo resultado = parser.Expresion();

    if (parser.posicion != parser.texto.Length)
        throw new FormatException("Token inesperado");

    return resultado;
}
