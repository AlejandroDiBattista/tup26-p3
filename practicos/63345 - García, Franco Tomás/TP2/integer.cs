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

    public override string ToString()
    {
        StringBuilder constructorTexto = new StringBuilder();
        if (esNegativo) constructorTexto.Append('-');

        for (int i = listaDigitos.Count - 1; i >= 0; i--)
            constructorTexto.Append(listaDigitos[i]);

        return constructorTexto.ToString();
    }
}