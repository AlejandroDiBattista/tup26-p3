using System.Globalization;

namespace NanoCalc;

internal static class FormulaTextFormatter {
    public static string Format(ExpressionNode expression, CellAddress owner) {
        return FormatInternal(expression, owner, parentPrecedence: 0);
    }

    public static string FormatArgument(FunctionArgumentNode argument, CellAddress owner) {
        return argument switch {
            ScalarArgumentNode scalar => Format(scalar.Expression, owner),
            RangeArgumentNode range => $"{owner.Offset(range.Start.RowOffset, range.Start.ColumnOffset).ToA1()}:{owner.Offset(range.End.RowOffset, range.End.ColumnOffset).ToA1()}",
            _ => string.Empty
        };
    }

    private static string FormatInternal(ExpressionNode expression, CellAddress owner, int parentPrecedence) {
        return expression switch {
            NumberLiteralNode number => number.Value.ToString(CultureInfo.InvariantCulture),
            StringLiteralNode text => $"\"{text.Value}\"",
            RelativeReferenceNode reference => owner.Offset(reference.RowOffset, reference.ColumnOffset).ToA1(),
            IdentifierNode identifier => identifier.Name,
            FunctionCallNode call => $"{call.Name}({string.Join(", ", call.Arguments.Select(argument => FormatArgument(argument, owner)))})",
            UnaryNode unary => FormatUnary(unary, owner, parentPrecedence),
            BinaryNode binary => FormatBinary(binary, owner, parentPrecedence),
            _ => string.Empty
        };
    }

    private static string FormatUnary(UnaryNode unary, CellAddress owner, int parentPrecedence) {
        const int precedence = 5;
        var operand = FormatInternal(unary.Operand, owner, precedence);
        var text = unary.Operator + operand;
        return precedence < parentPrecedence ? $"({text})" : text;
    }

    private static string FormatBinary(BinaryNode binary, CellAddress owner, int parentPrecedence) {
        var precedence = GetPrecedence(binary.Operator);
        var left = FormatInternal(binary.Left, owner, precedence);
        var right = FormatInternal(binary.Right, owner, precedence + 1);
        var text = $"{left} {binary.Operator} {right}";
        return precedence < parentPrecedence ? $"({text})" : text;
    }

    private static int GetPrecedence(string op) {
        return op switch {
            "==" or "=" or "!=" or "<>" or "<" or "<=" or ">" or ">=" => 1,
            "+" or "-" => 2,
            "*" or "/" or "%" => 3,
            "^" => 4,
            _ => 0
        };
    }
}
