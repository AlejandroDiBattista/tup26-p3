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

        public recorrer(string expresion)
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

    private Nodo parsexpresion(){
        saltarespacios();
        if (posicion >= expresion.Length)
        {
            throw new ArgumentException("Expresión incompleta.");
        }

var izquierda = parseTermino();
while (true){
    saltarespacios();
    if (posicion >= expresion.Length){break;}
 var operador = expresion[posicion];
    if (operador != '+' && operador != '-'){break;}
    posicion++;
    var derecho = parseTermino();
    izquierda = operador == '+' ? new Suma(izquierda, derecho) : new Resta(izquierda, derecho);
 return izquierda;
}}
private Nodo parseTermino(){
    saltarespacios();
    var izquierda = parseFactor();
    while (true)
            {
                if(posicion >= expresion.Length){break;}
                var operador = expresion[posicion];
                if (operador != '*' && operador != '/'){break;}
                posicion++;
                var derecho = parseFactor();
                izquierda = operador == '*' ? new Multiplicacionprod(izquierda, derecho) : new Division(izquierda, derecho);

                return izquierda;
            }
        }
    }}
    