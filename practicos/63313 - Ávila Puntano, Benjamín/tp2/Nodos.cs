using System.Security.Cryptography.X509Certificates;
// se usa abstract ya que hay varias formas de calcular un nudo : suma,resta, etc.
abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

// creamos una clase en base a nodo y se re define el comportamiento de evaluar
// usamos el override para que se use la version de evaluar de esta clase y no de nodo
class Numero (int valor) : Nodo{
    public override int Evaluar ( int x = 0) => valor; 
}

//similar a la anterior clase, se devuelve directamente el valor que toma X
class Variable : Nodo
{
    public override int Evaluar(int x = 0) => x;
}

//usamos el readonly para que sea inmutable estos dos nodos, binario es abstracto por que solo define la estructura y no las operaciones.
abstract class Binario(Nodo izquierdo, Nodo derecho) : Nodo
{
    protected readonly Nodo izquierdo = izquierdo; 
    protected readonly Nodo derecho = derecho;
}

// suma es un binario donde se usa los nodos izq,der para hacer una suma entre las dos
class Suma(Nodo izquierdo, Nodo derecho) : Binario(izquierdo,derecho)
{
    public override int Evaluar (int x=0) => izquierdo.Evaluar(x) + derecho.Evaluar(x);
}

//resta es un bionario que al igual que suma usa los nodos en una resta
class Resta(Nodo izquierdo, Nodo derecho) : Binario(izquierdo, derecho)
{
    public override int Evaluar (int x=0) => izquierdo.Evaluar(x) - derecho.Evaluar(x);

}

// el producto es un binario que multiplica los dos nodos entre si
class Multiplicacionprod(Nodo izquierdo, Nodo derecho) : Binario(izquierdo, derecho)
{
    public override int Evaluar (int x=0) => izquierdo.Evaluar(x) * derecho.Evaluar(x);

}

// divison es un binario que divide los dos  resultados de los nodos, si el divisor(nodo derecho) es 0 genera un error y si no retorna la division de los dos nodos 
class Division(Nodo izquierdo, Nodo derecho) : Binario(izquierdo, derecho)
{
    public override int Evaluar(int x = 0)
    {
            int divisor = derecho.Evaluar(x);

            if (divisor == 0) 
            throw new DivideByZeroException ("es Division por cero");
            return izquierdo.Evaluar(x) / divisor;
    }

    
    }
    //
    
 class N_negativos(Nodo operando) : Nodo
{
    public override int Evaluar(int x = 0) => -operando.Evaluar(x);
  
}