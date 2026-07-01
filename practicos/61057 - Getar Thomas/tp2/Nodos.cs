
using System;

namespace Calculadora
{
    abstract class Nodo
    {
        public abstract int Evaluar(int x = 0);
    }

    class NumeroNodo : Nodo
    {
        public int Valor { get; }

        public NumeroNodo(int valor)
        {
            Valor = valor;
        }

        public override int Evaluar(int x = 0)
        {
            return Valor;
        }
    }

    class VariableNodo : Nodo
    {
        public override int Evaluar(int x = 0)
        {
            return x;
        }
    }

    class NegativoNodo : Nodo
    {
        private readonly Nodo nodo;

        public NegativoNodo(Nodo nodo)
        {
            this.nodo = nodo;
        }

        public override int Evaluar(int x = 0)
        {
            return -nodo.Evaluar(x);
        }
    }

    class PositivoNodo : Nodo
    {
        private readonly Nodo nodo;

        public PositivoNodo(Nodo nodo)
        {
            this.nodo = nodo;
        }

        public override int Evaluar(int x = 0)
        {
            return nodo.Evaluar(x);
        }
    }

    abstract class NodoBinario : Nodo
    {
        protected readonly Nodo Izquierdo;
        protected readonly Nodo Derecho;

        protected NodoBinario(Nodo izquierdo, Nodo derecho)
        {
            Izquierdo = izquierdo;
            Derecho = derecho;
        }
    }

    class SumaNodo : NodoBinario
    {
        public SumaNodo(Nodo izq, Nodo der) : base(izq, der) { }

        public override int Evaluar(int x = 0)
        {
            return Izquierdo.Evaluar(x) + Derecho.Evaluar(x);
        }
    }

    class RestaNodo : NodoBinario
    {
        public RestaNodo(Nodo izq, Nodo der) : base(izq, der) { }

        public override int Evaluar(int x = 0)
        {
            return Izquierdo.Evaluar(x) - Derecho.Evaluar(x);
        }
    }

    class MultiplicacionNodo : NodoBinario
    {
        public MultiplicacionNodo(Nodo izq, Nodo der) : base(izq, der) { }

        public override int Evaluar(int x = 0)
        {
            return Izquierdo.Evaluar(x) * Derecho.Evaluar(x);
        }
    }

    class DivisionNodo : NodoBinario
    {
        public DivisionNodo(Nodo izq, Nodo der) : base(izq, der) { }

        public override int Evaluar(int x = 0)
        {
            int divisor = Derecho.Evaluar(x);

            if (divisor == 0)
                throw new DivideByZeroException("División por cero.");

            return Izquierdo.Evaluar(x) / divisor;
        }
    }
}