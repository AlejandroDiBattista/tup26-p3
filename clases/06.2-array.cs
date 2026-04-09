using static System.Console;
using System.Collections.Generic;

using DiccionarioEntero = Dictionary<string, int>;
using Persona = (string nombre, int edad);
using Personas = List<Persona>;

Personas personas = [ ("Juan", 30), ("Ana", 25), ("Pedro", 35) ]; // Lista de personas
foreach(var persona in personas) {
    WriteLine($"- {persona.nombre}: {persona.edad} años");
}

Clear();
// // WriteLine("!");

// // int edad;

// // int[] numeros;

// // numeros = new int[5]; // Crear un arreglo de enteros con 5 elementos

// // numeros[0] = 1000;
// // numeros[1] = 2000;
// // numeros[2] = 3000;
// // numeros[3] = 4000;
// // numeros[4] = 5000;

// // for(var i = 0; i < numeros.Length; i++)
// //     WriteLine($"- El número en la posición {i} es: {numeros[i]}");


// // numeros[numeros.Length - 1] = 9999; // Modificar el último elemento del arreglo
// // numeros[numeros.Length - 2] = 8888; // Modificar el penúltimo elemento del arreglo

// // numeros[^1] = 7777; // Modificar el último elemento del arreglo usando índice desde el final
// // numeros[^2] = 6666; // Modificar el penúltimo elemento del

// // var copia = numeros[1..3]; // Copiar los elementos desde la posición 1 hasta la posición 3 (sin incluir la posición 4)
// // WriteLine("\nCopia del arreglo:");
// // foreach (var x in copia)
// //     WriteLine($"- {x}");

// // var copiaCompleta = numeros[0..5]; // Copiar todo el arreglo
// // WriteLine("\nCopia completa del arreglo:");
// // foreach (var x in copiaCompleta)
// //     WriteLine($"- {x}");

// // var copiarDesdeTercero = numeros[2..]; // Copiar desde la posición 2 hasta el final del arreglo
// // var copiarHastaTercero = numeros[..3]; // Copiar desde el inicio del arreglo hasta la posición 3 (sin incluir la posición 4)
// // var parte = numeros[1..^1]; // Copiar desde la posición 1 hasta la posición antes del último elemento del arreglo


// // string[] nombres = ["Juan", "Ana", "Pedro", "María", "Luis"];
// // nombres[3].ToUpper(); // Convertir el nombre en la posición 3 a mayúsculas

// // int[][] tablero;

// // tablero = new int[3][]; // Crear un arreglo de arreglos de enteros con 3 filas
// // tablero[0] = new int[3]; // Crear la primera fila con 3
// // tablero[0][1] = 1000;

// // float sueldo = 1000;
// // int[] pares = { 2, 4, 6, 8, 10 };
// // int[] impares = [ 2, 3, 5, 7, 9 ];

// // int[][] paresDoble = 
// //     [ 
// //         [2, 4, 10], 
// //         [4, 8, 12],
// //         [1, 2,  3]
// //     ]; // Arreglo de arreglos de enteros

// // paresDoble[1][2] = 9999; // Modificar el elemento en la fila 1, columna 2 del arreglo de arreglos
// // int [,] matriz = 
// //     [ 
// //         [1, 2, 3], 
// //         [4, 5, 6], 
// //         [7, 8, 9] 
// //     ]; // Arreglo bidimensional de enteros

// // matriz[1, 2] = 9999; // Modificar el elemento en la fila 1, columna 2 del arreglo bidimensional

// // (byte r, byte g, byte b)[,] pantalla = // 3x3 (3 colores)
// //     [
// //         [(255,   0,   0), (  0, 255,   0), (  0,   0, 255)],
// //         [(255, 255,   0), (255,   0, 255), (  0, 255, 255)],
// //         [(128, 128, 128), ( 64,   64, 64), (192, 192, 192)]
// //     ];

// // pantalla[0, 1].r = 255;
// // if(pantalla[0, 1].g == 255)
// //     WriteLine("\nEl color en la posición (0, 1) es verde");

// // byte[,,] cubo = // 3x3x3 (3 dimensiones)
// //     [
// //         [ 
// //             [1, 2, 3], 
// //             [4, 5, 6], 
// //             [7, 8, 9] 
// //         ], 
// //         [ 
// //             [10, 11, 12], 
// //             [13, 14, 15], 
// //             [16, 17, 18] 
// //         ], 
// //         [ 
// //             [19, 20, 21], 
// //             [22, 23, 24], 
// //             [25, 26, 27] 
// //         ] 
// //     ];

