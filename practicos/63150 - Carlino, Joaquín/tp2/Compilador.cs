namespace calculadora;

// ─── Compilador: parser de descenso recursivo ────────────────────────────────
//
//  Gramática:
//    Expresion := Termino  { ('+' | '-') Termino  }
//    Termino   := Factor   { ('*' | '/') Factor   }
//    Factor    := '+' Factor
//              | '-' Factor
//              | '(' Expresion ')'
//              | numero
//              | 'x' | 'X'

class Compilador
{
    private string _texto = "";
    private int _pos;

    // ─── API pública ─────────────────────────────────────────────────────────

    public Nodo Compilar(string expresion)
    {
        _texto = expresion.Trim();
        _pos = 0;

        if (_texto.Length == 0)
            throw new ErrorDeParsing("Error: la expresión está vacía.");

        Nodo resultado = ParsearExpresion();

        SkipEspacios();
        if (_pos < _texto.Length)
            throw new ErrorDeParsing($"Error: token inesperado '{_texto[_pos]}' en posición {_pos}.");

        return resultado;
    }

    // ─── Expresion := Termino { ('+' | '-') Termino } ────────────────────────

    private Nodo ParsearExpresion()
    {
        Nodo nodo = ParsearTermino();

        while (true)
        {
            SkipEspacios();
            if (_pos >= _texto.Length) break;

            char op = _texto[_pos];
            if (op != '+' && op != '-') break;

            _pos++;
            Nodo der = ParsearTermino();
            nodo = op == '+' ? new SumaNodo(nodo, der) : new RestaNodo(nodo, der);
        }

        return nodo;
    }

    // ─── Termino := Factor { ('*' | '/') Factor } ───────────────────────────

    private Nodo ParsearTermino()
    {
        Nodo nodo = ParsearFactor();

        while (true)
        {
            SkipEspacios();
            if (_pos >= _texto.Length) break;

            char op = _texto[_pos];
            if (op != '*' && op != '/') break;

            _pos++;
            Nodo der = ParsearFactor();
            nodo = op == '*' ? new MultiplicacionNodo(nodo, der) : new DivisionNodo(nodo, der);
        }

        return nodo;
    }

    // ─── Factor := unario | '(' Expresion ')' | numero | x ──────────────────

    private Nodo ParsearFactor()
    {
        SkipEspacios();

        if (_pos >= _texto.Length)
            throw new ErrorDeParsing("Error: se esperaba un valor pero la expresión terminó.");

        char c = _texto[_pos];

        // Unario positivo (no cambia nada)
        if (c == '+')
        {
            _pos++;
            return ParsearFactor();
        }

        // Unario negativo
        if (c == '-')
        {
            _pos++;
            Nodo operando = ParsearFactor();
            return new NegativoNodo(operando);
        }

        // Paréntesis
        if (c == '(')
        {
            _pos++;
            Nodo interior = ParsearExpresion();
            SkipEspacios();

            if (_pos >= _texto.Length || _texto[_pos] != ')')
                throw new ErrorDeParsing("Error: paréntesis sin cerrar.");

            _pos++;
            return interior;
        }

        // Variable x / X
        if (c == 'x' || c == 'X')
        {
            _pos++;
            return new VariableNodo();
        }

        // Número entero
        if (char.IsDigit(c))
        {
            int inicio = _pos;
            while (_pos < _texto.Length && char.IsDigit(_texto[_pos]))
                _pos++;
            int valor = int.Parse(_texto[inicio.._pos]);
            return new NumeroNodo(valor);
        }

        throw new ErrorDeParsing($"Error: token inesperado '{c}' en posición {_pos}.");
    }

    // ─── Utilidades ──────────────────────────────────────────────────────────

    private void SkipEspacios()
    {
        while (_pos < _texto.Length && _texto[_pos] == ' ')
            _pos++;
    }
}