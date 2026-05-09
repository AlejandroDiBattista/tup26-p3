using System.Runtime.CompilerServices;
class Compilador {
    enum TipoToken {
        Numero, Variable,
        Suma, Resta,
        Multiplicacion, Division,
        ParentesisAbierto, ParentesisCerrado,
        Final
    }
    record Token(TipoToken Tipo, string Valor = "");
    private List<Token> _tokens = new();
    private int _posicion;
    private sealed class TokenCursor {
    private readonly Token _token;

    public TokenCursor(Token token) => _token = token;
    public static implicit operator string(TokenCursor token) => token.ToString();
    public static bool operator ==(TokenCursor token, char c) => token.ToString() == c.ToString();
    public static bool operator !=(TokenCursor token, char c) => !(token == c); 
    public override string ToString() {
        return _token.Tipo switch {
            TipoToken.Numero => _token.Valor,
            TipoToken.Variable => _token.Valor,
            TipoToken.Suma => "+",
            TipoToken.Resta => "-",
            TipoToken.Multiplicacion => "*",
            TipoToken.Division => "/",
            TipoToken.ParentesisAbierto => "(",
            TipoToken.ParentesisCerrado => ")",
            TipoToken.Final => "",
            _ => _token.Valor
        };
    }
    public override bool Equals(object? obj) {
        return obj is TokenCursor other && ToString() == other.ToString();
    }
    public override int GetHashCode() {
        return ToString().GetHashCode();
    }
}
private TokenCursor token => new(_tokens[_posicion]);
private bool EsNumero(string texto) => int.TryParse(texto, out _);
    private Token Actual => _tokens[_posicion];
    private Token Consumir => _tokens[_posicion++];
    private void AvanzarToken() => _posicion++;
    private bool Coincide(TipoToken tipo)
    {
        if (Actual.Tipo != tipo) {
            return false;
        }
        AvanzarToken();
        return true;
    }
    public static Nodo Parse(string expresion) {
        var compilador = new Compilador();
        compilador._tokens = compilador.Tokenizar(expresion);
        compilador._posicion = 0;
        var nodo = compilador.ParseExpresion();
        if (!compilador.Coincide(TipoToken.Final)) {
            throw new FormatException("Se esperaba el final de la expresión.");
        }
        return nodo;
    }
    List<Token> Tokenizar(string expresion) {
        int posicion = 0;
        char continuar() => expresion[posicion++];

        List<Token> token = new();

        while (posicion < expresion.Length) {
            char c = expresion[posicion];
            if (char.IsWhiteSpace(c)) {
             continuar();
                continue;
            }
           if (char.IsDigit(c)) {
                string numero = "";
                while (posicion < expresion.Length && char.IsDigit(expresion[posicion])) {
                    numero += continuar();
                }
                token.Add(new Token(TipoToken.Numero, numero));
                continue;
            }
            if (c == 'x' || c == 'X') {
                token.Add(new Token(TipoToken.Variable, c.ToString()));
                continuar();
                continue;
            }
            switch (c) {
                case '+':
                    token.Add(new Token(TipoToken.Suma));
                    break;
                case '-':
                    token.Add(new Token(TipoToken.Resta));
                    break;
                case '*':
                    token.Add(new Token(TipoToken.Multiplicacion));
                    break;
                case '/':
                    token.Add(new Token(TipoToken.Division));
                    break;
                case '(':
                    token.Add(new Token(TipoToken.ParentesisAbierto));
                    break;
                case ')':
                    token.Add(new Token(TipoToken.ParentesisCerrado));
                    break;
                default:
                    throw new FormatException($"Token inesperado: {c}");
            }
            continuar();
        }
        token.Add(new Token(TipoToken.Final));
        return token;
    }
    Nodo ParseTermino() {
        var nodo = ParseFactor();
        while (token == '*' || token == '/') {
            string operador = token;
            AvanzarToken();
            var terminoDerecho = ParseFactor();
            nodo = new NodoOperacion( operador, nodo, terminoDerecho);
        }
        return nodo;    
    }
    Nodo ParseExpresion() {
        var nodo = ParseTermino();
        while (token == '+' || token == '-') {
            string operador = token;
            AvanzarToken();
            var terminoDerecho = ParseTermino();
            nodo = new NodoOperacion( nodo, operador, terminoDerecho);
        }
        return nodo;    
    }
    Nodo ParseFactor(){

        if (token == '+') {
            AvanzarToken();
            return ParseFactor();
        }
        if (token == '-') {
            AvanzarToken();
            return new NodoOperacion(new NodoValor(0), "-", ParseFactor());
        }
        if (token == '(') {
            AvanzarToken();
            var nodo = ParseExpresion();
            if (token == ')') {
                AvanzarToken();
                return nodo;
            } else 
                throw new FormatException("Se esperaba ')'");
        }
        if (EsNumero(token)) {
            var valor = int.Parse(token);
            AvanzarToken();
            return new NodoValor(valor);
        }
        if (token == 'x' || token == 'X') {
            AvanzarToken();
            return new NodoVariable();
        }
        throw new FormatException("Token inesperado: " + token);
    }
}
/*
Expresion := Termino { ('+' | '-') Termino }
Termino   := Factor  { ('*' | '/') Factor }
Factor    := '+' Factor
          | '-' Factor
          | '(' Expresion ')'
          | numero
          | x

*/
