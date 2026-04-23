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
        throw new NotImplementedException("Implementar el parser para convertir la expresión en un AST.");
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
}
