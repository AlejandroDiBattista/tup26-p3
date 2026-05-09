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

//-----------------------------------
abstract class NodoBinario : Nodo {
    protected Nodo Izq;
    protected Nodo Der;

    public NodoBinario(Nodo izq, Nodo der) {
        Izq = izq;
        Der = der;
    }
}
class SumarNodo : NodoBinario {
    public SumarNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x = 0) => Izq.Evaluar(x) + Der.Evaluar(x);
}
//--------------RESTA---------------------

class RestaNodo : NodoBinario {
    public RestaNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x = 0) => Izq.Evaluar(x) - Der.Evaluar(x);
}

//----------------MULTIPLICATION-------------------
class MultiplicacionNodo : NodoBinario {
    public MultiplicacionNodo(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int x = 0) => Izq.Evaluar(x) * Der.Evaluar(x);
}
//---------------DIVISION--------------------
class DivisionNodo : NodoBinario {
    public DivisionNodo(Nodo izq, Nodo der) : base(izq, der) { }

    public override int Evaluar(int x = 0) {
        var denom = Der.Evaluar(x);
        if (denom == 0) throw new DivideByZeroException("División por cero");
        return Izq.Evaluar(x) / denom;
    }
}

