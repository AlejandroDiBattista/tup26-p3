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
        var nodo = ParsearTermino();

        while (true)
        {
            if (Coincide('+'))
            {
                nodo = new SumaNodo(nodo, ParsearTermino());
            }
            else if (Coincide('-'))
            {
                nodo = new RestaNodo(nodo, ParsearTermino());
            }
            else
            {
                break;
            }
        }
        return nodo;
    }

    private Nodo ParsearTermino()
    {
        var nodo = ParsearFactor();

        while (true)
        {
            if (Coincide('*'))
            {
                nodo = new MultiplicacionNodo(nodo, ParsearFactor());
            }
            else if (Coincide('/'))
            {
                nodo = new DivisionNodo(nodo, ParsearFactor());
            }
            else
            {
                break;
            }
        }
        return nodo;
    }

    private Nodo ParsearFactor()
    {
        SaltarEspacios();
         if (Coincide('+'))
        {
            return new PositivoNodo(ParsearFactor());
        }

         if (Coincide('-'))
        {
            return new NegativoNodo(ParsearFactor());
        }

        if (Coincide('('))
        {
            var expresion = ParsearExpresion();

            if (!Coincide(')'))
            {
                throw new FormatException("Se esperaba ')'");
            }

            return expresion;
        }

        if (ActualEsNumero()) 
        {
            int inicio = posicion;

            while (ActualEsNumero()) 
            {
                posicion++;
            }

            return new NumeroNodo(int.Parse(texto[inicio..posicion]));
        }

        if (ActualEsVariable()) 
        {
            posicion++;
            return new VariableNodo();
        }

        throw new FormatException("Token inesperado");
    }

    private void SaltarEspacios() 
    {
        while (posicion < texto.Length && char.IsWhiteSpace(texto[posicion])) 
        {
            posicion++;
        }
    }

    private bool Coincide(char caracter) 
    {
        SaltarEspacios();

        if (posicion < texto.Length && texto[posicion] == caracter) 
        {
            posicion++;
            return true;
        }

        return false;
    }

    private bool ActualEsNumero() 
    {
        return posicion < texto.Length && char.IsDigit(texto[posicion]);
    }

    private bool ActualEsVariable() 
    {
        return posicion < texto.Length && (texto[posicion] == 'x' || texto[posicion] == 'X');        
    }
}
