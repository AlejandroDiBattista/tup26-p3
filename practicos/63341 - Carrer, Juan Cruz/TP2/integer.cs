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
