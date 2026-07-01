using System;

namespace Calculadora
{
    // Clase base abstracta con nombre distinto
    public abstract class ExpresionMatematica
    {
        public abstract int Calcular(int valorX);
    }

    // Número constante
    internal sealed class ConstanteNumerica : ExpresionMatematica
    {
        private readonly int _numero;
        public ConstanteNumerica(int numero) => _numero = numero;
        public override int Calcular(int valorX) => _numero;
    }

    // Variable X
    internal sealed class VariableX : ExpresionMatematica
    {
        public override int Calcular(int valorX) => valorX;
    }

    // Operador unario menos
    internal sealed class Opuesto : ExpresionMatematica
    {
        private readonly ExpresionMatematica _operando;
        public Opuesto(ExpresionMatematica operando) => _operando = operando;
        public override int Calcular(int valorX) => -_operando.Calcular(valorX);
    }

    // Operador unario más (identidad)
    internal sealed class Identidad : ExpresionMatematica
    {
        private readonly ExpresionMatematica _operando;
        public Identidad(ExpresionMatematica operando) => _operando = operando;
        public override int Calcular(int valorX) => _operando.Calcular(valorX);
    }

    // Clase base para binarias (con campos protegidos)
    internal abstract class OperacionBinariaAbstracta : ExpresionMatematica
    {
        protected readonly ExpresionMatematica LadoIzquierdo;
        protected readonly ExpresionMatematica LadoDerecho;

        protected OperacionBinariaAbstracta(ExpresionMatematica izquierdo, ExpresionMatematica derecho)
        {
            LadoIzquierdo = izquierdo;
            LadoDerecho = derecho;
        }
    }

    // Suma
    internal sealed class Suma : OperacionBinariaAbstracta
    {
        public Suma(ExpresionMatematica izq, ExpresionMatematica der) : base(izq, der) { }
        public override int Calcular(int valorX) => LadoIzquierdo.Calcular(valorX) + LadoDerecho.Calcular(valorX);
    }

    // Resta
    internal sealed class Resta : OperacionBinariaAbstracta
    {
        public Resta(ExpresionMatematica izq, ExpresionMatematica der) : base(izq, der) { }
        public override int Calcular(int valorX) => LadoIzquierdo.Calcular(valorX) - LadoDerecho.Calcular(valorX);
    }

    // Multiplicación
    internal sealed class Producto : OperacionBinariaAbstracta
    {
        public Producto(ExpresionMatematica izq, ExpresionMatematica der) : base(izq, der) { }
        public override int Calcular(int valorX) => LadoIzquierdo.Calcular(valorX) * LadoDerecho.Calcular(valorX);
    }

    // División (con control de división por cero)
    internal sealed class Cociente : OperacionBinariaAbstracta
    {
        public Cociente(ExpresionMatematica izq, ExpresionMatematica der) : base(izq, der) { }
        public override int Calcular(int valorX)
        {
            int divisor = LadoDerecho.Calcular(valorX);
            if (divisor == 0)
                throw new DivideByZeroException("No se puede dividir por cero.");
            return LadoIzquierdo.Calcular(valorX) / divisor;
        }
    }
}