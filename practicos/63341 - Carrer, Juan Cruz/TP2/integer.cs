using System;
using System.Collections.Generic;
using System.Text;

class Integer
{
    private List<int> listaDigitos;
    private bool esNegativo;

    public Integer(string valorTexto)
    {
        listaDigitos = new List<int>();
        esNegativo = false;

        if (valorTexto[0] == '-')
        {
            esNegativo = true;
            valorTexto = valorTexto.Substring(1);
        }

        for (int indice = valorTexto.Length - 1; indice >= 0; indice--)
            listaDigitos.Add(valorTexto[indice] - '0');

        Normalizar();
    }

    private Integer(List<int> digitosIniciales, bool signoNegativo)
    {
        this.listaDigitos = digitosIniciales;
        this.esNegativo = signoNegativo;
        Normalizar();
    }
    private void Normalizar()
    {
        while (listaDigitos.Count > 1 && listaDigitos[^1] == 0)
            listaDigitos.RemoveAt(listaDigitos.Count - 1);

        if (listaDigitos.Count == 1 && listaDigitos[0] == 0)
            esNegativo = false;
    }
    public bool EsCero() => listaDigitos.Count == 1 && listaDigitos[0] == 0;

    public static Integer operator +(Integer valorA, Integer valorB)
    {
        if (valorA.esNegativo == valorB.esNegativo)
            return new Integer(SumarListas(valorA.listaDigitos, valorB.listaDigitos), valorA.esNegativo);

        if (CompararAbsoluto(valorA, valorB) >= 0)
            return new Integer(RestarListas(valorA.listaDigitos, valorB.listaDigitos), valorA.esNegativo);

        return new Integer(RestarListas(valorB.listaDigitos, valorA.listaDigitos), valorB.esNegativo);
    }

    public static Integer operator -(Integer valorA, Integer valorB) => valorA + (-valorB);

    public static Integer operator -(Integer valor)
        => new Integer(new List<int>(valor.listaDigitos), !valor.esNegativo);

    public static Integer operator *(Integer valorA, Integer valorB)
    {
        var resultado = new int[valorA.listaDigitos.Count + valorB.listaDigitos.Count];

        for (int i = 0; i < valorA.listaDigitos.Count; i++)
        {
            for (int j = 0; j < valorB.listaDigitos.Count; j++)
            {
                resultado[i + j] += valorA.listaDigitos[i] * valorB.listaDigitos[j];
                resultado[i + j + 1] += resultado[i + j] / 10;
                resultado[i + j] %= 10;
            }
        }

        return new Integer(new List<int>(resultado), valorA.esNegativo ^ valorB.esNegativo);
    }

    public static Integer operator /(Integer valorA, Integer valorB)
    {
        if (valorB.EsCero()) throw new DivideByZeroException();

        Integer dividendo = new Integer(valorA.ToString());
        Integer divisor = new Integer(valorB.ToString());
        Integer contador = new Integer("0");

        while (CompararAbsoluto(dividendo, divisor) >= 0)
        {
            dividendo = dividendo - divisor;
            contador = contador + new Integer("1");
        }

        return new Integer(contador.ToString())
        {
            esNegativo = valorA.esNegativo ^ valorB.esNegativo
        };
    }

    public static Integer operator %(Integer valorA, Integer valorB)
    {
        if (valorB.EsCero()) throw new DivideByZeroException();

        Integer dividendo = new Integer(valorA.ToString());
        Integer divisor = new Integer(valorB.ToString());

        while (CompararAbsoluto(dividendo, divisor) >= 0)
            dividendo = dividendo - divisor;

        return dividendo;
    }

    private static List<int> SumarListas(List<int> listaA, List<int> listaB)
    {
        List<int> resultado = new List<int>();
        int acarreo = 0;

        for (int i = 0; i < Math.Max(listaA.Count, listaB.Count) || acarreo > 0; i++)
        {
            int suma = acarreo;
            if (i < listaA.Count) suma += listaA[i];
            if (i < listaB.Count) suma += listaB[i];

            resultado.Add(suma % 10);
            acarreo = suma / 10;
        }

        return resultado;
    }

    private static List<int> RestarListas(List<int> listaA, List<int> listaB)
    {
        List<int> resultado = new List<int>();
        int prestamo = 0;

        for (int i = 0; i < listaA.Count; i++)
        {
            int diferencia = listaA[i] - prestamo - (i < listaB.Count ? listaB[i] : 0);

            if (diferencia < 0)
            {
                diferencia += 10;
                prestamo = 1;
            }
            else prestamo = 0;

            resultado.Add(diferencia);
        }

        return resultado;
    }

    private static int CompararAbsoluto(Integer valorA, Integer valorB)
    {
        if (valorA.listaDigitos.Count != valorB.listaDigitos.Count)
            return valorA.listaDigitos.Count.CompareTo(valorB.listaDigitos.Count);

        for (int i = valorA.listaDigitos.Count - 1; i >= 0; i--)
            if (valorA.listaDigitos[i] != valorB.listaDigitos[i])
                return valorA.listaDigitos[i].CompareTo(valorB.listaDigitos[i]);

        return 0;
    }

    public override string ToString()
    {
        StringBuilder resultadoTexto = new StringBuilder();
        if (esNegativo) resultadoTexto.Append('-');

        for (int i = listaDigitos.Count - 1; i >= 0; i--)
            resultadoTexto.Append(listaDigitos[i]);

        return resultadoTexto.ToString();
    }
}