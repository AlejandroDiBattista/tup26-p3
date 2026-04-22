string a = "100212";
int b = int.Parse(a);
string c = b.ToString();

string d = "3.14";
double e = double.Parse(d);
string f = e.ToString("F2"); // Formato con 2 decimales 

string g = $"El valor de a es {a}, el valor de b es {b.ToString()}, el valor de c es {c}, el valor de d es {d}, el valor de e es {e:F2)} y el valor de f es {f}.";

string h = "Hay " + 5 + " manzanas"; // Concatenación con números, el resultado es "Hay 5 manzanas"

// Lista de formatos de ToString():
// "C" o "c": Formato de moneda
// "D" o "d": Formato decimal (solo para enteros)
// "E" o "e": Formato de notación científica
// "F" o "f": Formato de punto fijo (número de decimales opcional)
// "G" o "g": Formato general
// "N" o "n": Formato numérico con separadores de miles
// "P" o "p": Formato de porcentaje
// "X" o "x": Formato hexadecimal (solo para enteros)   

int n1 = int.Parse("a123"); // Esto lanzará una excepción FormatException porque "a123" no es un número válido.

if (int.TryParse("a123", out int n2)) {
    Console.WriteLine($"La conversión fue exitosa: {n2}");
} else {
    Console.WriteLine("La conversión falló. El valor no es un número válido.");
}

string nombreCompleto = "Pérez, Juan Carlos";
string[] partes = nombreCompleto.Split(", ");

// Perez, Juan Carlos -> ["Pérez", "Juan Carlos"]
// Juan Carlos Pérez -> ["Pérez", "Juan Carlos"]
// Pérez -> ["Pérez", ""]

(string apellido, string nombre) SepararNombreApellido(string nombreCompleto) {
    if(nombreCompleto.Contains(", ")) {
        var partes = nombreCompleto.Split(", ");
        return (partes[0], partes[1]);
    } else  if(nombreCompleto.Contains(" ")) {
        var partes   = nombreCompleto.Split(" ");
        var apellido = partes[^1];
        var nombre   = string.Join(" ", partes[0..^1]);
        return (apellido, nombre);
    } else {
        return (nombreCompleto, "");
    }
}


var resultado = SepararNombreApellido("Pérez, Juan Carlos");
var (apellido, nombre) = SepararNombreApellido("Pérez, Juan Carlos");
Console.WriteLine($"Apellido: {apellido}, Nombre: {nombre}");

(int x, int y) = (10, 20);
int[] par = [10, 20];

var coordenada = (x: 10, y: 20);
var color = (r: 255, g: 0, b: 0);
var pixel = (color r, (int x, int y) coordenada);
pixel.coordenada.x = 15; // Esto es posible porque las tuplas son mutables 

if(coordenada.x > 15) {
    Console.WriteLine("El punto está a la derecha del eje y");
} else {
    Console.WriteLine("El punto está a la izquierda del eje y");
}

(int r, int g, (int x, int y) coordenada) = (255, 0, (10, 20));
coordenada.x = 15; // Esto es posible porque las tuplas son mutables

(string nombre, string apellido, int edad, int telefono) = ("Juan", "Pérez", 30, 555345123);

int Maximo(int[] numeros) {
    int max = numeros[0];
    foreach(int n in numeros) {
        if(n > max) {
            max = n;
        }
    }
    return max;
}

int Minimo(int[] numeros) {
    int min = numeros[0];
    foreach(int n in numeros) {
        if(n < min) {
            min = n;
        }
    }
    return min;
}

(int maximo, int minimo) MaximoMinimo(int[] numeros) {
    int max = numeros[0];
    int min = numeros[0];
    foreach(int n in numeros) {
        if(n > max) {
            max = n;
        }
        if(n < min) {
            min = n;
        }
    }
    return (max, min);
}

var numeros = [5, 3, 8, 1, 4];
var maximo = Maximo(numeros);
var minimo = Minimo(numeros);
var resultado = MaximoMinimo(numeros);
Console.WriteLine($"Máximo: {maximo}, Mínimo: {minimo}");
Console.WriteLine($"Máximo: {resultado.maximo}, Mínimo: {resultado.minimo}");

static (int minimo, int maximo) MinMax(int[] numeros)
{
    if (numeros == null || numeros.Length == 0)
        throw new ArgumentException("El array no puede ser nulo ni vacío.");

    int minimo = numeros[0];
    int maximo = numeros[0];

    foreach (int n in numeros)
    {
        if (n < minimo)
            minimo = n;

        if (n > maximo)
            maximo = n;
    }

    return (minimo, maximo);
}

