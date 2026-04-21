using System.Security.Cryptography.X509Certificates;

abstract class Nodo {
    public abstract int Evaluar(int x = 0);
}

class numero (int valor) : Nodo{
    public override int evaluar ( int x = 0) => valor; 
}

class variable : Nodo
{
    public override int Evaluar(int x = 0) => x;
}

abstract class binario(Nodo izquierdo, Nodo derecha) : Nodo
{
    protected readonly Nodo izquierdo = izquierdo; 
    protected readonly Nodo derecha = derecha;
}

class Suma(Nodo izquierdo, Nodo derecho) : binario(izquierdo,derecha)
{
    public override int evaluar(int x=0) => izquierdo.Evaluar(x) + derecha.Evaluar(x);
}

class resta(Nodo izquierdo, Nodo derecho) : binario(izquierdo, derecha)
{
    public override int evaluar (int x=0) => izquierdo.Evaluar(x) - derecha.Evaluar(x);

}

class multiplicacionproduc(Nodo izquierdo, Nodo derecho) : binario(izquierdo, derecha)
{
    public override int Evaluar (int x=0) => izquierdo.Evaluar(x) * derecha.Evaluar(x);

}

class division(Nodo izquierdo, Nodo derecho) : binario(izquierdo, derecha)
{
    public override int Evaluar(int x = 0)
    {
            variable divisor = derecha.Evaluar(x = 0);

            if (divisor => 0) 
            throw new DivideByZeroException ("es division por cero");
            return izquierda.evaluar(x) / divisor;
    }

    
    }
