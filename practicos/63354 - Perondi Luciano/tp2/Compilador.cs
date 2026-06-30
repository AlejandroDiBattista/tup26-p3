using System.Text;

enum TipoToken {
    Numero,
    Variable,
    Mas,
    Menos,
    Asterisco,
    Barra,
    ParenAbre,
    ParenCierra,
    Fin
}

record Token(TipoToken Tipo, int Valor = 0);


class Compilador {
    private string texto;
    private int posicion;
    private Token tokenActual;

    private Compilador(string texto) {
        this.texto = texto;
        this.posicion = 0;
        this.tokenActual = LeerToken();
    }

    public static Nodo Parse(string expresion) {
        var compilador = new Compilador(expresion);
        var nodo = compilador.ParseExpresion();
        if (compilador.tokenActual.Tipo != TipoToken.Fin)
            throw new FormatException($"Token inesperado: '{compilador.texto[compilador.posicion - 1]}'");
        return nodo;
    }

    private Token LeerToken() {
        while (posicion < texto.Length && texto[posicion] == ' ') posicion++;

        if (posicion >= texto.Length) return new Token(TipoToken.Fin);

        char c = texto[posicion++];
        return c switch {
            '+' => new Token(TipoToken.Mas),
            '-' => new Token(TipoToken.Menos),
            '*' => new Token(TipoToken.Asterisco),
            '/' => new Token(TipoToken.Barra),
            '(' => new Token(TipoToken.ParenAbre),
            ')' => new Token(TipoToken.ParenCierra),
            'x' or 'X' => new Token(TipoToken.Variable),
            _ when char.IsDigit(c) => LeerNumero(c),
            _ => throw new FormatException($"Token inesperado: '{c}'")
        };
    }

    private Token LeerNumero(char primero) {
        var sb = new StringBuilder();
        sb.Append(primero);
        while (posicion < texto.Length && char.IsDigit(texto[posicion]))
            sb.Append(texto[posicion++]);
        return new Token(TipoToken.Numero, int.Parse(sb.ToString()));
    }

    private Token Consumir() {
        var anterior = tokenActual;
        tokenActual = LeerToken();
        return anterior;
    }

    private Token Consumir(TipoToken tipo, string mensajeError) {
        if (tokenActual.Tipo != tipo)
            throw new FormatException(mensajeError);
        return Consumir();
    }

    private Nodo ParseExpresion() {
        var nodo = ParseTermino();
        while (tokenActual.Tipo == TipoToken.Mas || tokenActual.Tipo == TipoToken.Menos) {
            var esSuma = tokenActual.Tipo == TipoToken.Mas;
            Consumir();
            var derecha = ParseTermino();
            nodo = esSuma ? new SumaNodo(nodo, derecha) : new RestaNodo(nodo, derecha);
        }
        return nodo;
    }

    private Nodo ParseTermino() {
        var nodo = ParseFactor();
        while (tokenActual.Tipo == TipoToken.Asterisco || tokenActual.Tipo == TipoToken.Barra) {
            var esMult = tokenActual.Tipo == TipoToken.Asterisco;
            Consumir();
            var derecha = ParseFactor();
            nodo = esMult ? new MultiplicacionNodo(nodo, derecha) : new DivisionNodo(nodo, derecha);
        }
        return nodo;
    }

    private Nodo ParseFactor() {
        if (tokenActual.Tipo == TipoToken.Menos || tokenActual.Tipo == TipoToken.Mas) {
            var esNegativo = tokenActual.Tipo == TipoToken.Menos;
            Consumir();
            return new NegativoNodo(ParseFactor(), esNegativo);
        }
        if (tokenActual.Tipo == TipoToken.ParenAbre) {
            Consumir();
            var nodo = ParseExpresion();
            Consumir(TipoToken.ParenCierra, "Se esperaba ')'");
            return nodo;
        }
        if (tokenActual.Tipo == TipoToken.Numero)
            return new NumeroNodo(Consumir().Valor);
        if (tokenActual.Tipo == TipoToken.Variable) {
            Consumir();
            return new VariableNodo();
        }
        throw new FormatException(
            tokenActual.Tipo == TipoToken.Fin
                ? "Token inesperado: entrada vacía o incompleta"
                : $"Token inesperado: '{texto[posicion - 1]}'"
        );
    }
}
