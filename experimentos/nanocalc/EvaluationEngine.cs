using System.Globalization;

namespace NanoCalc;

internal sealed class EvaluationEngine {
    private readonly SpreadsheetDocument _document;
    private readonly Dictionary<CellAddress, CalcValue> _cache = [];
    private readonly HashSet<CellAddress> _visiting = [];
    private int _seenVersion = -1;

    public EvaluationEngine(SpreadsheetDocument document) {
        _document = document;
    }

    public CalcValue EvaluateCell(CellAddress address) {
        if (!address.IsValid) {
            return CalcValue.Empty;
        }

        EnsureFreshCache();

        if (_cache.TryGetValue(address, out var cached)) {
            return cached;
        }

        if (!_visiting.Add(address)) {
            return CacheAndReturn(address, CalcValue.Error("#circular"));
        }

        try {
            var cell = _document.GetCell(address);
            var value = cell.Content switch {
                EmptyCellContent => CalcValue.Empty,
                NumberCellContent number => CalcValue.FromNumber(number.Value),
                StringCellContent text => CalcValue.FromText(text.Value),
                FormulaCellContent formula => SafeEvaluate(formula.Expression, new EvaluationContext(this, address)),
                VariableDefinitionCellContent variable => CalcValue.FromText(variable.Name),
                FunctionDefinitionCellContent function => CalcValue.FromText(function.Name),
                _ => CalcValue.Empty
            };

            return CacheAndReturn(address, value);
        }
        finally {
            _visiting.Remove(address);
        }
    }

    public CalcValue EvaluateNamedValue(string name) {
        if (string.Equals(name, "pi", StringComparison.CurrentCultureIgnoreCase)) {
            return CalcValue.FromNumber((decimal)Math.PI);
        }

        if (string.Equals(name, "e", StringComparison.CurrentCultureIgnoreCase)) {
            return CalcValue.FromNumber((decimal)Math.E);
        }

        if (_document.TryGetVariableAddress(name, out var variableAddress)) {
            return EvaluateCell(variableAddress.Offset(0, 1));
        }

        if (_document.TryGetImplicitVariableAddress(name, out variableAddress)) {
            return EvaluateCell(variableAddress.Offset(0, 1));
        }

        if (name.Length >= 2 && CellAddress.TryParse(name, out var explicitCell)) {
            return EvaluateCell(explicitCell);
        }

        return CalcValue.Error("#error");
    }

    public CalcValue InvokeFunction(string name, IReadOnlyList<FunctionArgumentNode> arguments, EvaluationContext context) {
        var lowered = name.ToLowerInvariant();
        return lowered switch {
            "si" => InvokeIf(arguments, context),
            "suma" => InvokeAggregate(arguments, context, AggregateMode.Sum),
            "promedio" => InvokeAggregate(arguments, context, AggregateMode.Average),
            "minimo" => InvokeAggregate(arguments, context, AggregateMode.Minimum),
            "maximo" => InvokeAggregate(arguments, context, AggregateMode.Maximum),
            "sin" => InvokeUnary(arguments, context, value => Math.Sin(value)),
            "cos" => InvokeUnary(arguments, context, value => Math.Cos(value)),
            "tan" => InvokeUnary(arguments, context, value => Math.Tan(value)),
            "asin" => InvokeUnary(arguments, context, value => Math.Asin(value)),
            "acos" => InvokeUnary(arguments, context, value => Math.Acos(value)),
            "atan" => InvokeUnary(arguments, context, value => Math.Atan(value)),
            "mayusculas" => InvokeString(arguments, context, value => value.ToUpperInvariant()),
            "minusculas" => InvokeString(arguments, context, value => value.ToLowerInvariant()),
            "subcadena" => InvokeSubstring(arguments, context),
            _ => InvokeUserFunction(name, arguments, context)
        };
    }

    private CalcValue InvokeIf(IReadOnlyList<FunctionArgumentNode> arguments, EvaluationContext context) {
        if (arguments.Count != 3) {
            return CalcValue.Error("#error");
        }

        if (!TryGetScalarExpression(arguments[0], out var conditionExpression) ||
            !TryGetScalarExpression(arguments[1], out var trueExpression) ||
            !TryGetScalarExpression(arguments[2], out var falseExpression)) {
            return CalcValue.Error("#error");
        }

        var condition = SafeEvaluate(conditionExpression, context);
        if (condition.IsError) {
            return condition;
        }

        return condition.IsTruthy()
            ? SafeEvaluate(trueExpression, context)
            : SafeEvaluate(falseExpression, context);
    }

    private CalcValue InvokeUnary(IReadOnlyList<FunctionArgumentNode> arguments, EvaluationContext context, Func<double, double> operation) {
        if (arguments.Count != 1 || !TryGetScalarExpression(arguments[0], out var expression)) {
            return CalcValue.Error("#error");
        }

        var argument = SafeEvaluate(expression, context);
        if (argument.IsError) {
            return argument;
        }

        var result = operation((double)argument.ToNumber());
        return double.IsNaN(result) || double.IsInfinity(result)
            ? CalcValue.Error("#error")
            : CalcValue.FromNumber((decimal)result);
    }

    private CalcValue InvokeString(IReadOnlyList<FunctionArgumentNode> arguments, EvaluationContext context, Func<string, string> operation) {
        if (arguments.Count != 1 || !TryGetScalarExpression(arguments[0], out var expression)) {
            return CalcValue.Error("#error");
        }

        var argument = SafeEvaluate(expression, context);
        if (argument.IsError) {
            return argument;
        }

        return CalcValue.FromText(operation(argument.ToText()));
    }

