namespace Tup26.AlumnosApp;

enum TipoUmbralPractico {
    LineasTotales,
    LineasAgregadas
}

readonly record struct ConfiguracionPractico(int Numero, TipoUmbralPractico TipoUmbral, int Minimo) {
    public bool ParecePresentado(int lineasTotales, int lineasAgregadas) =>
        TipoUmbral switch {
            TipoUmbralPractico.LineasTotales => lineasTotales >= Minimo,
            TipoUmbralPractico.LineasAgregadas => lineasAgregadas >= Minimo,
            _ => false
        };

    public string DescripcionCriterio =>
        TipoUmbral switch {
            TipoUmbralPractico.LineasTotales => $"al menos {Minimo} líneas totales",
            TipoUmbralPractico.LineasAgregadas => $"al menos {Minimo} líneas fuente totales agregadas respecto del enunciado",
            _ => "criterio no definido"
        };
}

static class PracticosConfig {
    static readonly IReadOnlyDictionary<int, ConfiguracionPractico> configuraciones =
        new Dictionary<int, ConfiguracionPractico> {
            [1] = new(1, TipoUmbralPractico.LineasTotales,   100),
            [2] = new(2, TipoUmbralPractico.LineasAgregadas,  20),
            [3] = new(3, TipoUmbralPractico.LineasAgregadas,  50),
            [4] = new(4, TipoUmbralPractico.LineasAgregadas, 150),
            [5] = new(5, TipoUmbralPractico.LineasAgregadas, 200),
            [6] = new(6, TipoUmbralPractico.LineasAgregadas,  50)
        };

    public static bool TryObtener(int numero, out ConfiguracionPractico configuracion) =>
        configuraciones.TryGetValue(numero, out configuracion);

    public static IReadOnlyList<EnunciadoPracticoDisponible> FiltrarConfigurados(
        IEnumerable<EnunciadoPracticoDisponible> practicos) =>
        practicos.Where(practico => configuraciones.ContainsKey(practico.Numero))
                 .OrderBy(practico => practico.Numero)
                 .ToList();
}
