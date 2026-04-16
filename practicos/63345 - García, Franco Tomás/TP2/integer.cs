using System;
using System.Collections.Generic;
using System.Text;

    class Integer
    {
        private List<int> listaDigitos;
        private bool esNegativo;

    public bool Equals(Integer otro)
    {
        if (esNegativo != otro.esNegativo) return false;
        if (listaDigitos.Count != otro.listaDigitos.Count) return false;

        for (int i = 0; i < listaDigitos.Count; i++)
            if (listaDigitos[i] != otro.listaDigitos[i])
                return false;

        return true;
    }

    public int CompareTo(Integer otro)
    {
        if (esNegativo && !otro.esNegativo) return -1;
        if (!esNegativo && otro.esNegativo) return 1;

        int comparacion = CompararAbsolutos(this, otro);

        return esNegativo ? -comparacion : comparacion;
    }

    public bool EsNegativo()
    {
        return esNegativo;
    }

    public static Integer Parse(string texto)
    {
        return new Integer(texto);
    }

    public static implicit operator Integer(int valor)
    {
        return new Integer(valor.ToString());
    }

    public static Integer operator +(Integer numero)
    {
        return new Integer(new List<int>(numero.listaDigitos), numero.esNegativo);
    }
    public Integer(string valor)
    {
        listaDigitos = new List<int>();
        esNegativo = false;

        if (valor[0] == '-')
        {
            esNegativo = true;
            valor = valor.Substring(1);
        }

        for (int indice = valor.Length - 1; indice >= 0; indice--)
            listaDigitos.Add(valor[indice] - '0');

        Normalizar();
    }

    private Integer(List<int> digitosInternos, bool signoNegativo)
    {
        this.listaDigitos = digitosInternos;
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

    public static Integer operator +(Integer primero, Integer segundo)
    {
        if (primero.esNegativo == segundo.esNegativo)
            return new Integer(SumarListas(primero.listaDigitos, segundo.listaDigitos), primero.esNegativo);

        if (CompararAbsolutos(primero, segundo) >= 0)
            return new Integer(RestarListas(primero.listaDigitos, segundo.listaDigitos), primero.esNegativo);

        return new Integer(RestarListas(segundo.listaDigitos, primero.listaDigitos), segundo.esNegativo);
    }

    public static Integer operator -(Integer primero, Integer segundo) => primero + (-segundo);

    public static Integer operator -(Integer numero)
        => new Integer(new List<int>(numero.listaDigitos), !numero.esNegativo);

    public static Integer operator *(Integer primero, Integer segundo)
    {
        var resultado = new int[primero.listaDigitos.Count + segundo.listaDigitos.Count];

        for (int i = 0; i < primero.listaDigitos.Count; i++)
        {
            for (int j = 0; j < segundo.listaDigitos.Count; j++)
            {
                resultado[i + j] += primero.listaDigitos[i] * segundo.listaDigitos[j];
                resultado[i + j + 1] += resultado[i + j] / 10;
                resultado[i + j] %= 10;
            }
        }

        return new Integer(new List<int>(resultado), primero.esNegativo ^ segundo.esNegativo);
    }

    public static Integer operator /(Integer primero, Integer segundo)
    {
        if (segundo.EsCero()) throw new DivideByZeroException();

        Integer dividendo = new Integer(primero.ToString());
        Integer divisor = new Integer(segundo.ToString());
        Integer contador = new Integer("0");

        while (CompararAbsolutos(dividendo, divisor) >= 0)
        {
            dividendo = dividendo - divisor;
            contador = contador + new Integer("1");
        }

        return new Integer(contador.ToString())
        {
            esNegativo = primero.esNegativo ^ segundo.esNegativo
        };
    }

    public static Integer operator %(Integer primero, Integer segundo)
    {
        if (segundo.EsCero()) throw new DivideByZeroException();

        Integer dividendo = new Integer(primero.ToString());
        Integer divisor = new Integer(segundo.ToString());

        while (CompararAbsolutos(dividendo, divisor) >= 0)
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

    private static int CompararAbsolutos(Integer primero, Integer segundo)
    {
        if (primero.listaDigitos.Count != segundo.listaDigitos.Count)
            return primero.listaDigitos.Count.CompareTo(segundo.listaDigitos.Count);

        for (int i = primero.listaDigitos.Count - 1; i >= 0; i--)
            if (primero.listaDigitos[i] != segundo.listaDigitos[i])
                return primero.listaDigitos[i].CompareTo(segundo.listaDigitos[i]);

        return 0;
    }

    public override string ToString()
    {
        StringBuilder constructorTexto = new StringBuilder();
        if (esNegativo) constructorTexto.Append('-');

        for (int i = listaDigitos.Count - 1; i >= 0; i--)
            constructorTexto.Append(listaDigitos[i]);

        return constructorTexto.ToString();
    }
}