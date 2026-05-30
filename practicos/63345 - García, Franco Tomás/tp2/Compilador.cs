class Compilador {
    private string texto = "";
    private int indice = 0;
    private Exception Error(string mensaje) {
    return new FormatException($"{mensaje} en posición {indice}");
    }
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
            throw parser.Error("Token inesperado");

        return nodo;
    }

    private Nodo ParseExpresion() {
        var nodo = ParseTermino();

        while (true) {
            SaltarEspacios();

            if (Match('+')) {
                nodo = new NodoSuma(nodo, ParseTermino());
            }
            else if (Match('-')) {
                nodo = new NodoResta(nodo, ParseTermino());
            }
            else {
                break;
            }
        }

        return nodo;
    }
    private Nodo ParseTermino() {
    var nodo = ParseValor();

    while (true) {
        SaltarEspacios();

        if (Match('*')) {
            nodo = new NodoMultiplicacion(nodo, ParseValor());
        }
        else if (Match('/')) {
            nodo = new NodoDivision(nodo, ParseValor());
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

        if (actual == '\0')
        throw Error("Fin inesperado de la expresión");
        
        if (actual == '+' || actual == '-') {
            indice++;
            var valor = ParseValor();
            return new NodoUnario(actual, valor);
        }
        if (actual == '(') {
            indice++;
            var nodo = ParseExpresion();
            SaltarEspacios();

            if (!Match(')'))
                throw new FormatException("Se esperaba ')'");

            return nodo;
        }
        if (char.IsDigit(actual))
            return ParseNumero();
        if (char.ToLower(actual) == 'x') {
            indice++;
            return new NodoVariable();
        }

        throw Error("Token inesperado");
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