    private CalcValue InvokeSubstring(IReadOnlyList<FunctionArgumentNode> arguments, EvaluationContext context) {
        if (arguments.Count is < 2 or > 3) {
            return CalcValue.Error("#error");
        }

        if (!TryGetScalarExpression(arguments[0], out var sourceExpression) ||
            !TryGetScalarExpression(arguments[1], out var startExpression)) {
            return CalcValue.Error("#error");
        }

        var source = SafeEvaluate(sourceExpression, context);
        var startValue = SafeEvaluate(startExpression, context);
        if (source.IsError || startValue.IsError) {
            return source.IsError ? source : startValue;
        }

        var start = (int)startValue.ToNumber();
        var text = source.ToText();
        if (start < 0) {
            start = 0;
        }

        if (start >= text.Length) {
            return CalcValue.FromText(string.Empty);
        }

        if (arguments.Count == 2) {
            return CalcValue.FromText(text[start..]);
        }

        if (!TryGetScalarExpression(arguments[2], out var lengthExpression)) {
            return CalcValue.Error("#error");
        }

        var lengthValue = SafeEvaluate(lengthExpression, context);
        if (lengthValue.IsError) {
            return lengthValue;
        }

        var length = Math.Max(0, (int)lengthValue.ToNumber());
        return CalcValue.FromText(text.Substring(start, Math.Min(length, text.Length - start)));
    }

    private CalcValue InvokeAggregate(IReadOnlyList<FunctionArgumentNode> arguments, EvaluationContext context, AggregateMode mode) {
        if (!TryExpandArguments(arguments, context, out var values, out var error)) {
            return error;
        }

        if (mode == AggregateMode.Sum && values.Count == 0) {
            return CalcValue.FromNumber(0m);
        }

        if (values.Count == 0) {
            return CalcValue.Error("#error");
        }

        var numbers = values.Select(value => value.ToNumber()).ToList();
        return mode switch {
            AggregateMode.Sum => CalcValue.FromNumber(numbers.Sum()),
            AggregateMode.Average => CalcValue.FromNumber(numbers.Sum() / numbers.Count),
            AggregateMode.Minimum => CalcValue.FromNumber(numbers.Min()),
            AggregateMode.Maximum => CalcValue.FromNumber(numbers.Max()),
            _ => CalcValue.Error("#error")
        };
    }

    private CalcValue InvokeUserFunction(string name, IReadOnlyList<FunctionArgumentNode> arguments, EvaluationContext context) {
        if (!_document.TryGetFunction(name, out var definition)) {
            return CalcValue.Error("#error");
        }

        if (definition.Definition.Parameters.Count != arguments.Count) {
            return CalcValue.Error("#error");
        }

        var locals = new Dictionary<string, CalcValue>(StringComparer.CurrentCultureIgnoreCase);
        for (var index = 0; index < arguments.Count; index++) {
            if (!TryGetScalarExpression(arguments[index], out var expression)) {
                return CalcValue.Error("#error");
            }

            var value = SafeEvaluate(expression, context);
            if (value.IsError) {
                return value;
            }

            locals[definition.Definition.Parameters[index]] = value;
        }

        var definitionContext = new EvaluationContext(this, definition.Address, locals);
        return SafeEvaluate(definition.Definition.BodyExpression, definitionContext);
    }

    private bool TryExpandArguments(
        IReadOnlyList<FunctionArgumentNode> arguments,
        EvaluationContext context,
        out List<CalcValue> values,
        out CalcValue error) {
        values = [];
        error = default;

        foreach (var argument in arguments) {
            foreach (var value in EnumerateArgumentValues(argument, context)) {
                if (value.IsError) {
                    error = value;
                    values.Clear();
                    return false;
                }

                values.Add(value);
            }
        }

        return true;
    }

    private IEnumerable<CalcValue> EnumerateArgumentValues(FunctionArgumentNode argument, EvaluationContext context) {
        switch (argument) {
            case ScalarArgumentNode scalar:
                yield return SafeEvaluate(scalar.Expression, context);
                yield break;

            case RangeArgumentNode range: {
                    var start = context.Origin.Offset(range.Start.RowOffset, range.Start.ColumnOffset);
                    var end = context.Origin.Offset(range.End.RowOffset, range.End.ColumnOffset);
                    var top = Math.Min(start.Row, end.Row);
                    var bottom = Math.Max(start.Row, end.Row);
                    var left = Math.Min(start.Column, end.Column);
                    var right = Math.Max(start.Column, end.Column);

                    for (var row = top; row <= bottom; row++) {
                        for (var column = left; column <= right; column++) {
                            yield return EvaluateCell(new CellAddress(row, column));
                        }
                    }

                    yield break;
                }

            default:
                yield return CalcValue.Error("#error");
                yield break;
        }
    }

    private static bool TryGetScalarExpression(FunctionArgumentNode argument, out ExpressionNode expression) {
        if (argument is ScalarArgumentNode scalar) {
            expression = scalar.Expression;
            return true;
        }

        expression = null!;
        return false;
    }

    private CalcValue SafeEvaluate(ExpressionNode expression, EvaluationContext context) {
        try {
            return expression.Evaluate(context);
        }
        catch {
            return CalcValue.Error("#error");
        }
    }

    private void EnsureFreshCache() {
        if (_seenVersion == _document.Version) {
            return;
        }

        _cache.Clear();
        _visiting.Clear();
        _seenVersion = _document.Version;
    }

    private CalcValue CacheAndReturn(CellAddress address, CalcValue value) {
        _cache[address] = value;
        return value;
    }

    private enum AggregateMode {
        Sum,
        Average,
        Minimum,
        Maximum
    }
}
