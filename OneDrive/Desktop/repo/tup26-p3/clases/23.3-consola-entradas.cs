#!/usr/bin/env -S dotnet run
#:property PublishAot=false

using System.IO;

var entrada = new StreamReader("entrada.txt");
Console.SetIn(entrada);

var salida = new StreamWriter("salida.txt");
salida.AutoFlush = true;
Console.SetOut(salida);

string? linea;
while((linea = Console.ReadLine()) is not null) {
    Console.WriteLine(linea.ToUpper());
}