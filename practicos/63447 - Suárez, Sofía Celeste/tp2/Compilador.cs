namespace CalculadoraArimetica;
public class Compilador {
    private string _texto;
    private int _posicion;

    public static Nodo Parse(string expresion) {
        if (string.IsNullOrWhiteSpace(expresion)) {
            throw new FormatException("Token inesperado");
        }
        var compilador = new Compilador(expresion);
        Nodo resultado = compilador.ParseExpresion();
        if (compilador._posicion < compilador._texto.Length) {
            throw new FormatException("Token inesperado");
        }
        return resultado;
    }

    private Compilador(string texto) {
        _texto = texto.Replace(" ", ""); 
        _posicion = 0;
    }

    private Nodo ParseExpresion() {
        Nodo izq = ParseTermino();

        while (Actual == '+' || Actual == '-') {
            char op = Actual;
            _posicion++; 
            Nodo der = ParseTermino();
            
            if (op == '+') {
                izq = new SumaNodo(izq, der);
            } 
            else {
                izq = new RestaNodo(izq, der);
            }
        }
        return izq;
    }

    private Nodo ParseTermino() {
        Nodo izq = ParseFactor();

        while (Actual == '*' || Actual == '/') {
            char op = Actual;
            _posicion++; 
            Nodo der = ParseFactor();

            if (op == '*') {
                izq = new MultiplicacionNodo(izq, der);
            } 
            else {
                izq = new DivisionNodo(izq, der);
            }
        }
        return izq;
    }

    private Nodo ParseFactor() {
        if (Actual == '+') { 
            _posicion++; return ParseFactor(); 
        }
        if (Actual == '-') { 
            _posicion++; return new NegativoNodo(ParseFactor()); 
        }

        if (Actual == '(') {
            _posicion++; 
            Nodo nodo = ParseExpresion();
         
            if (Actual != ')') {
                throw new FormatException("Se esperaba ')'");
            }

            _posicion++; 
            return nodo;
        }

        if (Actual == 'x' || Actual == 'X') {
            _posicion++;
            return new VariableNodo();
        }

        if (!char.IsDigit(Actual)) {
            throw new FormatException("Token inesperado");
        }

        return ParseNumero();
    }

    private Nodo ParseNumero() {
        string n = "";
        while (_posicion < _texto.Length && char.IsDigit(_texto[_posicion])) {
            n += _texto[_posicion];
            _posicion++;
        }
        return new NumeroNodo(int.Parse(n));
    }

    private char Actual => _posicion < _texto.Length ? _texto[_posicion] : '\0';
}
