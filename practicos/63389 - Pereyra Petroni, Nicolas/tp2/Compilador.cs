class Compilador {
    public static Nodo Parse(string expresion) {
        var parser = new Parser(expresion);
        return parser.Parsear();
    }
}

class Parser {
    private string texto;
    private int pos;

    public Parser(string texto) {
        this.texto = texto.Replace(" ", "");
        this.pos = 0;
    }

    public Nodo Parsear() {
        Nodo nodo = ParseExpresion();

        if (pos < texto.Length)
            throw new FormatException("Token inesperado");

        return nodo;
    }

    private Nodo ParseExpresion() {
        Nodo nodo = ParseTermino();

        while (true) {
            if (Match('+')) {
                nodo = new SumaNodo(nodo, ParseTermino());
            } else if (Match('-')) {
                nodo = new RestaNodo(nodo, ParseTermino());
            } else {
                break;
            }
        }

        return nodo;
    }

    private Nodo ParseTermino() {
        Nodo nodo = ParseFactor();

        while (true) {
            if (Match('*')) {
                nodo = new MultiplicacionNodo(nodo, ParseFactor());
            } else if (Match('/')) {
                nodo = new DivisionNodo(nodo, ParseFactor());
            } else {
                break;
            }
        }

        return nodo;
    }

    private Nodo ParseFactor() {
        if (Match('+'))
            return ParseFactor();

        if (Match('-'))
            return new NegativoNodo(ParseFactor());

        if (Match('(')) {
            Nodo nodo = ParseExpresion();

            if (!Match(')'))
                throw new FormatException("Se esperaba ')'");

            return nodo;
        }

        if (pos < texto.Length && (texto[pos] == 'x' || texto[pos] == 'X')) {
            pos++;
            return new VariableNodo();
        }

        return ParseNumero();
    }

    private Nodo ParseNumero() {
        string numero = "";

        while (pos < texto.Length && char.IsDigit(texto[pos])) {
            numero += texto[pos];
            pos++;
        }

        if (numero == "")
            throw new FormatException("Token inesperado");

        return new NumeroNodo(int.Parse(numero));
    }

    private bool Match(char c) {
        if (pos < texto.Length && texto[pos] == c) {
            pos++;
            return true;
        }
        return false;
    }
}
