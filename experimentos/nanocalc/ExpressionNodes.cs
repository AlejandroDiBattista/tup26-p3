namespace NanoCalc;

internal abstract record ExpressionNode {
    public abstract CalcValue Evaluate(EvaluationContext context);
}

internal abstract record FunctionArgumentNode;

internal sealed record ScalarArgumentNode(ExpressionNode Expression) : FunctionArgumentNode;

internal sealed record RangeArgumentNode(RelativeReferenceNode Start, RelativeReferenceNode End) : FunctionArgumentNode;

internal sealed record NumberLiteralNode(decimal Value) : ExpressionNode {
    public override CalcValue Evaluate(EvaluationContext context) => CalcValue.FromNumber(Value);
}

internal sealed record StringLiteralNode(string Value) : ExpressionNode {
    public override CalcValue Evaluate(EvaluationContext context) => CalcValue.FromText(Value);
}

internal sealed record RelativeReferenceNode(int RowOffset, int ColumnOffset) : ExpressionNode {
    public override CalcValue Evaluate(EvaluationContext context) {
        return context.Engine.EvaluateCell(context.Origin.Offset(RowOffset, ColumnOffset));
    }
}

internal sealed record IdentifierNode(string Name) : ExpressionNode {
    public override CalcValue Evaluate(EvaluationContext context) {
        if (context.LocalValues.TryGetValue(Name, out var localValue)) {
            return localValue;
        }

        return context.Engine.EvaluateNamedValue(Name);
    }
}

internal sealed record UnaryNode(string Operator, ExpressionNode Operand) : ExpressionNode {
    public override CalcValue Evaluate(EvaluationContext context) {
        var value = Operand.Evaluate(context);
        if (value.IsError) {
            return value;
        }

        return Operator switch {
            "+" => CalcValue.FromNumber(value.ToNumber()),
            "-" => CalcValue.FromNumber(-value.ToNumber()),
            _ => CalcValue.Error("#error")
        };
    }
}

internal sealed record BinaryNode(string Operator, ExpressionNode Left, ExpressionNode Right) : ExpressionNode {
    public override CalcValue Evaluate(EvaluationContext context) {
        var leftValue = Left.Evaluate(context);
        if (leftValue.IsError) {
            return leftValue;
        }

        var rightValue = Right.Evaluate(context);
        if (rightValue.IsError) {
            return rightValue;
        }

        return Operator switch {
            "+" => ApplyAdd(leftValue, rightValue),
            "-" => CalcValue.FromNumber(leftValue.ToNumber() - rightValue.ToNumber()),
            "*" => CalcValue.FromNumber(leftValue.ToNumber() * rightValue.ToNumber()),
            "/" => rightValue.ToNumber() == 0m ? CalcValue.Error("#error") : CalcValue.FromNumber(leftValue.ToNumber() / rightValue.ToNumber()),
            "^" => CalcValue.FromNumber((decimal)Math.Pow((double)leftValue.ToNumber(), (double)rightValue.ToNumber())),
            "<" => Compare(leftValue, rightValue, value => value < 0),
            "<=" => Compare(leftValue, rightValue, value => value <= 0),
            ">" => Compare(leftValue, rightValue, value => value > 0),
            ">=" => Compare(leftValue, rightValue, value => value >= 0),
            "==" or "=" => Compare(leftValue, rightValue, value => value == 0),
            "!=" or "<>" => Compare(leftValue, rightValue, value => value != 0),
            _ => CalcValue.Error("#error")
        };
    }

    private static CalcValue ApplyAdd(CalcValue left, CalcValue right) {
        if (left.Kind == CalcValueKind.Text || right.Kind == CalcValueKind.Text) {
            return CalcValue.FromText(left.ToText() + right.ToText());
        }

        return CalcValue.FromNumber(left.ToNumber() + right.ToNumber());
    }

    private static CalcValue Compare(CalcValue left, CalcValue right, Func<int, bool> predicate) {
        int comparison;
        var leftIsNumeric = left.Kind == CalcValueKind.Number || decimal.TryParse(left.ToText(), out _);
        var rightIsNumeric = right.Kind == CalcValueKind.Number || decimal.TryParse(right.ToText(), out _);

        if (leftIsNumeric && rightIsNumeric) {
            comparison = left.ToNumber().CompareTo(right.ToNumber());
        }
        else if (left.Kind == CalcValueKind.Text || right.Kind == CalcValueKind.Text) {
            comparison = string.Compare(left.ToText(), right.ToText(), StringComparison.CurrentCultureIgnoreCase);
        }
        else {
            comparison = left.ToNumber().CompareTo(right.ToNumber());
        }

        return CalcValue.FromNumber(predicate(comparison) ? 1m : 0m);
    }
}

internal sealed record FunctionCallNode(string Name, IReadOnlyList<FunctionArgumentNode> Arguments) : ExpressionNode {
    public override CalcValue Evaluate(EvaluationContext context) {
        return context.Engine.InvokeFunction(Name, Arguments, context);
    }
}

internal sealed class EvaluationContext {
    public EvaluationContext(EvaluationEngine engine, CellAddress origin, IReadOnlyDictionary<string, CalcValue>? localValues = null) {
        Engine = engine;
        Origin = origin;
        LocalValues = localValues ?? new Dictionary<string, CalcValue>(StringComparer.CurrentCultureIgnoreCase);
    }

    public EvaluationEngine Engine { get; }
    public CellAddress Origin { get; }
    public IReadOnlyDictionary<string, CalcValue> LocalValues { get; }
}
