class Compilador {
    public static Nodo Parse(string expresion) {

        if (string.IsNullOrEmpty(expresion))
        {
            throw new ArgumentException("La expresión no puede estar vacía.");
        }

        var recorrer = new recorrer(expresion);
        var raiz = recorrer.ParsearExpresion();

        
        throw new NotImplementedException("Implementar el parser para convertir la expresión en un AST.");
    }
}