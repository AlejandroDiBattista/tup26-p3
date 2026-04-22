using System.Globalization;

namespace VisiCalc;

internal sealed class FormulaParser {
    private readonly List<Token> tokens;
    private readonly Func<CellAddress, double> resolveCell;
    private readonly Func<CellAddress, CellAddress, IEnumerable<double>> resolveRange;
    private int position;

    public FormulaParser(
        string formula,
        Func<CellAddress, double> resolveCell,
        Func<CellAddress, CellAddress, IEnumerable<double>> resolveRange) {
        tokens = Tokenize(formula);
        this.resolveCell = resolveCell;
        this.resolveRange = resolveRange;
    }

    public double Parse() {
        double result = ParseExpression();
        Expect(TokenKind.End, "Queda texto sin parsear en la formula.");
        return result;
    }

    private double ParseExpression() {
        double value = ParseTerm();

        while (Current.Kind is TokenKind.Plus or TokenKind.Minus) {
            TokenKind op = Consume().Kind;
            double right = ParseTerm();
            value = op == TokenKind.Plus ? value + right : value - right;
        }

        return value;
    }

    private double ParseTerm() {
        double value = ParseUnary();

        while (Current.Kind is TokenKind.Star or TokenKind.Slash) {
            TokenKind op = Consume().Kind;
            double right = ParseUnary();
            if (op == TokenKind.Star) {
                value *= right;
            } else {
                if (Math.Abs(right) < double.Epsilon) {
                    throw new FormulaException("Division por cero.");
                }

                value /= right;
            }
        }

        return value;
    }

    private double ParseUnary() {
        if (Match(TokenKind.Plus)) {
            return ParseUnary();
        }

        if (Match(TokenKind.Minus)) {
            return -ParseUnary();
        }

        return ParsePrimary();
    }

    private double ParsePrimary() {
        if (Match(TokenKind.Number, out Token number)) {
            return number.Number;
        }

        if (Match(TokenKind.Identifier, out Token identifier)) {
            if (Match(TokenKind.LeftParen)) {
                return ParseFunction(identifier.Text);
            }

            if (CellAddress.TryParse(identifier.Text, out CellAddress address)) {
                return resolveCell(address);
            }

            throw new FormulaException($"Identificador desconocido: {identifier.Text}.");
        }

        if (Match(TokenKind.LeftParen)) {
            double value = ParseExpression();
            Expect(TokenKind.RightParen, "Falta cerrar parentesis.");
            return value;
        }

        throw new FormulaException($"Token inesperado: {Current.Text}.");
    }

    private double ParseFunction(string name) {
        List<double> values = [];

        if (!Check(TokenKind.RightParen)) {
            do {
                if (Check(TokenKind.Identifier) &&
                    CellAddress.TryParse(Current.Text, out CellAddress start) &&
                    Peek().Kind == TokenKind.Colon) {
                    Consume();
                    Consume();
                    Token endToken = Expect(TokenKind.Identifier, "Se esperaba la celda final del rango.");
                    if (!CellAddress.TryParse(endToken.Text, out CellAddress end)) {
                        throw new FormulaException($"Rango invalido: {start}:{endToken.Text}.");
                    }

                    values.AddRange(resolveRange(start, end));
                } else {
                    values.Add(ParseExpression());
                }
            }
            while (Match(TokenKind.Comma));
        }

        Expect(TokenKind.RightParen, "Falta ')' al final de la funcion.");

        if (values.Count == 0) {
            values.Add(0d);
        }

        return name.ToUpperInvariant() switch {
            "SUM" => values.Sum(),
            "AVG" or "AVERAGE" => values.Average(),
            "MIN" => values.Min(),
            "MAX" => values.Max(),
            "COUNT" => values.Count,
            _ => throw new FormulaException($"Funcion desconocida: {name}.")
        };
    }

    private Token Current => tokens[position];

    private Token Peek() => position + 1 < tokens.Count ? tokens[position + 1] : tokens[^1];

    private bool Check(TokenKind kind) => Current.Kind == kind;

    private Token Consume() => tokens[position++];

    private bool Match(TokenKind kind) {
        if (!Check(kind)) {
            return false;
        }

        position++;
        return true;
    }

    private bool Match(TokenKind kind, out Token token) {
        if (!Check(kind)) {
            token = default;
            return false;
        }

        token = tokens[position++];
        return true;
    }

    private Token Expect(TokenKind kind, string message) {
        if (!Check(kind)) {
            throw new FormulaException(message);
        }

        return Consume();
    }

    private static List<Token> Tokenize(string formula) {
        List<Token> tokens = [];
        int i = 0;

        while (i < formula.Length) {
            char c = formula[i];

            if (char.IsWhiteSpace(c)) {
                i++;
                continue;
            }

            if (char.IsDigit(c) || (c == '.' && i + 1 < formula.Length && char.IsDigit(formula[i + 1]))) {
                int start = i;
                i++;
                while (i < formula.Length && (char.IsDigit(formula[i]) || formula[i] == '.')) {
                    i++;
                }

                string numberText = formula[start..i];
                if (!double.TryParse(numberText, NumberStyles.Float, CultureInfo.InvariantCulture, out double number)) {
                    throw new FormulaException($"Numero invalido: {numberText}.");
                }

                tokens.Add(new Token(TokenKind.Number, numberText, number));
                continue;
            }

            if (char.IsLetter(c) || c == '_') {
                int start = i;
                i++;
                while (i < formula.Length && (char.IsLetterOrDigit(formula[i]) || formula[i] == '_')) {
                    i++;
                }

                string identifier = formula[start..i];
                tokens.Add(new Token(TokenKind.Identifier, identifier, 0d));
                continue;
            }

            TokenKind kind = c switch {
                '+' => TokenKind.Plus,
                '-' => TokenKind.Minus,
                '*' => TokenKind.Star,
                '/' => TokenKind.Slash,
                '(' => TokenKind.LeftParen,
                ')' => TokenKind.RightParen,
                ',' => TokenKind.Comma,
                ':' => TokenKind.Colon,
                _ => throw new FormulaException($"Caracter invalido en formula: '{c}'.")
            };

            tokens.Add(new Token(kind, c.ToString(), 0d));
            i++;
        }

        tokens.Add(new Token(TokenKind.End, string.Empty, 0d));
        return tokens;
    }

    private enum TokenKind {
        Number, Identifier,
        Plus, Minus,
        Star, Slash,
        LeftParen, RightParen,
        Comma, Colon,
        End
    }

    private readonly record struct Token(TokenKind Kind, string Text, double Number);
}

internal sealed class FormulaException : Exception {
    public FormulaException(string message) : base(message) { }
}
