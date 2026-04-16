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