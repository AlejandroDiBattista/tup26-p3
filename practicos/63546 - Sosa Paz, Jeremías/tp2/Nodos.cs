using System;

namespace CalculadoraAST {
    public abstract class Nodo {
        public abstract int Evaluar(int x);
    }

    public class NumeroNodo : Nodo {
        private readonly int _valor;
        public NumeroNodo(int valor) => _valor = valor;
        public override int Evaluar(int x) => _valor;
    }

    public class VariableNodo : Nodo {
        public override int Evaluar(int x) => x;
    }

    public abstract class NodoBinario : Nodo {
        protected readonly Nodo Izquierdo;
        protected readonly Nodo Derecho;
        public NodoBinario(Nodo izq, Nodo der) {
            Izquierdo = izq;
            Derecho = der;
        }
    }

    public class SumaNodo : NodoBinario {
        public SumaNodo(Nodo i, Nodo d) : base(i, d) { }
        public override int Evaluar(int x) => Izquierdo.Evaluar(x) + Derecho.Evaluar(x);
    }

    public class RestaNodo : NodoBinario {
        public RestaNodo(Nodo i, Nodo d) : base(i, d) { }
        public override int Evaluar(int x) => Izquierdo.Evaluar(x) - Derecho.Evaluar(x);
    }

    public class MultiplicacionNodo : NodoBinario {
        public MultiplicacionNodo(Nodo i, Nodo d) : base(i, d) { }
        public override int Evaluar(int x) => Izquierdo.Evaluar(x) * Derecho.Evaluar(x);
    }

    public class DivisionNodo : NodoBinario {
        public DivisionNodo(Nodo i, Nodo d) : base(i, d) { }
        public override int Evaluar(int x) {
            int div = Derecho.Evaluar(x);
            if (div == 0) throw new DivideByZeroException("Error: División por cero.");
            return Izquierdo.Evaluar(x) / div;
        }
    }

    public class NegativoNodo : Nodo {
        private readonly Nodo _nodo;
        public NegativoNodo(Nodo n) => _nodo = n;
        public override int Evaluar(int x) => -_nodo.Evaluar(x);
    }
}
