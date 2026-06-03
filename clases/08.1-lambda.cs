
int suma(int x, int y)
{
    return x + y;
}
int suma2(int x, int y) => x + y;

int Mostrar(int x){
    Console.Write("El valor es: ");
    Console.WriteLine(x);
}

int min(int x, int y)
{
    if(x < y) 
        return x;
    else 
        return y;
    
}

suma(2, 3);

var numeros = [10, 20, 10, 30, 20];
var num = new List<int>()[10 , 20, 10, 30, 20];

// int Ordenar(int x, int y) => x - y;

Array.Sort(numeros,(int x, int y) => x - y);
Array.Sort(numeros,(int x, int y) => y - x);
Array.Reverse(numeros);

Persona[] agenda = [
    new Persona("Adrián", "Battista"),
    new Persona("María", "García"),
    new Persona("Juan", "Pérez")
];

int CompararPersonas(Persona a, Persona b) {
    int comparacion = string.Compare(a.Apellido, b.Apellido, StringComparison.OrdinalIgnoreCase);
    if (comparacion != 0) { return comparacion; }

    return string.Compare(a.Nombre, b.Nombre, StringComparison.OrdinalIgnoreCase);
}
Array.Sort(agenda, CompararPersonas);


class Persona(string Nombre, string Apellido);
