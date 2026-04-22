using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;

public class ParserError : Exception
{
    public ParserError(string message) : base(message) { }
}

public abstract class AstNode { }

public class NumberNode : AstNode
{
    public double Value { get; }

    public NumberNode(double value)
    {
        Value = value;
    }
}

public class VariableNode : AstNode
{
    public string Name { get; }

    public VariableNode(string name)
    {
        Name = name;
    }
}

public class AssignmentNode : AstNode
{
    public string Name { get; }
    public AstNode Value { get; }

    public AssignmentNode(string name, AstNode value)
    {
        Name = name;
        Value = value;
    }
}

public class BinaryOpNode : AstNode
{
    public string Op { get; }
    public AstNode Left { get; }
    public AstNode Right { get; }

    public BinaryOpNode(string op, AstNode left, AstNode right)
    {
        Op = op;
        Left = left;
        Right = right;
    }
}

public class UnaryOpNode : AstNode
{
    public string Op { get; }
    public AstNode Operand { get; }

    public UnaryOpNode(string op, AstNode operand)
    {
        Op = op;
        Operand = operand;
    }
}

public class Token
{
    public string Type { get; }
    public object? Value { get; }

    public Token(string type, object? value)
    {
        Type = type;
        Value = value;
    }
}

public class Lexer
{
    private readonly string _text;
    private int _pos;

    public Lexer(string text)
    {
        _text = text;
        _pos = 0;
    }

    private char? CurrentChar()
    {
        if (_pos >= _text.Length)
            return null;
        return _text[_pos];
    }

    private char? PeekChar()
    {
        int next = _pos + 1;
        if (next >= _text.Length)
            return null;
        return _text[next];
    }

    private void Advance()
    {
        _pos++;
    }

    private void SkipWhitespace()
    {
        while (CurrentChar() is char ch && char.IsWhiteSpace(ch))
            Advance();
    }

    private Token Number()
    {
        int start = _pos;
        bool hasDot = false;

        while (CurrentChar() is char ch)
        {
            if (char.IsDigit(ch))
            {
                Advance();
            }
            else if (ch == '.' && !hasDot)
            {
                hasDot = true;
                Advance();
            }
            else
            {
                break;
            }
        }

        string value = _text.Substring(start, _pos - start);
        if (value == ".")
            throw new ParserError("Número inválido");

        return new Token("NUMBER", double.Parse(value, CultureInfo.InvariantCulture));
    }

    private Token Identifier()
    {
        int start = _pos;

        while (CurrentChar() is char ch && (char.IsLetterOrDigit(ch) || ch == '_'))
            Advance();

        string value = _text.Substring(start, _pos - start);
        return new Token("IDENT", value);
    }

    public List<Token> Tokens()
    {
        var result = new List<Token>();

        while (CurrentChar() is char ch)
        {
            if (char.IsWhiteSpace(ch))
            {
                SkipWhitespace();
            }
            else if (char.IsDigit(ch) || ch == '.')
            {
                result.Add(Number());
            }
            else if (char.IsLetter(ch) || ch == '_')
            {
                result.Add(Identifier());
            }
            else if (ch == '=')
            {
                result.Add(new Token("=", "="));
                Advance();
            }
            else if ("+-*/()".Contains(ch))
            {
                result.Add(new Token(ch.ToString(), ch.ToString()));
                Advance();
            }
            else
            {
                throw new ParserError($"Carácter inesperado: {ch}");
            }
        }

        result.Add(new Token("EOF", null));
        return result;
    }
}

public class Parser
{
    private readonly List<Token> _tokens;
    private int _pos;
    private Token _current;

    public Parser(string text)
    {
        _tokens = new Lexer(text).Tokens();
        _pos = 0;
        _current = _tokens[_pos];
    }

    private Token Peek(int offset = 1)
    {
        int index = _pos + offset;
        if (index >= _tokens.Count)
            return _tokens[^1];
        return _tokens[index];
    }

