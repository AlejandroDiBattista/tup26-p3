class Compilador
{
    private string texto;
    private int posicion;

    private Compilador(string texto)
    {
        this.texto = texto;
        this.posicion = 0;
    }

    public static Nodo Parse(string expresion)
    {
        var compilador = new Compilador(expresion);
        return compilador.ParsearNumero();
    }

    private char Actual()
    {
        if (posicion >= texto.Length)
            return '\0';

        return texto[posicion];
    }

    private void Avanzar()
    {
        posicion++;
    }

    private Nodo ParsearNumero()
    {
        int inicio = posicion;

        while (char.IsDigit(Actual()))
        {
            Avanzar();
        }

        string numeroTexto = texto.Substring(inicio, posicion - inicio);

        if (numeroTexto.Length == 0)
            throw new Exception("Se esperaba un número");

        int valor = int.Parse(numeroTexto);

        return new NumeroNodo(valor);
    }
}