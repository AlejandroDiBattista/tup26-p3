class Compilador {
    private string expresion = "";
    private int posicion = 0;

    public static Nodo Parse(string expresion) {
        var compilador = new Compilador();
        compilador.expresion = expresion;
        compilador.posicion = 0;

        return compilador.ParseFactor();
    }

    private Nodo ParseFactor() {
        SaltarEspacios();

        if (Coincidir('(')) {
            Nodo nodo = ParseFactor();

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

        throw new FormatException("Token inesperado");
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