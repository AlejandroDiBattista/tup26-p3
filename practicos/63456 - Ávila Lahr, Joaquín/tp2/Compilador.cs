class Compilador {
    private string texto = "";
    private int posicion = 0;

    public static Nodo Parse(string entrada) {
        if (string.IsNullOrWhiteSpace(entrada))
            throw new FormatException("Token inesperado");

        var parser = new Compilador {
            texto = entrada,
            posicion = 0
        };

        var nodo = parser.Expresion();

        if (parser.posicion < parser.texto.Length)
            throw new FormatException("Token inesperado");

        return nodo;
    }

    private Nodo Expresion() {
        var nodo = Termino();

        while (true) {
            if (Coincide('+'))
                nodo = new SumaNodo(nodo, Termino());
            else if (Coincide('-'))
                nodo = new RestaNodo(nodo, Termino());
            else
                break;
        }

        return nodo;
    }

    private Nodo Termino() {
        var nodo = Factor();

        while (true) {
            if (Coincide('*'))
                nodo = new MultiplicacionNodo(nodo, Factor());
            else if (Coincide('/'))
                nodo = new DivisionNodo(nodo, Factor());
            else
                break;
        }

        return nodo;
    }

    private Nodo Factor() {
        SaltarEspacios();

        if (Coincide('+')) return Factor();
        if (Coincide('-')) return new NegativoNodo(Factor());

        if (Coincide('(')) {
            var nodo = Expresion();

            if (!Coincide(')'))
                throw new FormatException("Se esperaba ')'");

            return nodo;
        }

        if (char.IsDigit(Ver()))
            return LeerNumero();

        if (Coincide('x') || Coincide('X'))
            return new VariableNodo();

        throw new FormatException("Token inesperado");
    }

    private Nodo LeerNumero() {
        int inicio = posicion;

        while (posicion < texto.Length && char.IsDigit(texto[posicion]))
            posicion++;

        int valor = int.Parse(texto.Substring(inicio, posicion - inicio));
        return new NumeroNodo(valor);
    }

    private void SaltarEspacios() {
        while (posicion < texto.Length && char.IsWhiteSpace(texto[posicion]))
            posicion++;
    }

    private bool Coincide(char caracter) {
        SaltarEspacios();

        if (posicion < texto.Length && texto[posicion] == caracter) {
            posicion++;
            return true;
        }

        return false;
    }
    private char Ver() {
        SaltarEspacios();
        return posicion < texto.Length ? texto[posicion] : '\0';
    }
}