// // cubo[0, 0, 0] = 255; // Modificar el elemento en la posición (0, 0, 0) del arreglo tridimensional
// // cubo[^1, ^1, 1] = 128; // Modificar el elemento en la posición (2, 2, 2) del arreglo tridimensional usando índices desde el final

// // cubo[0, 0, 0] = 0;
// // cubo[0, 0, 1] = 0;
// // cubo[0, 0, 2] = 0; // Modificar el elemento en la posición (0, 0, 2) del arreglo tridimensional

// int[] stack = new int[5];
// int size = 0; // Índice del elemento superior de la pila

// void Push(int valor) {
//     if (size < stack.Length) {
//         Array.Resize(ref stack, stack.Length * 1.3); // Redimensionar la pila para duplicar su capacidad
//     }
//     stack[size] = valor; // Agregar el valor a la pila
//     size++; // Incrementar el tamaño de la pila
// }

// int Pop() {
//     if (size > 0){
//         size--; // Decrementar el tamaño de la pila
//         return stack[size]; // Devolver el valor del elemento superior de la pila
//     }
//     return -1; // Indicar que la pila está vacía
// }

// bool IsEmpty() => size == 0; // Verificar si la pila está vacía


// Push(10);
// Push(20);
// Push(30);
// while (!IsEmpty()){
//     int valor = Pop();
//     WriteLine($"Valor removido de la pila: {valor}");
// }

// // LIFO (Last In, First Out)   (Pila o Stack) // Carpeta en un escritorio
// // FIFO (First In, First Out)  (Cola o Queue) // Estamos esperando en una fila para comprar algo

// void Append(ref int[] lista, int valor) {
//     Array.Resize(ref lista, lista.Length + 10); // Redimensionar la lista para duplicar su capacidad
//     lista[^1] = valor; // Agregar el valor al final de la lista
// }

// List<int> lista = new List<int>()[10, 20, 30, 40, 50]; // Crear una lista con algunos elementos

// lista[0] = 99; // Modificar el elemento en la posición 0
// lista[^1] = 88; // Modificar el último elemento usando índice desde el final

// for(var i = 0; i < lista.Count; i++) // Count en lugar de Length para listas
// {
//     WriteLine($"- {lista[i]}");
// }

// lista.Add(60); // Agregar un nuevo elemento al final de la lista
// lista.RemoveAt(1); // Eliminar el elemento en la posición 1
// lista.Insert(1, 77); // Insertar un nuevo elemento en la posición 1


// void Push(List<int> lista, int valor) {
//     lista.Add(valor); // Agregar el valor al final de la lista
// }

// int Pop(List<int> lista) {
//     if (lista.Count > 0) {
//         int valor = lista[^1]; // Obtener el valor del último elemento de la lista
//         lista.RemoveAt(lista.Count - 1); // Eliminar el último elemento de la lista
//         return valor; // Devolver el valor del elemento removido
//     }
//     return -1; // Indicar que la lista está vacía
// }

// bool IsEmpty(List<int> lista) => lista.Count == 0; // Verificar si la lista está vacía

// Stack<int> pila = new Stack<int>(); // LIFO (Last In, First Out)
// pila.Push(10); // Agregar un elemento a la pila
// pila.Push(20); // Agregar un elemento a la pila
// pila.Push(30); // Agregar un elemento a la pila
// while(pila.Count > 0){
//     int valor = pila.Pop(); // Remover el elemento superior de la pila
//     WriteLine($"Valor removido de la pila: {valor}");
// }

// Queue<string> cola = new Queue<string>(); // FIFO (First In, First Out)
// cola.Enqueue("Juan"); // Agregar un elemento a la cola
// cola.Enqueue("Ana");  // Agregar un elemento a la cola
// cola.Enqueue("Pedro"); // Agregar un elemento a la cola
// while(cola.Count > 0){
//     string valor = cola.Dequeue(); // Remover el elemento al frente de la cola
//     WriteLine($"Valor removido de la cola: {valor}");
// }

// List<string> strings = new List<string>();
// strings.Add("Hola");
// strings.Add("Mundo");
// strings.Add("C#");
// strings.Add("Programación");
// strings.RemoveAt(1); // Eliminar el elemento en la posición 1 ("Mundo")

float[] sueldos = [ 10000, 2000, 4500, 4000, 5000 ]; // Arreglo de sueldos

// float Maximo(float[] lista) {
//     float maximo = lista[0]; // Inicializar el máximo con el primer elemento del arreglo
//     foreach(var item in lista) {
//         if (item.CompareTo(maximo) > 0) {
//             maximo = item; // Actualizar el máximo si se encuentra un sueldo mayor
//         }
//     }
//     return maximo; // Devolver el sueldo máximo encontrado
// }

