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
private Nodo Expresion()
{
    Nodo izquierda = Termino();

    while (posicion < texto.Length &&
          (texto[posicion] == '+' || texto[posicion] == '-'))
    {
        char op = texto[posicion++];
        Nodo derecha = Termino();

        if (op == '+')
            izquierda = new NoSuma(izquierda, derecha);
        else
            izquierda = new NoResta(izquierda, derecha);
    }

    return izquierda;
}
