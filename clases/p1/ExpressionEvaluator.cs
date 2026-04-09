using System.Globalization;

internal sealed class ExpressionEvaluator
{
    private string _expression = string.Empty;
    private int _index;

    public decimal Evaluate(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            throw new ExpressionException("La expresion no puede estar vacia.");
        }

        _expression = expression;
        _index = 0;

        var result = ParseExpression();
        SkipWhiteSpace();

        if (!IsAtEnd())
        {
            throw new ExpressionException(
                $"Caracter inesperado '{CurrentChar()}' en la posicion {Position()}.");
        }

        return result;
    }

    private decimal ParseExpression()
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

    private decimal ParseTerm()
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
                    throw new ExpressionException("No se puede dividir por cero.");
                }

                value /= divisor;
            }
            else
            {
                return value;
            }
        }
    }

    private decimal ParseFactor()
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
                throw new ExpressionException("Falta un parentesis de cierre.");
            }

            return value;
        }

        return ParseNumber();
    }

    private decimal ParseNumber()
    {
        SkipWhiteSpace();

        var start = _index;
        var hasDigits = false;
        var hasSeparator = false;

        while (!IsAtEnd())
        {
            var ch = CurrentChar();

            if (char.IsDigit(ch))
            {
                hasDigits = true;
                _index++;
            }
            else if ((ch == '.' || ch == ',') && !hasSeparator)
            {
                hasSeparator = true;
                _index++;
            }
            else
            {
                break;
            }
        }

        if (!hasDigits)
        {
            throw new ExpressionException($"Se esperaba un numero en la posicion {Position()}.");
        }

        var rawNumber = _expression[start.._index];
        var normalizedNumber = rawNumber.Replace(',', '.');

        if (!decimal.TryParse(
                normalizedNumber,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var result))
        {
            throw new ExpressionException($"No se pudo interpretar el numero '{rawNumber}'.");
        }

        return result;
    }

    private bool Match(char expected)
    {
        SkipWhiteSpace();

        if (IsAtEnd() || CurrentChar() != expected)
        {
            return false;
        }

        _index++;
        return true;
    }

    private void SkipWhiteSpace()
    {
        while (!IsAtEnd() && char.IsWhiteSpace(CurrentChar()))
        {
            _index++;
        }
    }

    private bool IsAtEnd() => _index >= _expression.Length;

    private char CurrentChar() => _expression[_index];

    private int Position() => _index + 1;
}

internal sealed class ExpressionException : Exception
{
    public ExpressionException(string message) : base(message)
    {
    }
}
