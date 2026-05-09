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
private Nodo Factor()
{
    if(posicion >= texto.Length)
        throw new FormatException("Token inesperado");

    if(texto[posicion]=='+')
    {
        posicion++;
        return Factor();
    }

    if(texto[posicion]=='-')
    {
        posicion++;
        return new NegativoNodo(Factor());
    }

    if(texto[posicion]=='(')
    {
        posicion++;

        Nodo nodo = Expresion();

        if(posicion >= texto.Length || texto[posicion]!=')')
            throw new FormatException("Se esperaba ')'");

        posicion++;
        return nodo;
    }
        if(char.ToLower(texto[posicion])=='x')
    {
        posicion++;
        return new VariableNodo();
    }

    if(char.IsDigit(texto[posicion]))
    {
        int inicio = posicion;

        while(posicion < texto.Length &&
             char.IsDigit(texto[posicion]))
        {
            posicion++;
        }

        int valor = int.Parse(
            texto.Substring(inicio,posicion-inicio)
        );

        return new NumeroNodo(valor);
    }
    
    throw new FormatException("Token inesperado");
}
}