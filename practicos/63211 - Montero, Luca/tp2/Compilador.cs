using System;

public class Compilador
{
    private string _entrada;
    private int _posicion;

    public Nodo Parsear(string entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada))
            throw new Exception("Error: Entrada vacía.");

        _entrada = entrada;
        _posicion = 0;
        Nodo resultado = ParsearExpresion();

        SaltarEspacios();
        if (_posicion < _entrada.Length)
            throw new Exception($"Error: Token inesperado en la posición {_posicion}.");

        return resultado;
    }

    private Nodo ParsearExpresion()
    {
        Nodo nodo = ParsearTermino();
        while (true)
        {
            SaltarEspacios();
            if (Coincidir('+'))
                nodo = new SumaNodo(nodo, ParsearTermino());
            else if (Coincidir('-'))
                nodo = new RestaNodo(nodo, ParsearTermino());
            else
                break;
        }
        return nodo;
    }

    private Nodo ParsearTermino()
    {
        Nodo nodo = ParsearFactor();
        while (true)
        {
            SaltarEspacios();
            if (Coincidir('*'))
                nodo = new MultiplicacionNodo(nodo, ParsearFactor());
            else if (Coincidir('/'))
                nodo = new DivisionNodo(nodo, ParsearFactor());
            else
                break;
        }
        return nodo;
    }

    private Nodo ParsearFactor()
    {
        SaltarEspacios();
        if (_posicion >= _entrada.Length)
            throw new Exception("Error: Se esperaba un factor (token inesperado).");

        if (Coincidir('+')) return new PositivoNodo(ParsearFactor());
        if (Coincidir('-')) return new NegativoNodo(ParsearFactor());

        if (Coincidir('('))
        {
            Nodo nodo = ParsearExpresion();
            SaltarEspacios();
            if (!Coincidir(')'))
                throw new Exception("Error: Paréntesis sin cerrar.");
            return nodo;
        }

        if (char.IsDigit(Actual()))
        {
            int inicio = _posicion;
            while (_posicion < _entrada.Length && char.IsDigit(Actual()))
                _posicion++;
            return new NumeroNodo(int.Parse(_entrada.Substring(inicio, _posicion - inicio)));
        }

        if (Actual() == 'x' || Actual() == 'X')
        {
            _posicion++;
            return new VariableNodo();
        }

        throw new Exception($"Error: Token inesperado '{Actual()}' en la posición {_posicion}.");
    }

    private char Actual() => _posicion < _entrada.Length ? _entrada[_posicion] : '\0';

    private bool Coincidir(char esperado)
    {
        if (Actual() == esperado)
        {
            _posicion++;
            return true;
        }
        return false;
    }

    private void SaltarEspacios()
    {
        while (_posicion < _entrada.Length && char.IsWhiteSpace(_entrada[_posicion]))
            _posicion++;
    }
}