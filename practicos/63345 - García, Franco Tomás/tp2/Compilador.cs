class Compilador {
    private string texto = "";
    private int indice = 0;
    public static Nodo Parse(string expresion) {
        if (string.IsNullOrWhiteSpace(expresion))
            throw new FormatException("Token inesperado");

        var parser = new Compilador {
            texto = expresion,
            indice = 0
        };

        var nodo = parser.ParseExpresion();

        parser.SaltarEspacios();

        if (parser.indice < parser.texto.Length)
            throw new FormatException("Token inesperado");

        return nodo;
    }

    private Nodo ParseExpresion() {
        var nodo = ParseValor();

        while (true) {
            SaltarEspacios();

            if (Match('+')) {
                nodo = new NodoSuma(nodo, ParseValor());
            }
            else if (Match('-')) {
                nodo = new NodoResta(nodo, ParseValor());
            }
            else {
                break;
            }
        }

        return nodo;
    }
    private Nodo ParseValor() {
        SaltarEspacios();

        char actual = VerActual();

        if (char.IsDigit(actual))
            return ParseNumero();

        if (char.ToLower(actual) == 'x') {
            indice++;
            return new NodoVariable();
        }
        throw new FormatException("Token inesperado");
    }
    private Nodo ParseNumero() {
        int inicio = indice;

        while (char.IsDigit(VerActual()))
            indice++;

        string numero = texto[inicio..indice];

        return new NodoNumero(int.Parse(numero));
    }

    private char VerActual() {
        return indice < texto.Length ? texto[indice] : '\0';
    }

    private bool Match(char esperado) {
        if (VerActual() == esperado) {
            indice++;
            return true;
        }
        return false;
    }

    private void SaltarEspacios() {
        while (char.IsWhiteSpace(VerActual()))
            indice++;
    }
}