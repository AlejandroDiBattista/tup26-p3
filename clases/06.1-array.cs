using static System.Console;
using System.Collections.Generic;


// // sueldos[2] = 2200; // Modificar el sueldo en la posición 2
// // sueldos[sueldos.Length - 1] = 3500; // Modificar el último sueldo
// // sueldos[^1] = 4000; // Modificar el último sueldo usando índice desde el final

// // void MostrarSueldos(float[] sueldos)
// // {
// //     WriteLine("\nSueldos:");
// //     foreach (var x in sueldos)
// //     {
// //         WriteLine($"- El sueldo es {x}");
// //     }
// // }

// // Clear();
// // WriteLine("Sueldos:");


// // // MostrarSueldos(sueldos);
// // // Array.Sort(sueldos); // Ordenar el arreglo de sueldos

// // // MostrarSueldos(sueldos);
// // // Array.Reverse(sueldos); // Invertir el orden del arreglo de sueldos

// // // MostrarSueldos(sueldos);

// // int OrdenPorXY((int x, int y) a, (int x, int y) b) // -1 si a < b, 0 si a == b, 1 si a > b
// // {
// //     if(a.x == b.x)                  // 1er criterio: comparar por x
// //         return - a.y.CompareTo(b.y);  // 2do criterio: comparar por y
// //     return - a.x.CompareTo(b.x);
// // }

// // int OrdenPorYX((int x, int y) a, (int x, int y) b)
// // {
// //     if(a.y == b.y)                  // 1er criterio: comparar por y
// //         return - a.x.CompareTo(b.x);  // 2do criterio: comparar por x
// //     return - a.y.CompareTo(b.y);
// // }

// // //                0     1     2     3     4
// // float[] miles = [ 1000, 2000, 3000, 4000, 5000];
// // var copia = miles[1..3]; // Desde 1 hasta 3 (sin incluir el 3)

// // copia = miles[0..5]; // Copiar todo el arreglo
// // foreach (var x in copia){ WriteLine($"- {x}"); }

// // copia[0] = 9999; // Modificar el primer elemento de la copia
// // WriteLine("\nDespués de modificar la copia:");
// // foreach (var x in copia){ WriteLine($"- {x}"); }

// // WriteLine("\nEl arreglo original después de modificar la copia:");
// // foreach (var x in miles){ WriteLine($"- {x}"); }


// // (int x, int y)[] puntos = [(30, 20), (20, 30), (20, 10), (10, 20), (10, 10)];

// // Array.Sort(puntos, OrdenPorYX);
// // foreach (var (x, y) in puntos)
// // {
// //     WriteLine($"- Punto: ({x}, {y})");
// // }

// // // Array.Sort(puntos, OrdenPorXY);
// // // Array.Reverse(puntos); // Invertir el orden para obtener el orden deseado
// // // Array.Copy(puntos, 0, puntos, 0, puntos.Length); // Copiar el arreglo ordenado de nuevo a sí mismo
// // // Array.Resize(ref puntos, puntos.Length); // Redimensionar el arreglo para reflejar los cambios


// T Maximo<T>(T[] lista) where T : IComparable<T>
// {
//     T max = lista[0];
//     foreach (var item in lista)
//     {
//         if (item.CompareTo(max) > 0)
//             max = item;
//     }
//     return max;
// }

// (int x, int y)[] puntos = [(30, 20), (20, 30), (20, 10), (10, 20), (10, 10)];

// float[] sueldos = [ 4000, 150, 1200, 2500, 3000 ];
// int[] descuento = [ 10, 20, 15, 5, 25 ];
// var maxSueldo    = Maximo<float>(sueldos);
// var maxDescuento = Maximo<int>(descuento);

// WriteLine($"El sueldo máximo es: {maxSueldo}");
// WriteLine($"El descuento máximo es: {maxDescuento}");

// Array.Sort(sueldos);
// Array.Sort(descuento);

// string[] nombres = [ "Juan", "Ana", "Pedro", "María", "Luis" ];
// Array.Sort(nombres);

// void Mostrar<T>(string titulo, T[] lista)
// {
//     WriteLine($"\n{titulo}:");
//     foreach (var x in lista)
//     {
//         WriteLine($"- {x}");
//     }
// }

// Mostrar<string>("Nombres", nombres);
// Mostrar<float>("Sueldos", sueldos);
// Mostrar<int>("Descuentos", descuento);

// Array.Sort(sueldos);
// Array.Reverse(sueldos); // Invertir el orden para obtener el orden descend

// int[][] matriz = new int[3][];
// matriz[0] = new int[] { 1, 2, 3 };
// matriz[1] = new int[] { 4, 5 };
// matriz[2] = new int[] { 6, 7, 8, 9 };

// matriz[1][0] = 99; // Modificar el primer elemento de la segunda fila

// int[,] matriz2D = new int[3, 4]; // Matriz de 3 filas y 4 columnas
// matriz2D[0, 0] =  1; // Modificar el elemento en la primera fila y primera columna
// matriz2D[1, 2] = 99; // Modificar el elemento en la segunda fila y tercera columna


// Color[,,] cubo = new Color[3,3,3]; // Cubo de 3x3x3
// cubo[0, 0, 0] = Color.Rojo;  // Modificar el elemento en la posición (0, 0, 0)
// cubo[1, 1, 1] = Color.Verde; // Modificar el elemento en la posición (1, 1, 1)
// cubo[2, 2, 2] = Color.Azul;  // Modificar el elemento en la posición (

