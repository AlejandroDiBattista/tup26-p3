using System;
using System.Collections.Generic;
namespace TP2.Calculadora;

public abstract class Nodo {
    public abstract int Evaluar(int valorX);
}

public class NodoNumero : Nodo {
    private readonly int numero;
    public NodoNumero(int numero) => this.numero = numero;
    public override int Evaluar(int valorX) => numero;
}

public class NodoVariable : Nodo {
    public override int Evaluar(int valorX) => valorX;
}

public abstract class NodoOperacion : Nodo {
    protected readonly Nodo OperandoIzq;
    protected readonly Nodo OperandoDer;

    protected NodoOperacion(Nodo izq, Nodo der) {
        OperandoIzq = izq;
        OperandoDer = der;
    }
}

public class NodoSuma : NodoOperacion {
    public NodoSuma(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int valorX) => OperandoIzq.Evaluar(valorX) + OperandoDer.Evaluar(valorX);
}

public class NodoResta : NodoOperacion {
    public NodoResta(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int valorX) => OperandoIzq.Evaluar(valorX) - OperandoDer.Evaluar(valorX);
}

public class NodoProducto : NodoOperacion {
    public NodoProducto(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int valorX) => OperandoIzq.Evaluar(valorX) * OperandoDer.Evaluar(valorX);
}

public class NodoCociente : NodoOperacion {
    public NodoCociente(Nodo izq, Nodo der) : base(izq, der) { }
    public override int Evaluar(int valorX) => OperandoIzq.Evaluar(valorX) / OperandoDer.Evaluar(valorX);
}

public class NodoNegacion : Nodo {
    private readonly Nodo operando;
    public NodoNegacion(Nodo operando) => this.operando = operando;
    public override int Evaluar(int valorX) => -operando.Evaluar(valorX);
}
