namespace NanoCalc;

internal sealed class Cell {
    public string RawText { get; private set; } = string.Empty;
    public CellContent Content { get; private set; } = CellContent.Empty;
    public CellDisplayFormat DisplayFormat { get; private set; } = CellDisplayFormat.General;

    public bool IsEmpty => string.IsNullOrWhiteSpace(RawText);

    public void SetRaw(CellAddress owner, string? rawText) {
        RawText = rawText ?? string.Empty;
        Content = CellContentParser.Parse(owner, RawText, out var displayFormat);
        DisplayFormat = displayFormat;
    }

    public void SetDisplayFormat(CellDisplayFormat displayFormat) {
        DisplayFormat = displayFormat;
    }

    public string GetEditableText(CellAddress owner) {
        return Content switch {
            EmptyCellContent => string.Empty,
            NumberCellContent => RawText,
            StringCellContent => RawText,
            VariableDefinitionCellContent => RawText,
            FormulaCellContent formula => "=" + FormulaTextFormatter.Format(formula.Expression, owner),
            FunctionDefinitionCellContent function => $"{function.Name}({string.Join(", ", function.Parameters)}) = {FormulaTextFormatter.Format(function.BodyExpression, owner)}",
            _ => RawText
        };
    }

    public Cell Clone() {
        return new Cell {
            RawText = RawText,
            Content = Content,
            DisplayFormat = DisplayFormat
        };
    }
}

internal enum CellDisplayFormat {
    General,
    NumberNormal,
    NumberTwoDecimals,
    NumberPercentage,
    NumberCurrency,
    TextNormal,
    TextUpper,
    TextLower,
    TextTitle
}

internal abstract record CellContent {
    public static readonly CellContent Empty = new EmptyCellContent();
}

internal sealed record EmptyCellContent : CellContent;

internal sealed record NumberCellContent(decimal Value) : CellContent;

internal sealed record StringCellContent(string Value) : CellContent;

internal sealed record FormulaCellContent(string ExpressionText, ExpressionNode Expression) : CellContent;

internal sealed record FunctionDefinitionCellContent(
    string Name,
    IReadOnlyList<string> Parameters,
    string BodyText,
    ExpressionNode BodyExpression) : CellContent;

internal sealed record VariableDefinitionCellContent(string Name) : CellContent;

internal static class CellContentParser {
    public static CellContent Parse(CellAddress owner, string rawText, out CellDisplayFormat displayFormat) {
        if (string.IsNullOrWhiteSpace(rawText)) {
            displayFormat = CellDisplayFormat.General;
            return CellContent.Empty;
        }

        var value = rawText.Trim();

        if (value.StartsWith('"')) {
            var text = value.Length > 1 && value.EndsWith('"')
                ? value[1..^1]
                : value[1..];
            displayFormat = CellDisplayFormat.TextNormal;
            return new StringCellContent(text);
        }

        if (value.StartsWith('=')) {
            var expressionText = value[1..].Trim();
            var expression = FormulaParser.ParseExpression(expressionText, owner);
            displayFormat = CellDisplayFormat.General;
            return new FormulaCellContent(expressionText, expression);
        }

        if (TryParseFunctionDefinition(owner, value, out var functionDefinition)) {
            displayFormat = CellDisplayFormat.TextNormal;
            return functionDefinition;
        }

        if (TryParseVariableDefinition(value, out var variableDefinition)) {
            displayFormat = CellDisplayFormat.TextNormal;
            return variableDefinition;
        }

        if (TryParseLiteralNumber(value, out var number, out displayFormat)) {
            return new NumberCellContent(number);
        }

        displayFormat = CellDisplayFormat.TextNormal;
        return new StringCellContent(value);
    }

    private static bool TryParseLiteralNumber(string rawText, out decimal number, out CellDisplayFormat displayFormat) {
        displayFormat = CellDisplayFormat.General;
        if (rawText.Length == 0) {
            number = default;
            return false;
        }

        var value = rawText.Trim();
        var isCurrency = value.StartsWith('$');
        var isPercentage = value.EndsWith('%');

        if (isCurrency) {
            value = value[1..].TrimStart();
        }

        if (isPercentage) {
            value = value[..^1].TrimEnd();
        }

        if (value.Length == 0 || !char.IsDigit(value[0])) {
            number = default;
            return false;
        }

        var parsed = decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out number)
            || decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.CurrentCulture, out number);

        if (!parsed) {
            return false;
        }

        if (isPercentage) {
            number /= 100m;
            displayFormat = CellDisplayFormat.NumberPercentage;
        }
        else if (isCurrency) {
            displayFormat = CellDisplayFormat.NumberCurrency;
        }
        else {
            displayFormat = CellDisplayFormat.NumberNormal;
        }

        return true;
    }

    private static bool TryParseVariableDefinition(string rawText, out VariableDefinitionCellContent variable) {
        variable = null!;
        if (!rawText.EndsWith('=')) {
            return false;
        }

        var name = rawText[..^1].Trim();
        if (!FormulaParser.IsIdentifier(name)) {
            return false;
        }

        variable = new VariableDefinitionCellContent(name);
        return true;
    }

    private static bool TryParseFunctionDefinition(CellAddress owner, string rawText, out FunctionDefinitionCellContent function) {
        function = null!;
        var equalsIndex = rawText.IndexOf('=');
        if (equalsIndex <= 0) {
            return false;
        }

        var left = rawText[..equalsIndex].Trim();
        var right = rawText[(equalsIndex + 1)..].Trim();
        var openIndex = left.IndexOf('(');
        var closeIndex = left.LastIndexOf(')');

        if (openIndex <= 0 || closeIndex <= openIndex) {
            return false;
        }

        var name = left[..openIndex].Trim();
        if (!FormulaParser.IsIdentifier(name)) {
            return false;
        }

        var parameterList = left[(openIndex + 1)..closeIndex].Trim();
        var parameters = parameterList.Length == 0
            ? Array.Empty<string>()
            : parameterList.Split(',').Select(part => part.Trim()).ToArray();

        if (parameters.Any(parameter => !FormulaParser.IsIdentifier(parameter))) {
            return false;
        }

        var expression = FormulaParser.ParseExpression(right, owner);
        function = new FunctionDefinitionCellContent(name, parameters, right, expression);
        return true;
    }
}
