class Compilador
{ 
     private string texto;
     private int pos;

    private Compilador(string texto)
    {
        this.texto = texto.Replace(" ", "");
        pos = 0;
    }
    public static Nodo Parse(string expresion)
    {
        var comp = new Compilador(expresion);
        return comp.ParseTermino();
    }
    private char Actual()
    {
        return pos < texto.Length ? texto[pos] : '\0';
    }

    private void Avanzar()
    {
        pos++;
    }

    private Nodo ParseTermino()
    {
        Nodo nodo = ParseFactor();

        while (Actual() == '*' || Actual() == '/')
        {
            char op = Actual();
            Avanzar();

            Nodo derecho = ParseFactor();

            if (op == '*')
                nodo = new MultiplicacionNodo(nodo, derecho);
            else
                nodo = new DivisionNodo(nodo, derecho);
        }

        return nodo;
    }


    private Nodo ParseFactor()
    {
        char c = Actual();

        if (c == '+')
        {
            Avanzar();
            return ParseFactor();
        }
        if (c == '-')
        {
            Avanzar();
            return new NegativoNodo(ParseFactor());
        }
        if (c == '(')
        {
            Avanzar();
            var nodo = ParseTermino();
            Avanzar(); 
            return nodo;
        }
        if (char.IsDigit(c))
        {
            int inicio = pos;

            while (char.IsDigit(Actual()))
                Avanzar();

            int valor = int.Parse(texto.Substring(inicio, pos - inicio));
            return new NumeroNodo(valor);
        }
        if (c == 'x' || c == 'X')
        {
            Avanzar();
            return new VariableNodo();
        }
        throw new FormatException("Token inesperado");
    }
}
