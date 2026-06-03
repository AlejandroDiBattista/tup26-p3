using System;

namespace CalculadoraAST
{
    public abstract class Nodo
    {
        public abstract int Evaluar(int x = 0);
    }

    public class PositivoNodo : Nodo
    {
        private readonly Nodo _operando;
        public PositivoNodo(Nodo operando) => _operando = operando;
        public override int Evaluar(int x = 0) => _operando.Evaluar(x);
    }

    public class NegativoNodo : Nodo
    {
        private readonly Nodo _operando;
        public NegativoNodo(Nodo operando) => _operando = operando;
        public override int Evaluar(int x = 0) => -_operando.Evaluar(x);
    }

    public class VariableNodo : Nodo
    {
        public override int Evaluar(int x = 0) => x;
    }

    public class NumeroNodo : Nodo
    {
        private readonly int _valor;
        public NumeroNodo(int valor) => _valor = valor;
        public override int Evaluar(int x = 0) => _valor;
    }

    public abstract class NodoBinario : Nodo
    {
        protected readonly Nodo Izquierdo;
        protected readonly Nodo Derecho;

        protected NodoBinario(Nodo izquierdo, Nodo derecho)
        {
            Izquierdo = izquierdo;
            Derecho = derecho;
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
            int divisor = Derecho.Evaluar(x);
            if (divisor == 0)
                throw new DivideByZeroException("Error: División por cero detectada durante la evaluación.");
            return Izquierdo.Evaluar(x) / divisor;
        }
    }
}