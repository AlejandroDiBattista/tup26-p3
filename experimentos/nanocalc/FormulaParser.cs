using System.Globalization;

namespace NanoCalc;

internal static class FormulaParser {
    public static ExpressionNode ParseExpression(string text, CellAddress owner) {
        var parser = new Parser(text, owner);
        return parser.Parse();
    }

    public static bool IsIdentifier(string text) {
        if (string.IsNullOrWhiteSpace(text)) {
            return false;
        }

        if (!char.IsLetter(text[0]) && text[0] != '_') {
            return false;
        }

        return text.All(character => char.IsLetterOrDigit(character) || character == '_');
    }

    private sealed class Parser {
        private readonly Tokenizer _tokenizer;
        private readonly CellAddress _owner;

        public Parser(string text, CellAddress owner) {
            _tokenizer = new Tokenizer(text);
            _owner = owner;
        }

        public ExpressionNode Parse() {
            var expression = ParseComparison();
            if (_tokenizer.Current.Kind != TokenKind.End) {
                throw new InvalidOperationException("Expresion invalida.");
            }

            return expression;
        }

        private ExpressionNode ParseComparison() {
            var expression = ParseAdditive();

            while (_tokenizer.Current.Kind == TokenKind.Operator &&
                   _tokenizer.Current.Text is "<" or "<=" or ">" or ">=" or "=" or "==" or "!=" or "<>") {
                var op = _tokenizer.Current.Text;
                _tokenizer.Advance();
                expression = new BinaryNode(op, expression, ParseAdditive());
            }

            return expression;
        }

        private ExpressionNode ParseAdditive() {
            var expression = ParseMultiplicative();

            while (_tokenizer.Current.Kind == TokenKind.Operator &&
                   _tokenizer.Current.Text is "+" or "-") {
                var op = _tokenizer.Current.Text;
                _tokenizer.Advance();
                expression = new BinaryNode(op, expression, ParseMultiplicative());
            }

            return expression;
        }

        private ExpressionNode ParseMultiplicative() {
            var expression = ParsePower();

            while (_tokenizer.Current.Kind == TokenKind.Operator &&
                   _tokenizer.Current.Text is "*" or "/") {
                var op = _tokenizer.Current.Text;
                _tokenizer.Advance();
                expression = new BinaryNode(op, expression, ParsePower());
            }

            return expression;
        }

        private ExpressionNode ParsePower() {
            var expression = ParseUnary();

            while (_tokenizer.Current.Kind == TokenKind.Operator &&
                   _tokenizer.Current.Text == "^") {
                _tokenizer.Advance();
                expression = new BinaryNode("^", expression, ParseUnary());
            }

            return expression;
        }

        private ExpressionNode ParseUnary() {
            if (_tokenizer.Current.Kind == TokenKind.Operator && _tokenizer.Current.Text is "+" or "-") {
                var op = _tokenizer.Current.Text;
                _tokenizer.Advance();
                return new UnaryNode(op, ParseUnary());
            }

            return ParsePrimary();
        }

        private ExpressionNode ParsePrimary() {
            var token = _tokenizer.Current;

            switch (token.Kind) {
                case TokenKind.Number:
                    _tokenizer.Advance();
                    return new NumberLiteralNode(decimal.Parse(token.Text, CultureInfo.InvariantCulture));

                case TokenKind.String:
                    _tokenizer.Advance();
                    return new StringLiteralNode(token.Text);

                case TokenKind.Identifier:
                    return ParseIdentifierBased();

                case TokenKind.LeftParen:
                    _tokenizer.Advance();
                    var nested = ParseComparison();
                    Expect(TokenKind.RightParen);
                    _tokenizer.Advance();
                    return nested;

                default:
                    throw new InvalidOperationException("Expresion invalida.");
            }
        }

        private ExpressionNode ParseIdentifierBased() {
            var identifier = _tokenizer.Current.Text;
            _tokenizer.Advance();

            if (TryParseReference(identifier, out var address)) {
                return new RelativeReferenceNode(address.Row - _owner.Row, address.Column - _owner.Column);
            }

            if (_tokenizer.Current.Kind == TokenKind.LeftParen) {
                _tokenizer.Advance();
                var arguments = new List<FunctionArgumentNode>();

                if (_tokenizer.Current.Kind != TokenKind.RightParen) {
                    while (true) {
                        arguments.Add(ParseFunctionArgument());
                        if (_tokenizer.Current.Kind != TokenKind.Comma) {
                            break;
                        }

                        _tokenizer.Advance();
                    }
                }

                Expect(TokenKind.RightParen);
                _tokenizer.Advance();
                return new FunctionCallNode(identifier, arguments);
            }

            return new IdentifierNode(identifier);
        }

