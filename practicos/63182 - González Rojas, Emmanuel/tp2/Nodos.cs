abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}
//-----------------------------------
class NumeroNodo : Nodo {
    public int Valor { get; }

    public NumeroNodo(int valor) {
        Valor = valor;
    }

    public override int Evaluar(int x = 0) => Valor;
}
//-----------------------------------
class VariableNodo : Nodo {
    public override int Evaluar(int x = 0) => x;
}
abstract class NodoUnario : Nodo {
    protected Nodo Operando;

    public NodoUnario(Nodo operando) {
        Operando = operando;
    }
}

class PositivoNodo : NodoUnario {
    public PositivoNodo(Nodo operando) : base(operando) { }
    public override int Evaluar(int x = 0) => Operando.Evaluar(x);
}


class NegativoNodo : NodoUnario {
    public NegativoNodo(Nodo operando) : base(operando) { }
    public override int Evaluar(int x = 0) => -Operando.Evaluar(x);
}

