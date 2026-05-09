namespace TP2_GONZALO_CALCULADORA;

public enum TipoToken
{
    Numero, Variable, Suma, Resta, Multiplicacion, Division,
    ParentesisIzquierdo, ParentesisDerecho, Fin
}

public class Token
{
    public TipoToken Tipo { get; }
    public string Valor { get; }

    public Token(TipoToken tipo, string valor = "")
    {
        Tipo = tipo;
        Valor = valor;
    }
}

// logica del compilador 

public static class Compilador
{
    private static List<Token> _tokens = new();
    private static int _posicion;
    public static Nodo Parse(string expresion)
    {
        _tokens = Tokenizar(expresion);
        _posicion = 0;
        
        Nodo resultado = ParsearExpresion();

        if (TokenActual().Tipo != TipoToken.Fin)
        {
            throw new FormatException($"Token inesperado: {TokenActual().Valor}");
        }
        return resultado;
    }
    
    private static Nodo ParsearExpresion()
    {
        Nodo nodoIzquierdo = ParsearTermino();

        while (TokenActual().Tipo == TipoToken.Suma || TokenActual().Tipo == TipoToken.Resta)
        {
            Token operador = TokenActual();
            Avanzar(); 
            
            Nodo nodoDerecho = ParsearTermino();
            
            if (operador.Tipo == TipoToken.Suma)
                nodoIzquierdo = new SumaNodo(nodoIzquierdo, nodoDerecho);
            else
                nodoIzquierdo = new RestaNodo(nodoIzquierdo, nodoDerecho);
        }

        return nodoIzquierdo;
    }

private static Nodo ParsearTermino()
    {
        Nodo nodoIzquierdo = ParsearFactor();

        while (TokenActual().Tipo == TipoToken.Multiplicacion || TokenActual().Tipo == TipoToken.Division)
        {
            Token operador = TokenActual();
            Avanzar();
            Nodo nodoDerecho = ParsearFactor();

            if (operador.Tipo == TipoToken.Multiplicacion)
                nodoIzquierdo = new MultiplicacionNodo(nodoIzquierdo, nodoDerecho);
            else
                nodoIzquierdo = new DivisionNodo(nodoIzquierdo, nodoDerecho);
        }
        return nodoIzquierdo;
    }

    private static Nodo ParsearFactor()
    {
        Token actual = TokenActual();

        if (actual.Tipo == TipoToken.Suma) 
        { 
            Avanzar(); 
            return ParsearFactor(); 
        }
        if (actual.Tipo == TipoToken.Resta) 
        { 
            Avanzar(); 
            return new NegativoNodo(ParsearFactor()); 
        }

        if (actual.Tipo == TipoToken.ParentesisIzquierdo)
        {
            Avanzar();
            Nodo nodoInterno = ParsearExpresion();
            
            if (TokenActual().Tipo != TipoToken.ParentesisDerecho)
                throw new FormatException("Se esperaba ')' o hay un paréntesis sin cerrar.");
                
            Avanzar();
            return nodoInterno;
        }

        if (actual.Tipo == TipoToken.Numero)
        {
            Avanzar();
            return new NumeroNodo(int.Parse(actual.Valor));
        }

        if (actual.Tipo == TipoToken.Variable)
        {
            Avanzar();
            return new VariableNodo();
        }

        throw new FormatException($"Token inesperado: {actual.Valor}");
    }

    // Funciones auxiliares

    private static Token TokenActual() => _tokens[_posicion];

    private static void Avanzar()
    {
        if (_posicion < _tokens.Count - 1)
            _posicion++;
    }
    // tokens
    public static List<Token> Tokenizar(string expresion)
    {
        var tokens = new List<Token>();
        int i = 0;
        while (i < expresion.Length)
        {
            char c = expresion[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }
            if (char.IsDigit(c))
            {
                string num = "";
                while (i < expresion.Length && char.IsDigit(expresion[i])) num += expresion[i++];
                tokens.Add(new Token(TipoToken.Numero, num));
                continue;
            }
            if (char.ToLower(c) == 'x') { tokens.Add(new Token(TipoToken.Variable, "x")); i++; continue; }

            switch (c)
            {
                case '+': tokens.Add(new Token(TipoToken.Suma, "+")); break;
                case '-': tokens.Add(new Token(TipoToken.Resta, "-")); break;
                case '*': tokens.Add(new Token(TipoToken.Multiplicacion, "*")); break;
                case '/': tokens.Add(new Token(TipoToken.Division, "/")); break;
                case '(': tokens.Add(new Token(TipoToken.ParentesisIzquierdo, "(")); break;
                case ')': tokens.Add(new Token(TipoToken.ParentesisDerecho, ")")); break;
                default: throw new FormatException($"Token inesperado: {c}");
            }
            i++;
        }
        tokens.Add(new Token(TipoToken.Fin));
        return tokens;
    }
}