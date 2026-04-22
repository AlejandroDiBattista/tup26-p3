class Compilador {
    private List<string> tokens;
    private int posicion;

    private Compilador(string expresion) {
        tokens = Tokenizar(expresion);
        posicion = 0;
    }

    public static Nodo Parse(string expresion) {
        if (string.IsNullOrWhiteSpace(expresion))
            throw new FormatException("Token inesperado: entrada vacía");

        var compilador = new Compilador(expresion);
        var nodo = compilador.ParseExpresion();

        if (compilador.posicion < compilador.tokens.Count)
            throw new FormatException($"Token inesperado: '{compilador.tokens[compilador.posicion]}'");

        return nodo;
    }

    private static List<string> Tokenizar(string expresion) {
        var tokens = new List<string>();
        int i = 0;
        while (i < expresion.Length) {
            if (char.IsWhiteSpace(expresion[i])) { i++; continue; }
            if (char.IsDigit(expresion[i])) {
                int j = i;
                while (j < expresion.Length && char.IsDigit(expresion[j])) j++;
                tokens.Add(expresion.Substring(i, j - i));
                i = j; continue;
            }
            if (char.IsLetter(expresion[i])) {
                if (char.ToLower(expresion[i]) == 'x') { tokens.Add("x"); i++; continue; }
                throw new FormatException($"Token inesperado: '{expresion[i]}'");
            }
            if ("+-*/()".Contains(expresion[i])) { tokens.Add(expresion[i].ToString()); i++; continue; }
            throw new FormatException($"Token inesperado: '{expresion[i]}'");
        }
        return tokens;
    }

    private string? TokenActual => posicion < tokens.Count ? tokens[posicion] : null;

    private string Consumir() {
        if (posicion >= tokens.Count)
            throw new FormatException("Token inesperado: se esperaba más entrada");
        return tokens[posicion++];
    }

    private Nodo ParseExpresion() {
        var izquierdo = ParseTermino();
        while (TokenActual == "+" || TokenActual == "-") {
            var op = Consumir();
            var derecho = ParseTermino();
            izquierdo = op == "+" ? new SumaNodo(izquierdo, derecho) : new RestaNodo(izquierdo, derecho);
        }
        return izquierdo;
    }

    private Nodo ParseTermino() {
        var izquierdo = ParseFactor();
        while (TokenActual == "*" || TokenActual == "/") {
            var op = Consumir();
            var derecho = ParseFactor();
            izquierdo = op == "*" ? new MultiplicacionNodo(izquierdo, derecho) : new DivisionNodo(izquierdo, derecho);
        }
        return izquierdo;
    }

    private Nodo ParseFactor() {
        if (TokenActual == "+") { Consumir(); return ParseFactor(); }
        if (TokenActual == "-") { Consumir(); return new NegativoNodo(ParseFactor()); }
        if (TokenActual == "(") {
            Consumir();
            var nodo = ParseExpresion();
            if (TokenActual != ")") throw new FormatException("Se esperaba ')'");
            Consumir();
            return nodo;
        }
        if (TokenActual != null && int.TryParse(TokenActual, out int valor)) { Consumir(); return new NumeroNodo(valor); }
        if (TokenActual == "x") { Consumir(); return new VariableNodo(); }
        throw new FormatException($"Token inesperado: '{TokenActual ?? "fin de entrada"}'");
    }
}

