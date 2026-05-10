class Compilador {
    private string expresion = "";
    private int posicion = 0;

    public static Nodo Parse(string expresion) {
        var compilador = new Compilador();
        compilador.expresion = expresion;
        compilador.posicion = 0;

        var nodo = compilador.ParseExpresion();
        compilador.SaltarEspacios();

        if (!compilador.Fin()) {
            throw new FormatException($"Token inesperado: '{compilador.Actual()}'");
        }

        return nodo;
    }

    private Nodo ParseExpresion() {
        Nodo izquierda = ParseTermino();

        while (true) {
            SaltarEspacios();

            if (Coincidir('+')) {
                Nodo derecha = ParseTermino();
                izquierda = new NodoBinario(izquierda, '+', derecha);
            } else if (Coincidir('-')) {
                Nodo derecha = ParseTermino();
                izquierda = new NodoBinario(izquierda, '-', derecha);
            } else {
                break;
            }
        }

        return izquierda;
    }

    private Nodo ParseTermino() {
        Nodo izquierda = ParseFactor();

        while (true) {
            SaltarEspacios();

            if (Coincidir('*')) {
                Nodo derecha = ParseFactor();
                izquierda = new NodoBinario(izquierda, '*', derecha);
            } else if (Coincidir('/')) {
                Nodo derecha = ParseFactor();
                izquierda = new NodoBinario(izquierda, '/', derecha);
            } else {
                break;
            }
        }

        return izquierda;
    }

private Nodo ParseFactor() {
    SaltarEspacios();

    if (Coincidir('+')) {
        return new NodoUnario('+', ParseFactor());
    }

    if (Coincidir('-')) {
        return new NodoUnario('-', ParseFactor());
    }

    if (Coincidir('(')) {
        Nodo nodo = ParseExpresion();

        if (!Coincidir(')')) {
            throw new FormatException("Se esperaba ')'");
        }

        return nodo;
    }

    if (!Fin() && char.IsDigit(Actual())) {
        return ParseNumero();
    }

    if (!Fin() && (Actual() == 'x' || Actual() == 'X')) {
        posicion++;
        return new NodoVariable();
    }

    throw new FormatException(Fin() ? "Token inesperado" : $"Token inesperado: '{Actual()}'");
}

    private Nodo ParseNumero() {
        SaltarEspacios();

        int inicio = posicion;

        while (!Fin() && char.IsDigit(Actual())) {
            posicion++;
        }

        string texto = expresion.Substring(inicio, posicion - inicio);
        return new NodoNumero(int.Parse(texto));
    }

    private bool Coincidir(char esperado) {
        SaltarEspacios();

        if (!Fin() && Actual() == esperado) {
            posicion++;
            return true;
        }

        return false;
    }

    private void SaltarEspacios() {
        while (!Fin() && char.IsWhiteSpace(Actual())) {
            posicion++;
        }
    }

    private bool Fin() {
        return posicion >= expresion.Length;
    }

    private char Actual() {
        return expresion[posicion];
    }
}