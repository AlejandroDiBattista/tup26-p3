using System;

namespace CalculadoraAST
{
    public abstract class Nodo
    {
        public abstract int Evaluar(int x = 0);
    }

    public class NumeroNodo : Nodo
    {
        private readonly int _valor;
        public NumeroNodo(int valor) => _valor = valor;
        public override int Evaluar(int x = 0) => _valor;
    }

    public class VariableNodo : Nodo
    {
        public override int Evaluar(int x = 0) => x;
    }

    public class PositivoNodo : Nodo
    {
        private readonly Nodo _ndo;
        public PositivoNodo(Nodo ndo) => _ndo = ndo;
        public override int Evaluar(int x = 0) => _ndo.Evaluar(x);
    }

    public class NegativoNodo : Nodo
    {
        private readonly Nodo _ndo;
        public NegativoNodo(Nodo ndo) => _ndo = ndo;
        public override int Evaluar(int x = 0) => -_ndo.Evaluar(x);
    }

    public abstract class NodoBinario : Nodo
    {
        protected readonly Nodo Izquierdo;
        protected readonly Nodo Derecho;
        protected NodoBinario(Nodo izq, Nodo der)
        {
            Izquierdo = izq;
            Derecho = der;
        }
    }

    public class SumaNodo : NodoBinario
    {
        public SumaNodo(Nodo izq, Nodo der) : base(izq, der) { }
        public override int Evaluar(int x = 0) => Izquierdo.Evaluar(x) + Derecho.Evaluar(x);
    }

    public class RestaNodo : NodoBinario
    {
        public RestaNodo(Nodo izq, Nodo der) : base(izq, der) { }
        public override int Evaluar(int x = 0) => Izquierdo.Evaluar(x) - Derecho.Evaluar(x);
    }

    public class MultiplicacionNodo : NodoBinario
    {
        public MultiplicacionNodo(Nodo izq, Nodo der) : base(izq, der) { }
        public override int Evaluar(int x = 0) => Izquierdo.Evaluar(x) * Derecho.Evaluar(x);
    }

    public class DivisionNodo : NodoBinario
    {
        public DivisionNodo(Nodo izq, Nodo der) : base(izq, der) { }
        public override int Evaluar(int x = 0)
        {
            int denominador = Derecho.Evaluar(x);
            if (denominador == 0) throw new DivideByZeroException("Error: División por cero.");
            return Izquierdo.Evaluar(x) / denominador;
        }
    }
}