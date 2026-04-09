using System.Globalization;

Console.WriteLine("Calculadora aritmetica");
Console.WriteLine("Operadores soportados: +, -, *, / y parentesis");
Console.WriteLine("Ingresa una expresion por linea. Deja la linea vacia para salir.");

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
    {
        break;
    }

    try
    {
        var parser = new ExpressionParser(input);
        var result = parser.Parse();
        Console.WriteLine(result.ToString(CultureInfo.InvariantCulture));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

sealed class ExpressionParser
{
    private readonly string _text;
    private int _position;

    public ExpressionParser(string text)
    {
        _text = text;
    }

    public double Parse()
    {
        var value = ParseExpression();
        SkipWhiteSpace();

        if (!IsAtEnd())
        {
            throw new FormatException($"Caracter inesperado: '{Current()}' en la posicion {_position + 1}.");
        }

        return value;
    }

    private double ParseExpression()
    {
        var value = ParseTerm();

        while (true)
        {
            SkipWhiteSpace();

            if (Match('+'))
            {
                value += ParseTerm();
            }
            else if (Match('-'))
            {
                value -= ParseTerm();
            }
            else
            {
                return value;
            }
        }
    }

    private double ParseTerm()
    {
        var value = ParseFactor();

        while (true)
        {
            SkipWhiteSpace();

            if (Match('*'))
            {
                value *= ParseFactor();
            }
            else if (Match('/'))
            {
                var divisor = ParseFactor();

                if (divisor == 0)
                {
                    throw new DivideByZeroException("No se puede dividir por cero.");
                }

                value /= divisor;
            }
            else
            {
                return value;
            }
        }
    }

    private double ParseFactor()
    {
        SkipWhiteSpace();

        if (Match('+'))
        {
            return ParseFactor();
        }

        if (Match('-'))
        {
            return -ParseFactor();
        }

        if (Match('('))
        {
            var value = ParseExpression();
            SkipWhiteSpace();

            if (!Match(')'))
            {
                throw new FormatException("Falta cerrar un parentesis.");
            }

            return value;
        }

        return ParseNumber();
    }

    private double ParseNumber()
    {
        SkipWhiteSpace();
        var start = _position;
        var hasDigits = false;

        while (!IsAtEnd() && char.IsDigit(Current()))
        {
            hasDigits = true;
            _position++;
        }

        if (!IsAtEnd() && (Current() == '.' || Current() == ','))
        {
            _position++;

            while (!IsAtEnd() && char.IsDigit(Current()))
            {
                hasDigits = true;
                _position++;
            }
        }

        if (!hasDigits)
        {
            if (IsAtEnd())
            {
                throw new FormatException("La expresion termino antes de lo esperado.");
            }

            throw new FormatException($"Se esperaba un numero en la posicion {_position + 1}.");
        }

        var token = _text[start.._position].Replace(',', '.');

        if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            throw new FormatException($"Numero invalido: '{token}'.");
        }

        return value;
    }

    private bool Match(char expected)
    {
        if (IsAtEnd() || Current() != expected)
        {
            return false;
        }

        _position++;
        return true;
    }

    private void SkipWhiteSpace()
    {
        while (!IsAtEnd() && char.IsWhiteSpace(Current()))
        {
            _position++;
        }
    }

    private char Current() => _text[_position];

    private bool IsAtEnd() => _position >= _text.Length;
}
