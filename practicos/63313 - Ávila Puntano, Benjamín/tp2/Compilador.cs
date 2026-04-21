class Compilador {
    public static Nodo Parse(string expresion) {

        if (string.IsNullOrEmpty(expresion))
        {
            throw new ArgumentException("La expresión no puede estar vacía.");
        }

        var recorrer = new recorrer(expresion);
        var raiz = recorrer.ParsearExpresion(); 
        recorrer.saltarespacios();
     
         if(!recorrer.eof)  
         {
            throw new ArgumentException("Expresión no válidad");

         }
         return raiz;
         
        
    }
    private class recorrer
    {
        private readonly string expresion;
        private int posicion;

        public recorrer(string expresion,int posicion)
        {
            this.expresion = expresion;
            this.posicion = 0;
        }

    public bool eof => posicion >= expresion.Length; // Verifica si se ha llegado al final de la expresión
    private char peek() => eof ? '\0' : expresion[posicion]; // Devuelve el siguiente carácter sin avanzar la posición
    private char next() => eof ? '\0' : expresion[posicion++]; // Devuelve el siguiente carácter y avanza la posición
    
    public void saltarespacios()
    {
        while (char.IsWhiteSpace(peek()))
        {
            next();
        }
    }

}