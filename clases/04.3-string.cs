using static System.Console;
string nombre = "Juan";
string apellido = "Pérez";

Clear();
WriteLine($"Hola {nombre} {apellido}"); // Hola Juan Pérez
WriteLine(nombre.Length);


var linea ="Alejandro,Di Battista, 58";
var partes = linea.Split(',');
WriteLine(partes[0]); // Alejandro
WriteLine(partes[1]); // Di Battista
WriteLine(partes[2].Trim()); // 58

string[] frutas = ["manzana", "banana", "cereza"];
WriteLine(frutas[0]); // manzana
WriteLine(frutas[1]); // banana
WriteLine(frutas[2]); // cereza
var listaDeFrutas = string.Join(";", frutas);
WriteLine(listaDeFrutas); // manzana;banana;cereza