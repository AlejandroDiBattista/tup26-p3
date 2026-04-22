
IEnumeracion<int> e = new Range(1, 10);
while (e.Next()) {
    Console.WriteLine(e.Current);
}

var l = new Lista<int>();
l.Agregar(1);
l.Agregar(2);
l.Agregar(3);

IEnumeracion<int> rango = new Range(1, 20);

while (rango.Next()) {
    Console.WriteLine(rango.Current);
}


List<T> ToList<T>(this IEnumeracion<T> source) {
    var list = new List<T>();
    while (source.Next()) {
        list.Add(source.Current);
    }
    return list;
}

void Imprimir<T>(this IEnumeracion<T> source) {
    while (source.Next()) {
        Console.WriteLine(source.Current);
    }
}

public interface IEnumeracion<T> {
    bool Next();
    T Current { get; }
}

public class Lista<T> : IEnumeracion<T> {
    private int index = -1;
    private List<T> source;
    public Lista() {
        source = new List<T>();
    }
    public void Agregar(T item) => source.Add(item);

    public bool Next() => ++index < source.Count;
    public T Current => source[index];
}


// Recorrer una secuencia de números del 1 al 10
public class Range(int start, int count) : IEnumeracion<int> {
    private int index = -1;

    public bool Next() => ++index < count;
    public int Current => start + index;
}

public static class IEnumeracionExtensions {
    extension<T>(IEnumeracion<T> source) {
        public IEnumeracion<T> Where(Func<T, bool> predicate) => new Where<T>(source, predicate);
        public IEnumeracion<TResult> Select<TResult>(Func<T, TResult> selector) => new Select<T, TResult>(source, selector);
        public IEnumeracion<T> Take(int count) => new Take<T>(source, count);
    }
}



public class Where<T>(IEnumeracion<T> source, Func<T, bool> predicate) : IEnumeracion<T> {

    public bool Next() {
        while (source.Next()) {
            if (predicate(source.Current)) {
                return true;
            }
        }
        return false;
    }

    public T Current => source.Current;
}

public class Select<TSource, TResult>(IEnumeracion<TSource> source, Func<TSource, TResult> selector) : IEnumeracion<TResult> {
    public bool Next() {
        return source.Next();
    }

    public TResult Current => selector(source.Current);
}

public class Take<T>(IEnumeracion<T> source, int count) : IEnumeracion<T> {
    private int currentIndex = 0;
    public bool Next() {
        if (currentIndex >= count) {
            return false;
        }
        if (source.Next()) {
            currentIndex++;
            return true;
        }
        return false;
    }

    public T Current => source.Current;
}

