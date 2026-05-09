class Compilador {
    public static Nodo Parse(string expresion) {
        var parser = new Parser(expresion);
        return parser.Parse();
    }

    private class Parser {
        private readonly string texto;
        private int posicion;

        public Parser(string texto) {
            this.texto = texto ?? "";
            posicion = 0;
        }

        public Nodo Parse() {
            SaltarEspacios();

            if (Fin()) {
                throw new FormatException("Token inesperado: entrada vacía.");
            }

            var resultado = ParseExpresion();

            SaltarEspacios();
            if (!Fin()) {
                throw new FormatException($"Token inesperado: '{Actual()}'.");
            }

            return resultado;
        }

        private Nodo ParseExpresion() {
            var izquierda = ParseTermino();

            while (true) {
                SaltarEspacios();

                if (Coincide('+')) {
                    var derecha = ParseTermino();
                    izquierda = new SumaNodo(izquierda, derecha);
                } else if (Coincide('-')) {
                    var derecha = ParseTermino();
                    izquierda = new RestaNodo(izquierda, derecha);
                } else {
                    break;
                }
            }

            return izquierda;
        }

        private Nodo ParseTermino() {
            var izquierda = ParseFactor();

            while (true) {
                SaltarEspacios();

                if (Coincide('*')) {
                    var derecha = ParseFactor();
                    izquierda = new MultiplicacionNodo(izquierda, derecha);
                } else if (Coincide('/')) {
                    var derecha = ParseFactor();
                    izquierda = new DivisionNodo(izquierda, derecha);
                } else {
                    break;
                }
            }

            return izquierda;
        }

        private Nodo ParseFactor() {
            SaltarEspacios();

            if (Fin()) {
                throw new FormatException("Token inesperado: fin de entrada.");
            }

            if (Coincide('+')) {
                return new PositivoNodo(ParseFactor());
            }

            if (Coincide('-')) {
                return new NegativoNodo(ParseFactor());
            }

            if (Coincide('(')) {
                SaltarEspacios();

                if (Coincide(')')) {
                    throw new FormatException("Token inesperado: ')'.");
                }

                var expresion = ParseExpresion();
                SaltarEspacios();

                if (!Coincide(')')) {
                    throw new FormatException("Se esperaba ')'.");
                }

                return expresion;
            }

            if (char.IsDigit(Actual())) {
                return ParseNumero();
            }

            if (char.ToLowerInvariant(Actual()) == 'x') {
                Avanzar();
                return new VariableNodo();
            }

            throw new FormatException($"Token inesperado: '{Actual()}'.");
        }

        private Nodo ParseNumero() {
            var inicio = posicion;

            while (!Fin() && char.IsDigit(Actual())) {
                Avanzar();
            }

            var textoNumero = texto.Substring(inicio, posicion - inicio);
            return new NumeroNodo(int.Parse(textoNumero));
        }

        private void SaltarEspacios() {
            while (!Fin() && char.IsWhiteSpace(Actual())) {
                Avanzar();
            }
        }

        private bool Coincide(char esperado) {
            if (!Fin() && Actual() == esperado) {
                Avanzar();
                return true;
            }

            return false;
        }

        private char Actual() {
            return texto[posicion];
        }

        private void Avanzar() {
            posicion++;
        }

        private bool Fin() {
            return posicion >= texto.Length;
        }
    }
}