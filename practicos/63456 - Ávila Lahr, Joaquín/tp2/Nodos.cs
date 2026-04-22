abstract class Nodo
{
    public abstract int Evaluar(int x);
}
class NumeroNodo : Nodo
{
    public int Valor;
    public NumeroNodo(int valor) => Valor = valor;
    public override int Evaluar(int x) => Valor;
}
class VariableNodo : Nodo
{
    public override int Evaluar(int x) => x;
}
class NegativoNodo : Nodo
{

}
abstract class NodoBinario : Nodo
{

}
class SumaNodo : NodoBinario
{
 
}
class RestaNodo : NodoBinario
{

}
class MultiplicacionNodo : NodoBinario
{
    
}
class DivisionNodo : NodoBinario
{

}




