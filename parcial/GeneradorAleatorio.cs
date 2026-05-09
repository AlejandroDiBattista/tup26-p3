using System;
using System.Collections.Generic;

public static class Aleatorio {
    public static void Main() {
        Console.WriteLine(string.Join(", ", GenerarExamen(12345, 100, 10)));
    }
    
    public static List<int> GenerarExamen(long semilla, int maximo, int cantidad) {
        const long modulo = 2147483647;
        const long multiplicador = 48271;

        var estado = semilla % modulo;

        var numeros = new List<int>();
        for (var numero = 1; numero <= maximo; numero++) {
            numeros.Add(numero);
        }

        for (var i = numeros.Count - 1; i > 0; i--) {
            estado = (estado * multiplicador) % modulo;
            var j = (int)(estado % (i + 1));
            (numeros[i], numeros[j]) = (numeros[j], numeros[i]);
        }

        return numeros.GetRange(0, cantidad);
    }
}
