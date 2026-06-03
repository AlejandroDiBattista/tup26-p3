
IEnumerable<int> Primos() {
    yield return 2;
    yield return 3;
    yield return 5;
    yield return 7;
    yield return 11;
    // ...
}

// IEnumerable<O> Mapear<T, O>(IEnumerable<T> lista, Func<T, O> transformar) {
//     foreach (T item in lista) {
//         yield return transformar(item);
//     }
// }

foreach (int primo in Primos().Mapear( x => 2*x)) {
    Console.WriteLine(primo);
}

static class Extender
{
    extension<T>(IEnumerable<T> lista)
    {
        public IEnumerable<O> Mapear<O>(Func<T, O> transformar) {
            foreach (T item in lista) {
                yield return transformar(item);
            }
        }
    }
}