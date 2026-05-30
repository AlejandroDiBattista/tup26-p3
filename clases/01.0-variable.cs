// Top Level Statements (Sentencias de nivel superior) -> Main implicito
// File Based (Basado en archivo): Sin projecto -> Script

// - Nombre en C# -
// Los nombres de variables, funciones, clases, etc. pueden contener letras, numeros y guiones bajos
// No pueden comenzar con un numero (permite unicode, por lo que puede comenzar con una letra de otro idioma)

// <nombre> ::= (<letra> | '_') [<letra> | <digito> | '_']* (un nombre debe comenzar con una letra o guion bajo y puede contener letras, digitos o guiones bajos)

// - Sintaxis general para declarar una variable -
// <declaracion> ::= <tipo> <nombre> = <valor>; // Sintaxis general para declarar una variable 
// <tipo>   ::= int | string | bool | double | char | etc. (tipos de datos primitivos)
// <nombre> ::= cualquier identificador válido
// <valor>  ::= cualquier valor literal o expresión que se pueda asignar al tipo de la variable
// var <nombre> = <valor>; // Sintaxis para declarar una variable con inferencia de tipo

int numero = 5; // Declaracion de variable
var edad   = 30; // Declaracion de variable con inferencia de tipo (variable declaration with type inference)

// Literales -> Valores escritos directamente en el codigo
int numero2 	= 10;           // Literal entero
string texto 	= "Hola";      	// Literal de cadena
int[] numeros 	= {1, 2, 3};  	// Literal de arreglo
int[] pares 	= [2, 4, 6];    // Literal de arreglo
// var impares 	= [1, 3, 5];    // Literal de arreglo con inferencia de tipo

//var int = 20; // Palabra reservada -> No se puede usar como nombre de variable, clase, etc. a menos que se use el prefijo @
var @int = 20;  // Palabra reservada con prefijo @ -> Se puede usar como nombre de variable, clase, etc. aunque sea una palabra reservada

// - Convenciones de nombres (Naming Conventions) -
// camelCase  -> Variables, funciones, etc. 
// PascalCase -> Clases, Interfaces, etc. 

// - Variables - Representan un espacio en memoria donde se puede almacenar un valor que puede cambiar a lo largo del tiempo. 
// Variables por valor -> Almacenan el valor directamente en la variable (tipos de datos primitivos como int, double, bool, etc.)
// Variables por referencia -> Almacenan una referencia a un objeto en memoria (tipos de datos complejos como string, arrays, clases, etc.)

var x = 10;
var y = x; // y obtiene una copia del valor de x (variables por valor)
y = 20;    // Cambiar el valor de y no afecta a x
Console.WriteLine($"x: {x}, y: {y}"); // Salida: x: 10, y: 20

var a = new int[] {1, 2, 3};
var b = a; // b obtiene una referencia al mismo arreglo que a (variables por referencia)
b[0] = 10; // Cambiar el valor del arreglo a través de b afecta a a
Console.WriteLine($"a[0]: {a[0]}, b[0]: {b[0]}"); // Salida: a[0]: 10, b[0]: 10  (ambos a y b apuntan al mismo arreglo en memoria)

var p = new Persona { Nombre = "Adrian", Edad = 30 };
var q = p; // q obtiene una referencia al mismo objeto que p (variables por referencia)
q.Nombre = "Maria"; // Cambiar el valor del objeto a través de q afecta a p
Console.WriteLine($"p.Nombre: {p.Nombre}, q.Nombre: {q.Nombre}"); // Salida: p.Nombre: Maria, q.Nombre: Maria (ambos p y q apuntan al mismo objeto en memoria)

// Tipos de datos primitivos (Variables por valor)
// Tipos numericos.
// Comienza con un numero -> int, double, float, decimal, etc. (numeric types)
// Si contienen un punto decimal -> double, float, decimal (floating-point types)

short s1 = 100;      // Literal entero corto (short literal)
int i1 = 10;         // Literal entero
uint ui1 = 20U;      // Literal entero sin signo (unsigned int literal)
long i2 = 100L;      // Literal entero largo (long literal)
ulong ui2 = 200UL;   // Literal entero largo sin signo (unsigned long literal)
// UInt16 ui5 = 500U;   // Literal entero sin signo de 16 bits (unsigned int literal de 16 bits)
UInt32 ui3 = 300U;   // Literal entero sin signo de 32 bits (unsigned int literal de 32 bits)
UInt128 ui4 = 400UL; // Literal entero sin signo de 128 bits (unsigned long literal de 128 bits)
byte b1 = 255;       // Literal entero de 8 bits (byte literal)

// Prefijo 0x -> Hexadecimal, 0b -> Binario
var i3 = 0xFFFF; 	// Literal entero hexadecimal 
var i4 = 0b1010; 	// Literal entero binario 


// Postfijo define el tipo de entero -> U (unsigned), L (long), UL (unsigned long), etc. 

// Numeros con punto decimal
double d1 = 3.14;    // Literal de punto flotante de doble precisión
float f2 = 2.718f;   // Literal de punto flotante de precisión simple
decimal m1 = 1.618m; // Literal de punto decimal (decimal literal)

var d2 = .14;        // Literal de punto flotante de doble precisión con inferencia de tipo
var d3 = 0.3e5;      // Literal de punto flotante de doble precisión con inferencia de tipo

float f1 = 3.14f;    // Literal de punto flotante de precisión


// Tipos de datos booleanos
bool esVerdadero = true;  // Literal booleano verdadero
bool esFalso = false;     // Literal booleano falso


// Tipos de datos de caracteres
char letraA = 'A';       // Literal de carácter
char letraB = '\u0042';  // Literal de carácter Unicode (B)
char letraC = '\n';      // Literal de carácter de nueva línea

char uno = '1'; // Literal de carácter que representa el dígito 1
int unoInt = uno - '0'; // Convertir el carácter '1' a su valor entero (1)


Console.WriteLine($"Valor entero de '{uno}': {unoInt}"); // Salida: Valor entero de '1': 1

Console.WriteLine($"Tamaños de tipos de datos primitivos:");
Console.WriteLine($"- Tamaño de bool:   {sizeof(bool)} bytes"); // Salida: Tamaño de bool: 1 byte
Console.WriteLine($"- Tamaño de char:   {sizeof(char)} bytes"); // Salida: Tamaño de char: 2 bytes
Console.WriteLine($"- Tamaño de int:    {sizeof(int)} bytes"); // Salida: Tamaño de int: 4 bytes
Console.WriteLine($"- Tamaño de double: {sizeof(double)} bytes"); // Salida: Tamaño de double: 8 bytes

class Persona {
	public string Nombre { get; set; } = string.Empty;
	public int Edad { get; set; }
}

