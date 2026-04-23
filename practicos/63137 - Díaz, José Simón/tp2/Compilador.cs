enum TipoToken {
    Numero, Variable,
    Suma, Resta,
    Multiplicacion, Division,
    ParentesisAbierto, ParentesisCerrado,
    Final
}

record Token(TipoToken Tipo, string Valor = "");

class Compilador {
    public static Nodo Parse(string expresion) {
        var tokens = Tokenizar(expresion);
        return Parsear(tokens);
    }

    private static List<Token> Tokenizar(string expresion) {
        int posicion = 0;
        var tokens = new List<Token>();

        while (posicion < expresion.Length) {
            char caracter = expresion[posicion];

            if (char.IsWhiteSpace(caracter)) {
                posicion++;
                continue;
            }

            if (char.IsDigit(caracter)) {
                string numero = "";
                while (posicion < expresion.Length && char.IsDigit(expresion[posicion]))
                    numero += expresion[posicion++];
                tokens.Add(new Token(TipoToken.Numero, numero));
                continue;
            }

            if (char.IsLetter(caracter)) {
                string variable = "";
                while (posicion < expresion.Length && char.IsLetter(expresion[posicion]))
                    variable += expresion[posicion++];
                tokens.Add(new Token(TipoToken.Variable, variable));
                continue;
            }

            Token tokenOperador = caracter switch {
                '+' => new Token(TipoToken.Suma),
                '-' => new Token(TipoToken.Resta),
                '*' => new Token(TipoToken.Multiplicacion),
                '/' => new Token(TipoToken.Division),
                '(' => new Token(TipoToken.ParentesisAbierto),
                ')' => new Token(TipoToken.ParentesisCerrado),
                _   => throw new FormatException($"Token inesperado: '{caracter}'")
            };

            tokens.Add(tokenOperador);
            posicion++;
        }

        tokens.Add(new Token(TipoToken.Final));
        return tokens;
    }

    private static Nodo Parsear(List<Token> tokens) {
        int posicion = 0;
        Token tokenActual = tokens[posicion];

        void consumir() {
            posicion++;
            tokenActual = tokens[posicion];
        }

        Nodo expresion() {
            Nodo izquierdo = termino();

            while (tokenActual.Tipo == TipoToken.Suma || tokenActual.Tipo == TipoToken.Resta) {
                TipoToken operador = tokenActual.Tipo;
                consumir();
                Nodo derecho = termino();

                izquierdo = operador == TipoToken.Suma
                    ? new SumaNodo(izquierdo, derecho)
                    : new RestaNodo(izquierdo, derecho);
            }

            return izquierdo;
        }

        Nodo termino() {
            Nodo izquierdo = factor();

            while (tokenActual.Tipo == TipoToken.Multiplicacion || tokenActual.Tipo == TipoToken.Division) {
                TipoToken operador = tokenActual.Tipo;
                consumir();
                Nodo derecho = factor();

                izquierdo = operador == TipoToken.Multiplicacion
                    ? new MultiplicacionNodo(izquierdo, derecho)
                    : new DivisionNodo(izquierdo, derecho);
            }

            return izquierdo;
        }

        Nodo factor() {
            if (tokenActual.Tipo == TipoToken.Suma) {
                consumir();
                return new PositivoNodo(factor());
            }

            if (tokenActual.Tipo == TipoToken.Resta) {
                consumir();
                return new NegativoNodo(factor());
            }

            if (tokenActual.Tipo == TipoToken.Numero) {
                int valor = int.Parse(tokenActual.Valor);
                consumir();
                return new NumeroNodo(valor);
            }

            if (tokenActual.Tipo == TipoToken.Variable) {
                consumir();
                return new VariableNodo();
            }

            if (tokenActual.Tipo == TipoToken.ParentesisAbierto) {
                consumir();
                Nodo nodoInterno = expresion();

                if (tokenActual.Tipo != TipoToken.ParentesisCerrado)
                    throw new FormatException("Se esperaba ')'");

                consumir();
                return nodoInterno;
            }

            if (tokenActual.Tipo == TipoToken.Final)
                throw new FormatException("Token inesperado: fin de expresión");

            throw new FormatException($"Token inesperado: '{tokenActual.Valor}'");
        }

        Nodo raiz = expresion();

        if (tokenActual.Tipo != TipoToken.Final)
            throw new FormatException($"Token inesperado: '{tokenActual.Valor}'");

        return raiz;
    }
}
