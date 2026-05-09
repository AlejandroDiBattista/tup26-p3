using System;

class Compilador {
   private static string texto = "";
    private static int pos;

    public static Nodo Parse(string expresion) {
        if (string.IsNullOrWhiteSpace(expresion))
            throw new FormatException("Token inesperado");

        texto = expresion;
        pos = 0;

        var nodo = ParseExpresion();

        SaltarEspacios();

        if (pos < texto.Length)
            throw new FormatException("Token inesperado");

        return nodo;
    }

    private static char Actual => 
        pos < texto.Length ? texto[pos] : '\0';

    private static void Avanzar() => pos++;

    private static void SaltarEspacios() {
        while (char.IsWhiteSpace(Actual))
            Avanzar();

    }

    private static Nodo ParseExpresion() {
        var nodo = ParseTermino();

        while (true) {
            SaltarEspacios();

            if (Actual == '+') {
                Avanzar();
                nodo = new SumaNodo(nodo, ParseTermino());
            } 
            else if (Actual == '-') {
                Avanzar();
                nodo = new RestaNodo(nodo, ParseTermino());
            } 
            else {
                break;
            }
        }

        return nodo;
    }
   
    private static Nodo ParseTermino() {
        var nodo = ParseFactor();

        while (true) {
            SaltarEspacios();

            if (Actual == '*') {
                Avanzar();
                nodo = new MultiplicacionNodo(nodo, ParseFactor());
            } 
            else if (Actual == '/') {
                Avanzar();
                nodo = new DivisionNodo(nodo, ParseFactor());
            } 
            else {
                break;
            }
        }

        return nodo;
    }

    private static Nodo ParseFactor(){
         SaltarEspacios();
         if (Actual == '+'){
            Avanzar();
            return ParseFactor();
        }
        if (Actual == '-'){
            Avanzar();
            return new NegativoNodo(ParseFactor());
        }
        if (Actual == '('){
            Avanzar();
            var nodo = ParseExpresion();
            SaltarEspacios();
            if (Actual != ')')
                throw new FormatException("Se esperaba ')'");
            Avanzar();
            return nodo;
        }
        if (char.IsDigit(Actual)){
            int inicio = pos;
            while (char.IsDigit(Actual))
                Avanzar();
            
            int numero = int.Parse(texto.Substring(inicio, pos - inicio));
            return new NumeroNodo(numero);
        }

        if (Actual == 'x' || Actual == 'X')
        {
            Avanzar();
            return new VariableNodo();
        }
        throw new FormatException("Token inesperado");
    }
}
