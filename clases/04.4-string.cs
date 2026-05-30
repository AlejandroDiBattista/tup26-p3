using static System.Console;


var nombre   = "Analia";
var apellido = "Pérez";
var nombreCompleto = $"{nombre} {apellido}";
var nc = "Analia Pérez";
Clear();
WriteLine(nombreCompleto == nc); // True

int[] numeros1 = [1, 2, 3];
int[] numeros2 = [1, 2, 3];

var x = 10.2;
WriteLine("X es " + x.ToString()); // X es 10.2
WriteLine($"X es {x.ToString()}");


int[] pares = [2, 4, 6];
var tmp = string.Join(",", pares); // "2, 4, 6"
var lista = tmp.Split(","); // ["2", " 4", " 6"]
WriteLine($"Los pares son {tmp}"); // Los pares son 2, 4, 6

var chela = "Chela 👩🏻‍🦳";
WriteLine(chela.Length); 
foreach(var c in chela)
    WriteLine(c);
