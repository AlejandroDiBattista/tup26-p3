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

    while(posicion < texto.Length &&
        (texto[posicion]=='+' || texto[posicion]=='-'))
    {
        char op = texto[posicion++];
        Nodo derecha = Termino();

        if(op=='+')
            izquierda = new SumaNodo(izquierda,derecha);
        else
            izquierda = new RestaNodo(izquierda,derecha);
        }

    return izquierda;
}
private Nodo Termino()
{
    Nodo izquierda = Factor();

    while(posicion < texto.Length &&
         (texto[posicion]=='*' || texto[posicion]=='/'))
    {
        char op = texto[posicion++];
        Nodo derecha = Factor();

        if(op=='*')
            izquierda = new MultiplicacionNodo(izquierda,derecha);
        else
            izquierda = new DivisionNodo(izquierda,derecha);
    }

    return izquierda;
}