// Pixel[,] pantalla = new Pixel[1920, 1080]; // Pantalla de 1920x1080 píxeles

// pantalla[0, 0] = new Pixel(255, 0, 0); // Modificar el píxel en la esquina superior izquierda a rojo
// pantalla[100,200].R = 120;

// byte[,,] png = new byte[1920, 1080, 3]; // Imagen PNG de 1920x1080 píxeles con 3 canales (R, G, B)

// png[100, 200, 0] = 255; // Modificar el canal R del píxel en la esquina superior izquierda a rojo

// record Pixel(byte R, byte G, byte B);
// enum Color{ Rojo, Verde, Azul }

int[] pila = new int[1000]; // Pila de 1000 elementos
int cantidad = 0; // Cantidad de elementos en la pila

void Push(int valor) {
    if(cantidad == pila.Length)
    {
        Array.Resize(ref pila, pila.Length * 2 ); // Redimensionar la pila para duplicar su capacidad
    }
    if (cantidad < pila.Length)
    {
        pila[cantidad] = valor; // Agregar el valor a la pila
        cantidad++; // Incrementar la cantidad de elementos
    }
}
int Pop() {
    if (cantidad > 0)
    {
        cantidad--; // Decrementar la cantidad de elementos
        if(cantidad < pila.Length / 2)
        {
            Array.Resize(ref pila, pila.Length / 2 ); // Redimensionar la pila para reducir su capacidad a la mitad
        }
        return pila[cantidad]; // Devolver el valor del elemento superior
    }
    return -1;
}
int[] arreglo = lista.ToArray(); // Convertir la lista a un arreglo

List<int> lista = new List<int>()[10, 20, 30, 40, 50]; // Crear una lista con algunos elementos

foreach (var x in lista)
{
    WriteLine($"- {x}");
}
lista[2]  = 99; // Modificar el elemento en la posición 2
lista[^1] = 88; // Modificar el último elemento usando índice desde el final
WriteLine($"\nDespués de modificar la lista: {lista[3]}" );

lista.Add(60); // Agregar un nuevo elemento al final de la lista
WriteLine($"\nDespués de agregar un nuevo elemento: {lista[5]}" );
lista.RemoveAt(1); // Eliminar el elemento en la posición 1

lista.Insert(1, 77); // Insertar un nuevo elemento en la posición 1
WriteLine($"\nDespués de insertar un nuevo elemento: {lista[1]}" );

for(var i = 0; i < lista.Count; i++)
{
    WriteLine($"- {lista[i]}");
}

for(var i = 0; i < sueldos.Length; i++)
{
    WriteLine($"- {sueldos[i]}");
}
lista.Sort(); // Ordenar la lista
lista.Select(x => x * 2).Where(x => x > 50).SortBy(x => x);

Dictionary<string, int> diccionario = new Dictionary<string, int>();
diccionario["Juan"] = 30; // Agregar un nuevo par clave-valor al diccionario
diccionario["Ana"]  = 25; // Agregar un nuevo par clave-valor al diccionario
diccionario["Pedro"] = 35; // Agregar un nuevo par clave-valor al diccionario
if(diccionario.ContainsKey("Ana"))
{
    WriteLine($"\nLa edad de Ana es: {diccionario["Ana"]}");
}
foreach (var clave in diccionario.Keys)
{
    WriteLine($"- {clave}: {diccionario[clave]}");
}

Stack<int> pila = new Stack<int>();                 // LIFO (Last In, First Out)
pila.Push(10); // Agregar un elemento a la pila
pila.Push(20); // Agregar un elemento a la pila
pila.Push(30); // Agregar un elemento a la pila
WriteLine($"\nEl elemento superior de la pila es: {pila.Peek()}");
WriteLine($"El elemento removido de la pila es: {pila.Pop()}");
WriteLine($"El nuevo elemento superior de la pila es: {pila.Peek()}");

Queue<string> cola = new Queue<string>();           // FIFO (First In, First Out)
cola.Enqueue("Juan"); // Agregar un elemento a la cola
cola.Enqueue("Ana");  // Agregar un elemento a la cola
cola.Enqueue("Pedro"); // Agregar un elemento a la cola
WriteLine($"\nEl elemento al frente de la cola es: {cola.Peek()}");
WriteLine($"El elemento removido de la cola es: {cola.Dequeue()}");
WriteLine($"El nuevo elemento al frente de la cola es: {cola.Peek()}");


(int x, int y)[] puntos = [(30, 20), (20, 30), (20, 10), (10, 20), (10, 10)];
int[] a;
a[0];

List<int> lista = new List<int>()[10, 20, 30, 40, 50];

Dictionary<string, int> diccionario = new Dictionary<string, int>();
diccionario["Juan"] = 30;

HashSet<string> conjunto = new HashSet<string>();
conjunto.Add("Juan");
conjunto.Add("Ana");
conjunto.Add("Pedro");
conjunto.Contains("Ana"); // Verificar si el conjunto contiene un elemento
conjunto.Remove("Pedro"); // Eliminar un elemento del conjunto 
foreach (var x in conjunto)
{
    WriteLine($"- {x}");
}

List<char> chars = new List<char>(){ 'a', 'b', 'c', 'd', 'e' };
