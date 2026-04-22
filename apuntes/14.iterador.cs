
using System;
using System.Collections.Generic;
using System.Linq;

class Program {
    static void Main() {
        var randomLista = new RandomLista(20, 1);

        // Con una lista normal, podríamos acceder a los elementos por índice:
        for (var i = 0; i < randomLista.Lista.Count; i++) {
            Console.WriteLine(randomLista.Lista[i]);
        } // Ojo... lista se calcula 2 * Lista.Count veces

        // O también:
        foreach (var numero in randomLista.Lista) {
            Console.WriteLine(numero);
        }

        // Pero esto no es muy eficiente: estamos generando toda la lista aunque solo queramos recorrerla una vez. Además, si queremos generar una lista infinita, no podríamos hacerlo con esta clase.
        var randomEnumerable = new RandomListaEnumerable(20, 1);
        foreach (var numero in randomEnumerable) {
            Console.WriteLine(numero);
        }

        // Con esta clase, cada vez que iteramos, se genera una nueva lista de números aleatorios. Esto es un poco mejor, pero sigue sin ser ideal: estamos generando toda la lista aunque solo queramos recorrerla una vez. Además, si queremos generar una lista infinita, no podríamos hacerlo con esta clase.
        var randomEnumerator = new RandomEnumerator(1);
        foreach (var numero in randomEnumerator.Take(20)) {
            Console.WriteLine(numero);
        }

        // Con esta clase, cada vez que iteramos, se genera un nuevo número aleatorio. Esto es mucho mejor: solo generamos los números que necesitamos, y podemos generar una lista infinita si queremos.
        var randomYield = new RandomYield(1);
        foreach (var numero in randomYield.Take(20)) {
            Console.WriteLine(numero);
        }
    }

    // Generador de números pseudoaleatorios
    // (Algoritmo de Park-Miller: https://en.wikipedia.org/wiki/Lehmer_random_number_generator)
    public static int CalcularSiguiente(int estado) => (int)(((long)estado * 1_103_515_245 + 12_345) & int.MaxValue);
}

class RandomLista {
    List<int> numeros;

    public RandomLista(int cantidad = 10, int semilla = 1) {
        this.numeros = new List<int>();

        int estado = semilla;
        for (int i = 0; i < cantidad; i++) {
            estado = Program.CalcularSiguiente(estado);
            this.numeros.Add(estado % 100);
        }
    }

    public IList<int> Lista => this.numeros.ToList(); // Devuelve una copia de la lista, para evitar que se modifique desde afuera
}


class RandomListaEnumerable : IEnumerable<int> {
    List<int> numeros = new();

    public RandomListaEnumerable(int cantidad = 10, int semilla = 1) {
        int estado = semilla;
        for (int i = 0; i < cantidad; i++) {
            estado = Program.CalcularSiguiente(estado);
            this.numeros.Add(estado % 100);
        }
    }

    public IEnumerator<int> GetEnumerator() => this.numeros.GetEnumerator();
}

// Implementacion manual del enumerador (sin usar List<T>) (Lazy)
class RandomEnumerator : IEnumerator<int> {
    readonly int semilla;
    int estado;
    int actual;

    public RandomEnumerator(int semilla = 1) {
        this.semilla = semilla;
        Reset();
    }

    public int Current => this.actual;

    public bool MoveNext() {
        this.estado = Program.CalcularSiguiente(this.estado);
        this.actual = this.estado % 100;
        return true;
    }

    public void Reset() {
        this.estado = this.semilla;
    }
}

// Implementacion manual enumerable usando Enumerator)
class RandomYield : IEnumerable<int> {
    readonly int semilla;

    public RandomYield(int semilla = 1) {
        this.semilla = semilla;
    }

    public IEnumerator<int> GetEnumerator() {
        int estado = this.semilla;
        while (true) {
            estado = Program.CalcularSiguiente(estado);
            yield return estado % 100;
        }
    }
}
