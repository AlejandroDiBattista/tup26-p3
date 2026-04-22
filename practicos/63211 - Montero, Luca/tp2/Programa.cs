using System;

public abstract class Nodo
{
    public abstract int Evaluar(int x);
}

public class NumeroNodo : Nodo
{
    public int Valor { get; }
    public NumeroNodo(int valor) => Valor = valor;
    public override int Evaluar(int x) => Valor;
}

public class VariableNodo : Nodo
{
    public override int Evaluar(int x) => x;
}

public class PositivoNodo : Nodo
{
    public Nodo Operando { get; }
    public PositivoNodo(Nodo operando) => Operando = operando;
    public override int Evaluar(int x) => Operando.Evaluar(x);
}

public class NegativoNodo : Nodo
{
    public Nodo Operando { get; }
    public NegativoNodo(Nodo operando) => Operando = operando;
    public override int Evaluar(int x) => -Operando.Evaluar(x);
}

public abstract class NodoBinario : Nodo
{
    public Nodo Izquierdo { get; }
    public Nodo Derecho { get; }

    protected NodoBinario(Nodo izquierdo, Nodo derecho)
    {
        Izquierdo = izquierdo;
        Derecho = derecho;
    }
}

public class SumaNodo : NodoBinario
{
    public SumaNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x) => Izquierdo.Evaluar(x) + Derecho.Evaluar(x);
}

public class RestaNodo : NodoBinario
{
    public RestaNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x) => Izquierdo.Evaluar(x) - Derecho.Evaluar(x);
}

public class MultiplicacionNodo : NodoBinario
{
    public MultiplicacionNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x) => Izquierdo.Evaluar(x) * Derecho.Evaluar(x);
}

public class DivisionNodo : NodoBinario
{
    public DivisionNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x)
    {
        int divisor = Derecho.Evaluar(x);
        if (divisor == 0)
            throw new DivideByZeroException("Error: División por cero.");
        return Izquierdo.Evaluar(x) / divisor;
    }
}
public class Compilador
{
    private readonly string _input;
    private int _pos;

    public Compilador(string input)
    {
        
        _input = input.Replace(" ", "");
        _pos = 0;
    }
    public Nodo Parsear()
    {
        if (string.IsNullOrWhiteSpace(_input))
            throw new Exception("Error: Entrada vacía.");

        Nodo resultado = ParsearExpresion();

        if (_pos < _input.Length)
            throw new Exception($"Error: Token inesperado en la posición {_pos} ('{_input[_pos]}').");

        return resultado;
    }
    private Nodo ParsearExpresion()
    {
        Nodo nodo = ParsearTermino();

        while (_pos < _input.Length && (_input[_pos] == '+' || _input[_pos] == '-'))
        {
            char operador = _input[_pos];
            _pos++;
            Nodo derecho = ParsearTermino();

            if (operador == '+')
                nodo = new SumaNodo(nodo, derecho);
            else
                nodo = new RestaNodo(nodo, derecho);
        }

        return nodo;
    }

    private Nodo ParsearTermino()
    {
        Nodo nodo = ParsearFactor();

        while (_pos < _input.Length && (_input[_pos] == '*' || _input[_pos] == '/'))
        {
            char operador = _input[_pos];
            _pos++;
            Nodo derecho = ParsearFactor();

            if (operador == '*')
                nodo = new MultiplicacionNodo(nodo, derecho);
            else
                nodo = new DivisionNodo(nodo, derecho);
        }

        return nodo;
    }
    private Nodo ParsearFactor()
    {
        if (_pos >= _input.Length)
            throw new Exception("Error: Se esperaba un factor, pero se encontró el final de la expresión.");

        char actual = _input[_pos];

        
        if (actual == '+')
        {
            _pos++;
            return new PositivoNodo(ParsearFactor());
        }
        if (actual == '-')
        {
            _pos++;
            return new NegativoNodo(ParsearFactor());
        }

        if (actual == '(')
        {
            _pos++;
            Nodo nodo = ParsearExpresion();
            if (_pos >= _input.Length || _input[_pos] != ')')
                throw new Exception("Error: Paréntesis sin cerrar.");
            _pos++; 
            return nodo;
        }

        
        if (char.ToLower(actual) == 'x')
        {
            _pos++;
            return new VariableNodo();
        }

        
        if (char.IsDigit(actual))
        {
            int inicio = _pos;
            while (_pos < _input.Length && char.IsDigit(_input[_pos]))
                _pos++;
            
            int valor = int.Parse(_input.Substring(inicio, _pos - inicio));
            return new NumeroNodo(valor);
        }

        throw new Exception($"Error: Token inesperado '{actual}' en la posición {_pos}.");
    }
}