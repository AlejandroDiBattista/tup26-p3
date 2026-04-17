class Compilador {
    public static Nodo Parse(string expression) {
        if (string.IsNullOrWhiteSpace(expression)) {
            throw new FormatException("Token inesperado");
        }

        var analizador = new Analizador (expression);
        var raiz = analizador.ParseExpresion();
        analizador.SaltarBlancos();

        if (!analizador.EOF) throw new FormatException("Token inesperado");

        return raiz;}

        private class Analizador {
        private readonly string texto;
        private int pos;

        public Analizador(string texto) {
            this.texto = texto;
            this.pos = 0;
        }

        // Indica si se llegó al final del texto.
        public bool EOF => pos >= texto.Length;

        // Devuelve el carácter actual sin consumirlo.
        private char Peek() => EOF ? '\0' : texto[pos];

        // Consume y devuelve el siguiente carácter.
        private char Next() => EOF ? '\0' : texto[pos++];

        // Salta espacios en blanco desde la posición actual.
        public void SaltarBlancos() {
            while (!EOF && char.IsWhiteSpace(Peek())) pos++;
        }
    }
}
