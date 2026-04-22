abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}
//--clase NumeroNodo--------------------------------
class NumeroNodo : Nodo {
    public int Valor { get; }

    public NumeroNodo(int valor) {
        Valor = valor;
    }

    public override int Evaluar(int x = 0) => Valor;
}
//-------clase VariableNodo--------------------------------
class VariableNodo : Nodo {
    public override int Evaluar(int x = 0) => x;
}
abstract class NodoUnario : Nodo {
    protected Nodo Operando;

    public NodoUnario(Nodo operando) {
        Operando = operando;
    }
}
class NegativoNodo : NodoUnario {
    public NegativoNodo(Nodo operando) : base(operando) { }
    public override int Evaluar(int x = 0) => -Operando.Evaluar(x);
}
class PositivoNodo : NodoUnario {
    public PositivoNodo(Nodo operando) : base(operando) { }
    public override int Evaluar(int x = 0) => Operando.Evaluar(x);
}