    private void Eat(string tokenType)
    {
        if (_current.Type == tokenType)
        {
            _pos++;
            _current = _tokens[_pos];
        }
        else
        {
            throw new ParserError($"Se esperaba {tokenType} y se encontró {_current.Type}");
        }
    }

    public AstNode Parse()
    {
        AstNode node = Statement();
        if (_current.Type != "EOF")
            throw new ParserError("Expresión inválida");
        return node;
    }

    private AstNode Statement()
    {
        if (_current.Type == "IDENT" && Peek().Type == "=")
        {
            string name = (string)_current.Value!;
            Eat("IDENT");
            Eat("=");
            return new AssignmentNode(name, Expr());
        }

        return Expr();
    }

    private AstNode Expr()
    {
        AstNode node = Term();

        while (_current.Type == "+" || _current.Type == "-")
        {
            string op = _current.Type;
            Eat(op);
            node = new BinaryOpNode(op, node, Term());
        }

        return node;
    }

    private AstNode Term()
    {
        AstNode node = Factor();

        while (_current.Type == "*" || _current.Type == "/")
        {
            string op = _current.Type;
            Eat(op);
            node = new BinaryOpNode(op, node, Factor());
        }

        return node;
    }

    private AstNode Factor()
    {
        string tokenType = _current.Type;
        object? tokenValue = _current.Value;

        if (tokenType == "+")
        {
            Eat("+");
            return new UnaryOpNode("+", Factor());
        }

        if (tokenType == "-")
        {
            Eat("-");
            return new UnaryOpNode("-", Factor());
        }

        if (tokenType == "NUMBER")
        {
            Eat("NUMBER");
            return new NumberNode((double)tokenValue!);
        }

        if (tokenType == "IDENT")
        {
            Eat("IDENT");
            return new VariableNode((string)tokenValue!);
        }

        if (tokenType == "(")
        {
            Eat("(");
            AstNode node = Expr();
            Eat(")");
            return node;
        }

        throw new ParserError($"Token inesperado: {tokenType}");
    }
}

public class Evaluator
{
    private readonly Dictionary<string, double> _variables;

    public Evaluator(Dictionary<string, double> variables)
    {
        _variables = variables;
    }

    public double Visit(AstNode node)
    {
        if (node is NumberNode number)
            return number.Value;

        if (node is VariableNode variable)
        {
            if (!_variables.TryGetValue(variable.Name, out double value))
                throw new ParserError($"Variable no definida: {variable.Name}");
            return value;
        }

        if (node is AssignmentNode assignment)
        {
            double value = Visit(assignment.Value);
            _variables[assignment.Name] = value;
            return value;
        }

        if (node is UnaryOpNode unary)
        {
            double value = Visit(unary.Operand);
            return unary.Op == "+" ? value : -value;
        }

        if (node is BinaryOpNode binary)
        {
            double left = Visit(binary.Left);
            double right = Visit(binary.Right);

            return binary.Op switch
            {
                "+" => left + right,
                "-" => left - right,
                "*" => left * right,
                "/" => right == 0 ? throw new ParserError("División por cero") : left / right,
                _ => throw new ParserError("Nodo AST inválido")
            };
        }

        throw new ParserError("Nodo AST inválido");
    }
}

