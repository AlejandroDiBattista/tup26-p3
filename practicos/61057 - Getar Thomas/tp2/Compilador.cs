using System;

namespace Calculadora
{
    class Compilador
    {
        private readonly string texto;
        private int posicion;

        public Compilador(string expresion)
        {
            texto = expresion ?? "";
            posicion = 0;
        }

        public Nodo Parsear()
        {
            if (string.IsNullOrWhiteSpace(texto))
                throw new Exception("La expresión está vacía.");

            Nodo resultado = Expresion();

            SaltarEspacios();

            if (posicion < texto.Length)
                throw new Exception($"Token inesperado: '{texto[posicion]}'");

            return resultado;
        }

        private Nodo Expresion()
        {
            Nodo nodo = Termino();

            while (true)
            {
                SaltarEspacios();

                if (Coincide('+'))
                    nodo = new SumaNodo(nodo, Termino());
                else if (Coincide('-'))
                    nodo = new RestaNodo(nodo, Termino());
                else
                    break;
            }

            return nodo;
        }

        private Nodo Termino()
        {
            Nodo nodo = Factor();

            while (true)
            {
                SaltarEspacios();

                if (Coincide('*'))
                    nodo = new MultiplicacionNodo(nodo, Factor());
                else if (Coincide('/'))
                    nodo = new DivisionNodo(nodo, Factor());
                else
                    break;
            }

            return nodo;
        }

        private Nodo Factor()
        {
            SaltarEspacios();

            if (Coincide('+'))
                return new PositivoNodo(Factor());

            if (Coincide('-'))
                return new NegativoNodo(Factor());

            if (Coincide('('))
            {
                Nodo expr = Expresion();

                SaltarEspacios();

                if (!Coincide(')'))
                    throw new Exception("Paréntesis sin cerrar.");

                return expr;
            }

            if (EsFin())
                throw new Exception("Fin inesperado.");

            char c = texto[posicion];

            if (char.IsDigit(c))
            {
                int inicio = posicion;

                while (!EsFin() && char.IsDigit(texto[posicion]))
                    posicion++;

                string numero = texto.Substring(inicio, posicion - inicio);

                return new NumeroNodo(int.Parse(numero));
            }

            if (c == 'x' || c == 'X')
            {
                posicion++;
                return new VariableNodo();
            }

            throw new Exception($"Token inesperado: '{c}'");
        }

        private bool Coincide(char c)
        {
            SaltarEspacios();

            if (!EsFin() && texto[posicion] == c)
            {
                posicion++;
                return true;
            }

            return false;
        }

        private bool EsFin()
        {
            return posicion >= texto.Length;
        }

        private void SaltarEspacios()
        {
            while (!EsFin() && char.IsWhiteSpace(texto[posicion]))
                posicion++;
        }
    }
}