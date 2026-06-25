

static class Compilador {

    
    public static Nodo Parse(string texto) {
        var tokens = Tokenizar(texto);
        var parser = new Parser(tokens);
        var arbol = parser.ParseExpresion();

    
        if (parser.HayMasTokens())
            throw new Exception($"Token inesperado: '{parser.TokenActual()}'");

        return arbol;
    }

   
    static List<string> Tokenizar(string texto) {
        var lista = new List<string>();
        int i = 0;

        while (i < texto.Length) {
            char c = texto[i];

            if (char.IsWhiteSpace(c)) {
                i++;
                continue;
            }

           
            if (char.IsDigit(c)) {
                int inicio = i;
                while (i < texto.Length && char.IsDigit(texto[i]))
                    i++;
                lista.Add(texto[inicio..i]);
                continue;
            }

           
            if (c == 'x' || c == 'X') {
                lista.Add("x");
                i++;
                continue;
            }

            
            if ("+-*/()".Contains(c)) {
                lista.Add(c.ToString());
                i++;
                continue;
            }

            throw new Exception($"Carácter no reconocido: '{c}'");
        }

        return lista;
    }
}

class Parser(List<string> tokens) {
    int pos = 0;

    public bool HayMasTokens() => pos < tokens.Count;
    public string TokenActual() => tokens[pos];

    string Consumir() => tokens[pos++];

    string Consumir(string esperado) {
        if (!HayMasTokens())
            throw new Exception($"Se esperaba '{esperado}' pero se llegó al final de la expresión.");
        string tok = Consumir();
        if (tok != esperado)
            throw new Exception($"Se esperaba '{esperado}' pero se encontró '{tok}'.");
        return tok;
    }

   
    public Nodo ParseExpresion() {
        var nodo = ParseTermino();

        while (HayMasTokens() && (TokenActual() == "+" || TokenActual() == "-")) {
            string op = Consumir();
            var derecho = ParseTermino();
            nodo = op == "+" ? new SumaNodo(nodo, derecho) : new RestaNodo(nodo, derecho);
        }

        return nodo;
    }

  
    Nodo ParseTermino() {
        var nodo = ParseFactor();

        while (HayMasTokens() && (TokenActual() == "*" || TokenActual() == "/")) {
            string op = Consumir();
            var derecho = ParseFactor();
            nodo = op == "*" ? new MultiplicacionNodo(nodo, derecho) : new DivisionNodo(nodo, derecho);
        }

        return nodo;
    }

  
    Nodo ParseFactor() {
        if (!HayMasTokens())
            throw new Exception("Expresión incompleta, se esperaba un valor o paréntesis.");

        string tok = TokenActual();

     
        if (tok == "+") {
            Consumir();
            return ParseFactor();
        }

  
        if (tok == "-") {
            Consumir();
            return new NegativoNodo(ParseFactor());
        }

    
        if (tok == "(") {
            Consumir("(");
            var expr = ParseExpresion();
            Consumir(")");
            return expr;
        }

 
        if (tok == "x") {
            Consumir();
            return new VariableNodo();
        }

      
        if (int.TryParse(tok, out int numero)) {
            Consumir();
            return new NumeroNodo(numero);
        }

        throw new Exception($"Token inesperado: '{tok}'");
    }
}