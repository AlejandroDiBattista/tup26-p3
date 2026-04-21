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

    public bool eof => posicion >= expresion.Length;

    private char 
    }
}