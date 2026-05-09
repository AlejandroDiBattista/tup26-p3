using System;

class Compilador {
    public static Nodo Parse(string expresion) {
        if (string.IsNullOrWhiteSpace(expresion)) {
            throw new FormatException("Token inesperado");
        }

        // Uso la clase ConstructorAST para procesar la cadena
        var constructor = new ConstructorAST(expresion);
        var arbolRaiz = constructor.ParseExpresion();
        constructor.SaltarEspacios();

        if (!constructor.FinDeTexto) {
            throw new FormatException("Token inesperado");
        }

        return arbolRaiz;
    }

    private class ConstructorAST {
        private readonly string texto;
        private int indice;

        public ConstructorAST(string contenido) {
            texto = contenido;
            indice = 0;
        }

        public bool FinDeTexto => indice >= texto.Length;

        private char VerActual() => FinDeTexto ? '\0' : texto[indice];

        private char Avanzar() => FinDeTexto ? '\0' : texto[indice++];

        public void SaltarEspacios() {
            while (!FinDeTexto && char.IsWhiteSpace(VerActual())) {
                indice++;
            }
        }
        public Nodo ParseExpresion() {
            var nodo = ParseTermino();
            SaltarEspacios();

            while (VerActual() == '+' || VerActual() == '-') {
                char op = Avanzar();
                var derecho = ParseTermino();
                
                nodo = (op == '+') 
                    ? new SumaNodo(nodo, derecho) 
                    : new RestaNodo(nodo, derecho);

                SaltarEspacios();
            }

            return nodo;
        }

        public Nodo ParseTermino() {
            var nodo = ParseFactor();
            SaltarEspacios();

            while (VerActual() == '*' || VerActual() == '/') {
                char op = Avanzar();
                var derecho = ParseFactor();

                nodo = (op == '*') 
                    ? new MultiplicacionNodo(nodo, derecho) 
                    : new DivisionNodo(nodo, derecho);

                SaltarEspacios();
            }

            return nodo;
        }
        public Nodo ParseFactor() {
            SaltarEspacios();
            char c = VerActual();

            // Gestión de operadores unarios
            if (c == '+') {
                Avanzar();
                return new PositivoNodo(ParseFactor());
            }
            if (c == '-') {
                Avanzar();
                return new NegativoNodo(ParseFactor());
            }

            // Paréntesis
            if (c == '(') {
                Avanzar();
                var interior = ParseExpresion();
                SaltarEspacios();
                if (Avanzar() != ')') {
                    throw new FormatException("Se esperaba ')'");
                }
                return interior;
            }

            // Números enteros
            if (char.IsDigit(c)) {
                int inicio = indice;
                while (char.IsDigit(VerActual())) {
                    Avanzar();
                }
                string valorTexto = texto.Substring(inicio, indice - inicio);
                return new NumeroNodo(int.Parse(valorTexto));
            }

            // Variable x o X
            if (char.ToLower(c) == 'x') {
                Avanzar();
                return new VariableNodo();
            }

            throw new FormatException("Token inesperado");
        }
    }
}