public static class AstFormatter
{
    public static object NodeToObject(AstNode node)
    {
        if (node is NumberNode number)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "Number",
                ["value"] = number.Value
            };
        }

        if (node is VariableNode variable)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "Variable",
                ["name"] = variable.Name
            };
        }

        if (node is AssignmentNode assignment)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "Assignment",
                ["name"] = assignment.Name,
                ["value"] = NodeToObject(assignment.Value)
            };
        }

        if (node is UnaryOpNode unary)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "UnaryOp",
                ["op"] = unary.Op,
                ["operand"] = NodeToObject(unary.Operand)
            };
        }

        if (node is BinaryOpNode binary)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "BinaryOp",
                ["op"] = binary.Op,
                ["left"] = NodeToObject(binary.Left),
                ["right"] = NodeToObject(binary.Right)
            };
        }

        throw new ParserError("Nodo AST inválido");
    }

    public static string AstToJson(AstNode node)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(NodeToObject(node), options);
    }

    public static string FormatAstTree(AstNode node, string prefix = "", bool isLast = true)
    {
        string connector = isLast ? "└── " : "├── ";

        if (node is NumberNode number)
        {
            return $"{prefix}{connector}Number({number.Value.ToString(CultureInfo.InvariantCulture)})";
        }

        if (node is VariableNode variable)
        {
            return $"{prefix}{connector}Variable({variable.Name})";
        }

        if (node is AssignmentNode assignment)
        {
            string label = $"Assignment({assignment.Name})";
            string nextPrefix = prefix + (isLast ? "    " : "│   ");
            string child = FormatAstTree(assignment.Value, nextPrefix, true);
            return $"{prefix}{connector}{label}\n{child}";
        }

        if (node is UnaryOpNode unary)
        {
            string label = $"UnaryOp({unary.Op})";
            string nextPrefix = prefix + (isLast ? "    " : "│   ");
            string child = FormatAstTree(unary.Operand, nextPrefix, true);
            return $"{prefix}{connector}{label}\n{child}";
        }

        if (node is BinaryOpNode binary)
        {
            string label = $"BinaryOp({binary.Op})";
            string nextPrefix = prefix + (isLast ? "    " : "│   ");
            string left = FormatAstTree(binary.Left, nextPrefix, false);
            string right = FormatAstTree(binary.Right, nextPrefix, true);
            return $"{prefix}{connector}{label}\n{left}\n{right}";
        }

        return $"{prefix}{connector}NodoDesconocido";
    }
}

public class Program
{
    public static AstNode BuildAst(string expression)
    {
        return new Parser(expression).Parse();
    }

    public static double Evaluate(string expression, Dictionary<string, double> variables)
    {
        AstNode ast = BuildAst(expression);
        return new Evaluator(variables).Visit(ast);
    }

    private static void PrintVariables(Dictionary<string, double> variables)
    {
        Console.WriteLine("Variables:");

        if (variables.Count == 0)
        {
            Console.WriteLine("  (sin variables)");
            return;
        }

        foreach (var pair in variables)
            Console.WriteLine($"  {pair.Key} = {pair.Value.ToString(CultureInfo.InvariantCulture)}");
    }

    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        var variables = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        Console.WriteLine("Evaluador de expresiones en C# usando RDP + AST con variables");
        Console.WriteLine("Gramática:");
        Console.WriteLine("statement -> IDENT = expr | expr");
        Console.WriteLine("expr      -> term ((+ | -) term)*");
        Console.WriteLine("term      -> factor ((* | /) factor)*");
        Console.WriteLine("factor    -> +factor | -factor | NUMBER | IDENT | (expr)");
        Console.WriteLine("Ejemplos: x = 10, y = x * 2, y + 5");
        Console.WriteLine("Comandos: salir, vars");

        while (true)
        {
            Console.Write("> ");
            string? expr = Console.ReadLine()?.Trim();

            if (expr == null)
                break;

            string lower = expr.ToLowerInvariant();
            if (lower == "salir" || lower == "exit" || lower == "quit")
                break;

            if (lower == "vars")
            {
                PrintVariables(variables);
                continue;
            }

            if (string.IsNullOrWhiteSpace(expr))
                continue;

            try
            {
                AstNode ast = BuildAst(expr);
                double result = new Evaluator(variables).Visit(ast);
                object printable = result % 1 == 0 ? (object)(int)result : result;

                Console.WriteLine("AST:");
                Console.WriteLine(AstFormatter.FormatAstTree(ast));
                Console.WriteLine("AST JSON:");
                Console.WriteLine(AstFormatter.AstToJson(ast));
                Console.WriteLine($"Resultado: {printable}");
            }
            catch (ParserError e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }
        }
    }
}