        private FunctionArgumentNode ParseFunctionArgument() {
            if (_tokenizer.Current.Kind == TokenKind.Identifier &&
                _tokenizer.Peek.Kind == TokenKind.Colon &&
                TryParseReference(_tokenizer.Current.Text, out var start)) {
                _tokenizer.Advance();
                _tokenizer.Advance();

                if (_tokenizer.Current.Kind != TokenKind.Identifier ||
                    !TryParseReference(_tokenizer.Current.Text, out var end)) {
                    throw new InvalidOperationException("Expresion invalida.");
                }

                _tokenizer.Advance();
                return CreateRangeArgument(start, end);
            }

            return new ScalarArgumentNode(ParseComparison());
        }

        private static bool TryParseReference(string tokenText, out CellAddress address) {
            return CellAddress.TryParse(tokenText, out address);
        }

        private FunctionArgumentNode CreateRangeArgument(CellAddress start, CellAddress end) {
            var top = Math.Min(start.Row, end.Row);
            var bottom = Math.Max(start.Row, end.Row);
            var left = Math.Min(start.Column, end.Column);
            var right = Math.Max(start.Column, end.Column);

            return new RangeArgumentNode(
                new RelativeReferenceNode(top - _owner.Row, left - _owner.Column),
                new RelativeReferenceNode(bottom - _owner.Row, right - _owner.Column));
        }

        private void Expect(TokenKind expected) {
            if (_tokenizer.Current.Kind != expected) {
                throw new InvalidOperationException("Expresion invalida.");
            }
        }
    }

    private sealed class Tokenizer {
        private readonly string _text;
        private int _position;

        public Tokenizer(string text) {
            _text = text;
            Current = ReadNext();
            Peek = ReadNext();
        }

        public Token Current { get; private set; }
        public Token Peek { get; private set; }

        public void Advance() {
            Current = Peek;
            Peek = ReadNext();
        }

        private Token ReadNext() {
            while (_position < _text.Length && char.IsWhiteSpace(_text[_position])) {
                _position++;
            }

            if (_position >= _text.Length) {
                return new Token(TokenKind.End, string.Empty);
            }

            var current = _text[_position];

            if (char.IsDigit(current) || (current == '.' && _position + 1 < _text.Length && char.IsDigit(_text[_position + 1]))) {
                return ReadNumber();
            }

            if (char.IsLetter(current) || current == '_') {
                return ReadIdentifier();
            }

            if (current == '"') {
                return ReadString();
            }

            _position++;
            return current switch {
                '(' => new Token(TokenKind.LeftParen, "("),
                ')' => new Token(TokenKind.RightParen, ")"),
                ',' => new Token(TokenKind.Comma, ","),
                ':' => new Token(TokenKind.Colon, ":"),
                '<' when Match('=') => new Token(TokenKind.Operator, "<="),
                '>' when Match('=') => new Token(TokenKind.Operator, ">="),
                '<' when Match('>') => new Token(TokenKind.Operator, "<>"),
                '=' when Match('=') => new Token(TokenKind.Operator, "=="),
                '!' when Match('=') => new Token(TokenKind.Operator, "!="),
                '+' or '-' or '*' or '/' or '^' or '<' or '>' or '=' => new Token(TokenKind.Operator, current.ToString()),
                _ => throw new InvalidOperationException("Token invalido.")
            };
        }

        private bool Match(char expected) {
            if (_position < _text.Length && _text[_position] == expected) {
                _position++;
                return true;
            }

            return false;
        }

        private Token ReadNumber() {
            var start = _position;
            while (_position < _text.Length && (char.IsDigit(_text[_position]) || _text[_position] == '.')) {
                _position++;
            }

            return new Token(TokenKind.Number, _text[start.._position]);
        }

        private Token ReadIdentifier() {
            var start = _position;
            while (_position < _text.Length && (char.IsLetterOrDigit(_text[_position]) || _text[_position] == '_')) {
                _position++;
            }

            return new Token(TokenKind.Identifier, _text[start.._position]);
        }

        private Token ReadString() {
            _position++;
            var start = _position;
            while (_position < _text.Length && _text[_position] != '"') {
                _position++;
            }

            var value = _text[start..Math.Min(_position, _text.Length)];
            if (_position < _text.Length && _text[_position] == '"') {
                _position++;
            }

            return new Token(TokenKind.String, value);
        }
    }

    private readonly record struct Token(TokenKind Kind, string Text);

    private enum TokenKind {
        End,
        Number,
        String,
        Identifier,
        Operator,
        LeftParen,
        RightParen,
        Comma,
        Colon
    }
}