// byte Maximo(byte[] lista) {
//     byte maximo = lista[0]; // Inicializar el máximo con el primer elemento del arreglo
//     foreach(var item in lista) {
//         if (item.CompareTo(maximo) > 0) {
//             maximo = item; // Actualizar el máximo si se encuentra un sueldo mayor
//         }
//     }
//     return maximo; // Devolver el sueldo máximo encontrado
// }

Tipo Maximo<Tipo>(Tipo[] lista) where Tipo : IComparable<Tipo> {
    Tipo maximo = lista[0]; // Inicializar el máximo con el primer elemento del arreglo
    foreach(var item in lista) {
        if (item.CompareTo(maximo) > 0) {
            maximo = item; // Actualizar el máximo si se encuentra un sueldo mayor
        }
    }
    return maximo; // Devolver el sueldo máximo encontrado
}

var maximo = Maximo(sueldos);

int[] descuentos = [10, 20, 15, 30, 25]; // Arreglo de descuentos
var maximoDescuento = Maximo(descuentos);



// Sobrecarga de funciones: Definir varias funciones con el mismo nombre pero con diferentes tipos de parámetros
// Overload de funciones: Definir varias funciones con el mismo nombre pero con diferentes tipos de parámetros

int   suma(int a, int b)     => a + b; // Función lambda para sumar dos números
float suma(float a, float b) => a + b; // Función lambda para sumar dos números

suma(1.2, 2.4);


// var x = 100;
// var y = 200;
// var cmp = x.CompareTo(y); // Devuelve un valor negativo si x < y, 0 si x == y, y un valor positivo si x > y
// x > y   // cmp > 0
// x < y   // cmp < 0
// x == y  // cmp == 0
// x != y  // cmp != 0
// x >= y  // cmp >= 0
// x <= y  // cmp <= 0


string nombre = "Juan";

nombre.ToUpper(); // Convertir el nombre a mayúsculas

var nombres = [ "Juan", "Ana", "Pedro", "María", "Luis" ]; // Arreglo de nombres
string.Join(", ", nombres); // Unir los nombres con una coma y un espacio como separador


Array.Sort(sueldos);    // Array.SortFloat(sueldos); // Ordenar el arreglo de sueldos en orden ascendente
Array.Sort(descuentos); // Array.SortInt(descuentos); // Ordenar el arreglo de descuentos en orden ascendente

Persona[] personas = 
    [ 
        new Persona { Nombre = "Juan", Edad = 30 },
        new Persona { Nombre = "Ana", Edad = 25 },
        new Persona { Nombre = "Pedro", Edad = 35 }
    ]; 

Array.Sort(personas);
Array.Sort(sueldos);
Array.Sort(descuentos);


List<string> listaNombres = new List<string>(nombres); // Convertir el arreglo de nombres a una lista
listaNombres.Sort(); // Ordenar la lista de nombres en orden ascendente


DiccionarioEntero diccionario = new DiccionarioEntero();
diccionario["Juan"] = 30; // Agregar un nuevo par clave-valor al

int OrdenarNombre(Persona a, Persona b) => a.nombre.CompareTo(b.nombre); // Comparar los nombres de las personas para determinar su orden

int OrdenarEdad(Persona a, Persona b)
{
    return a.edad.CompareTo(b.edad); // Comparar las edades de las personas para determinar su orden
}

int OrdenarPorEdadYNombre(Persona a, Persona b)
{   //Apellido / Nombre / Edad
    int resultado = a.apellido.CompareTo(b.apellido); // Comparar las edades de las personas
    if (resultado == 0) {
        resultado = a.nombre.CompareTo(b.nombre); // Si las edades son iguales, comparar los nombres
    }
    if(resultado == 0) {
        resultado = a.edad.CompareTo(b.edad); // Si los nombres también son iguales, comparar las edades
    }
    return resultado; // Devolver el resultado de la comparación
}


Persona[] agenda  = 
    [ 
        ("Juan",  30), 
        ("Ana",   45), 
        ("Pedro", 35) 
    ]; // Arreglo de tuplas con nombre y edad

Array.Sort(agenda, (Persona a, Persona b) => a.nombre.CompareTo(b.nombre)); // Ordenar el arreglo de tuplas por el primer elemento (nombre) en orden ascendente
Array.Sort(agenda, OrdenarEdad); // Ordenar el arreglo de tuplas por el segundo elemento (edad) en orden ascendente
Array.Sort(agenda, OrdenarPorEdadYNombre); // Ordenar el arreglo de tuplas por el segundo elemento (edad) en orden ascendente