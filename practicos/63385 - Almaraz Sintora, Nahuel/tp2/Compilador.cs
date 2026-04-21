class Compilador {
    private string _expresion = "";
    private int _pos = 0;
    public static Nodo Parse(string expresion) {
        var compilador = new Compilador(expresion);
        var nodo = compilador.ParseExpresion();
        compilador.SaltarEspacios();
        if (compilador._pos < compilador._expresion.Length) {
            throw new FormatException($"Token inesperado: '{compilador._expresion[compilador._pos]}'");
        }
        return nodo;
    }
    private Compilador(string expresion) {
        _expresion = expresion;
    }
    private Nodo ParseExpresion() {
        SaltarEspacios();

        if (_pos >= _expresion.Length) {
            throw new FormatException("Token inesperado: entrada vacía.");
        }

        var izquierda = ParseTermino();

        while (true) {
            SaltarEspacios();
            if (_pos >= _expresion.Length) break;

            var op = _expresion[_pos];
            if (op != '+' && op != '-') break;

            _pos++;
            var derecha = ParseTermino();

            izquierda = op == '+'
                ? new SumaNodo(izquierda, derecha)
                : new RestaNodo(izquierda, derecha);
        }

        return izquierda;
    }
}
