class Compilador {
    private readonly string _input;
    private int _cursor;
    private Compilador(string input) {
        _input = input;
        _cursor = 0;
    }
    private void IgnorarEspacios() {
        while (_cursor < _input.Length && char.IsWhiteSpace(_input[_cursor])) {
            _cursor++;
        }
    }
    public static Nodo Parse(string expresion) {
        throw new NotImplementedException("Implementar el parser para convertir la expresión en un AST.");
    }
}
