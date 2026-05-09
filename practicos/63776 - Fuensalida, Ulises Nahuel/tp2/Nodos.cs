using System;

namespace CalculadoraUTN
{
    abstract class Nodo { public abstract int Evaluar(int x = 0); }

    class NumeroNodo : Nodo {
        private readonly int _v;
        public NumeroNodo(int v) => _v = v;
        public override int Evaluar(int x = 0) => _v;
    }

    class VariableNodo : Nodo { public override int Evaluar(int x = 0) => x; }

    class NegativoNodo : Nodo {
        private readonly Nodo _n;
        public NegativoNodo(Nodo n) => _n = n;
        public override int Evaluar(int x = 0) => -_n.Evaluar(x);
    }

    class PositivoNodo : Nodo {
        private readonly Nodo _n;
        public PositivoNodo(Nodo n) => _n = n;
        public override int Evaluar(int x = 0) => _n.Evaluar(x);
    }

    abstract class NodoBinario : Nodo {
        protected Nodo Izq, Der;
        public NodoBinario(Nodo i, Nodo d) { Izq = i; Der = d; }
    }

    class SumaNodo : NodoBinario {
        public SumaNodo(Nodo i, Nodo d) : base(i, d) { }
        public override int Evaluar(int x = 0) => Izq.Evaluar(x) + Der.Evaluar(x);
    }

    class RestaNodo : NodoBinario {
        public RestaNodo(Nodo i, Nodo d) : base(i, d) { }
        public override int Evaluar(int x = 0) => Izq.Evaluar(x) - Der.Evaluar(x);
    }

    class MultiplicacionNodo : NodoBinario {
        public MultiplicacionNodo(Nodo i, Nodo d) : base(i, d) { }
        public override int Evaluar(int x = 0) => Izq.Evaluar(x) * Der.Evaluar(x);
    }

    class DivisionNodo : NodoBinario {
        public DivisionNodo(Nodo i, Nodo d) : base(i, d) { }
        public override int Evaluar(int x = 0) {
            int divisor = Der.Evaluar(x);
            if (divisor == 0) throw new DivideByZeroException();
            return Izq.Evaluar(x) / divisor;
        }
    }
}
