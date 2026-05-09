abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}
class NodoValor : Nodo {
    private readonly int _valor;
    public NodoValor(int valor) {
        _valor = valor;
    }
    public override int Evaluar(int x = 0) => _valor;
}
sealed class NumeroNodo : NodoValor {
    public NumeroNodo(int valor) : base(valor) { }
}
class NodoVariable : Nodo {
    public override int Evaluar(int x = 0) => x;
}
sealed class VariableNodo : NodoVariable { }
class NodoOperacion : Nodo {
    private readonly Nodo _izquierdo;
    private readonly Nodo _derecho;
    private readonly string _operador;
    public NodoOperacion(Nodo izquierdo, string operador, Nodo derecho) {
        _izquierdo = izquierdo;
        _operador = operador;
        _derecho = derecho;
    }
    public NodoOperacion(string operador, Nodo izquierdo, Nodo derecho)
        : this(izquierdo, operador, derecho) {
    }
    public override int Evaluar(int x = 0) {
        return _operador switch {
            "+" => _izquierdo.Evaluar(x) + _derecho.Evaluar(x),
            "-" => _izquierdo.Evaluar(x) - _derecho.Evaluar(x),
            "*" => _izquierdo.Evaluar(x) * _derecho.Evaluar(x),
            "/" => _izquierdo.Evaluar(x) / _derecho.Evaluar(x),
            _ => throw new Exception($"Operador inesperado: {_operador}")
        };
    }
}
sealed class NegativoNodo : Nodo {
    private readonly Nodo _operando;
    public NegativoNodo(Nodo operando) {
        _operando = operando;
    }
    public override int Evaluar(int x = 0) => -_operando.Evaluar(x);
}