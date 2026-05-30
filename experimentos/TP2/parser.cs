using System.Globalization;
using System.Text;
using static System.Console;

EjecutarPruebas();

void EjecutarPruebas() {
    WriteLine("Ejecutando pruebas del motor de planilla...\n");

    var numero = 1;

    Probar(ref numero, "Carga inicial, compilación y evaluación", () => {
        var planilla = new Planilla();
        planilla.CargarDesdeLineas([
            "A1: 10",
            "B1: =A1+2",
            "C1: =B1*3",
            "D1: hola",
            "E1: =SUMA(A1, B1, C1)"
        ]);

        AfirmarNumero(planilla, "A1", 10);
        AfirmarNumero(planilla, "B1", 12);
        AfirmarNumero(planilla, "C1", 36);
        AfirmarCadena(planilla, "D1", "hola");
        AfirmarNumero(planilla, "E1", 58);
    });

    Probar(ref numero, "Detección de circularidad en carga inicial", () => {
        var planilla = new Planilla();
        planilla.CargarDesdeLineas([
            "A1: =B1",
            "B1: =A1"
        ]);

        AfirmarError(planilla, "A1", "#circular");
        AfirmarError(planilla, "B1", "#circular");
    });

    Probar(ref numero, "Reevaluación por demanda al invalidar cache", () => {
        var planilla = new Planilla();
        planilla.CargarDesdeLineas([
            "A1: 1",
            "B1: =A1+1",
            "C1: =B1+1",
            "D1: =C1+1"
        ]);

        planilla.SetCell(DireccionCelda.Parse("A1"), "10");

        AfirmarNumero(planilla, "D1", 13);
        AfirmarNumero(planilla, "C1", 12);
        AfirmarNumero(planilla, "B1", 11);
    });

    Probar(ref numero, "Cambio de dependencias en una fórmula", () => {
        var planilla = new Planilla();
        planilla.CargarDesdeLineas([
            "A1: 10",
            "B1: 20",
            "C1: =A1"
        ]);

        planilla.SetCell(DireccionCelda.Parse("C1"), "=B1");
        planilla.SetCell(DireccionCelda.Parse("A1"), "99");
        AfirmarNumero(planilla, "C1", 20);

        planilla.SetCell(DireccionCelda.Parse("B1"), "77");
        AfirmarNumero(planilla, "C1", 77);
    });

    Probar(ref numero, "Introducción incremental de un ciclo", () => {
        var planilla = new Planilla();
        planilla.CargarDesdeLineas([
            "A1: 1",
            "B1: =A1+1"
        ]);

        planilla.SetCell(DireccionCelda.Parse("A1"), "=B1+1");

        AfirmarError(planilla, "A1", "#circular");
        AfirmarError(planilla, "B1", "#circular");
    });

    Probar(ref numero, "Persistencia a disco y recarga", () => {
        var planilla = new Planilla();
        planilla.CargarDesdeLineas([
            "A1: 5",
            "B1: =A1*2",
            "C1: \"texto persistido"
        ]);

        var path = Path.Combine(Path.GetTempPath(), $"planilla-{Guid.NewGuid():N}.txt");
        try {
            planilla.GuardarEnArchivo(path);

            var recargada = new Planilla();
            recargada.CargarDesdeArchivo(path);

            AfirmarNumero(recargada, "B1", 10);
            AfirmarCadena(recargada, "C1", "texto persistido");
        } finally {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }
    });

    WriteLine($"\nTodas las pruebas pasaron. Total: {numero - 1} grupos.");
}

void Probar(ref int numero, string descripcion, Action accion) {
    WriteLine($"{numero}. {descripcion}");
    accion();
    numero++;
}

void Afirmar(bool condicion, string mensaje) {
    if (!condicion) {
        throw new InvalidOperationException(mensaje);
    }
}

void AfirmarNumero(Planilla planilla, string direccion, double esperado) {
    var valor = planilla.ObtenerValor(DireccionCelda.Parse(direccion));
    Afirmar(valor is NumeroValor numero && Math.Abs(numero.Valor - esperado) < 0.0000001,
        $"Se esperaba {esperado} en {direccion}, pero se obtuvo {valor}.");
}

void AfirmarCadena(Planilla planilla, string direccion, string esperado) {
    var valor = planilla.ObtenerValor(DireccionCelda.Parse(direccion));
    Afirmar(valor is CadenaValor(var texto) && texto == esperado,
        $"Se esperaba '{esperado}' en {direccion}, pero se obtuvo {valor}.");
}

void AfirmarError(Planilla planilla, string direccion, string esperado) {
    var valor = planilla.ObtenerValor(DireccionCelda.Parse(direccion));
    Afirmar(valor is ErrorValor(var codigo) && codigo == esperado,
        $"Se esperaba el error {esperado} en {direccion}, pero se obtuvo {valor}.");
}

readonly record struct DireccionCelda(int Fila, int Columna) {
    public bool IsValid => Fila >= 0 && Columna >= 0;

    public static DireccionCelda Parse(string texto) {
        if (!TryParse(texto, out var direccion)) {
            throw new FormatException($"Dirección inválida: {texto}");
        }

        return direccion;
    }

    public static bool TryParse(string? texto, out DireccionCelda direccion) {
        direccion = default;
        if (string.IsNullOrWhiteSpace(texto)) {
            return false;
        }

        texto = texto.Trim().ToUpperInvariant();
        var indice = 0;
        var columna = 0;

        while (indice < texto.Length && char.IsLetter(texto[indice])) {
            columna = columna * 26 + (texto[indice] - 'A' + 1);
            indice++;
        }

        if (indice == 0 || indice == texto.Length) {
            return false;
        }

        if (!int.TryParse(texto[indice..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var filaUsuario) || filaUsuario <= 0) {
            return false;
        }

        direccion = new DireccionCelda(filaUsuario - 1, columna - 1);
        return true;
    }

    public override string ToString() => $"{NombreColumna(Columna)}{Fila + 1}";

    private static string NombreColumna(int columna) {
        if (columna < 0) {
            return "?";
        }

        var numero = columna + 1;
        var sb = new StringBuilder();

        while (numero > 0) {
            numero--;
            sb.Insert(0, (char)('A' + (numero % 26)));
            numero /= 26;
        }

        return sb.ToString();
    }
}

abstract record ValorCelda;
record VacioValor : ValorCelda {
    public static readonly VacioValor Instance = new();
    public override string ToString() => string.Empty;
}
record NumeroValor(double Valor) : ValorCelda {
    public override string ToString() => Valor.ToString("G", CultureInfo.InvariantCulture);
}
record CadenaValor(string Texto) : ValorCelda {
    public override string ToString() => Texto;
}
record ErrorValor(string Codigo) : ValorCelda {
    public static readonly ErrorValor Error = new("#error");
    public static readonly ErrorValor Circular = new("#circular");
    public static readonly ErrorValor Ref = new("#ref");

    public override string ToString() => Codigo;
}

sealed class Celda {
    public DireccionCelda Direccion { get; }
    public string RawInput { get; private set; } = string.Empty;
    public Nodo Compilado { get; private set; } = new Vacio();
    public ValorCelda Valor { get; private set; } = VacioValor.Instance;

    public Celda(DireccionCelda direccion) {
        Direccion = direccion;
    }

    public void ActualizarContenido(string rawInput, Nodo compilado) {
        RawInput = rawInput;
        Compilado = compilado;
    }

    public void SetValor(ValorCelda valor) {
        Valor = valor;
    }

    public bool EsHuerfana() =>
        string.IsNullOrWhiteSpace(RawInput) &&
        Compilado is Vacio;
}

sealed class Planilla {
    private readonly Dictionary<DireccionCelda, Celda> _celdas = [];
    private readonly Dictionary<DireccionCelda, ValorCelda> _cache = [];
    private readonly Evaluador _evaluador;

    public Planilla() {
        _evaluador = new Evaluador(this);
    }

    public void CargarDesdeArchivo(string path) {
        CargarDesdeLineas(File.ReadAllLines(path));
    }

    public void GuardarEnArchivo(string path) {
        var lineas = _celdas
            .Values
            .Where(celda => !string.IsNullOrWhiteSpace(celda.RawInput))
            .OrderBy(celda => celda.Direccion.Fila)
            .ThenBy(celda => celda.Direccion.Columna)
            .Select(celda => $"{celda.Direccion}: {celda.RawInput}");

        File.WriteAllLines(path, lineas);
    }

    public void CargarDesdeLineas(IEnumerable<string> lineas) {
        _celdas.Clear();
        _cache.Clear();

        foreach (var linea in lineas) {
            if (string.IsNullOrWhiteSpace(linea)) {
                continue;
            }

            var (direccion, rawInput) = ParsearLinea(linea);
            var nodo = PrepararEntrada(rawInput);
            var celda = ObtenerOCrearCelda(direccion);
            celda.ActualizarContenido(rawInput, nodo);
        }

        LimpiarHuerfanas();
    }

    public void SetCell(DireccionCelda direccion, string rawInput) {
        if (string.IsNullOrWhiteSpace(rawInput)) {
            _celdas.Remove(direccion);
            InvalidarCache();
            return;
        }

        var celda = ObtenerOCrearCelda(direccion);
        var nodo = PrepararEntrada(rawInput);
        celda.ActualizarContenido(rawInput, nodo);
        InvalidarCache();
        LimpiarHuerfanas();
    }

    public ValorCelda ObtenerValor(DireccionCelda direccion) {
        return EvaluarCelda(direccion, []);
    }

    public Celda ObtenerCelda(DireccionCelda direccion) => ObtenerOCrearCelda(direccion);

    private static (DireccionCelda Direccion, string RawInput) ParsearLinea(string linea) {
        var indice = linea.IndexOf(':');
        if (indice < 0) {
            throw new FormatException($"Línea inválida: {linea}");
        }

        var direccionTexto = linea[..indice].Trim();
        var rawInput = linea[(indice + 1)..].TrimStart();
        return (DireccionCelda.Parse(direccionTexto), rawInput);
    }

    private Nodo PrepararEntrada(string rawInput) {
        try {
            return CompiladorPlanilla.Compilar(rawInput);
        } catch {
            return new ErrorNodo("#error");
        }
    }

    internal ValorCelda EvaluarCelda(DireccionCelda direccion, HashSet<DireccionCelda> pila) {
        if (_cache.TryGetValue(direccion, out var valorCacheado)) {
            return valorCacheado;
        }

        if (!direccion.IsValid) {
            return ErrorValor.Ref;
        }

        if (!_celdas.TryGetValue(direccion, out var celda)) {
            return VacioValor.Instance;
        }

        if (!pila.Add(direccion)) {
            return ErrorValor.Circular;
        }

        try {
            var valor = _evaluador.Evaluar(celda.Compilado, pila);
            _cache[direccion] = valor;
            celda.SetValor(valor);
            return valor;
        } finally {
            pila.Remove(direccion);
        }
    }

    private Celda ObtenerOCrearCelda(DireccionCelda direccion) {
        if (!_celdas.TryGetValue(direccion, out var celda)) {
            celda = new Celda(direccion);
            _celdas[direccion] = celda;
        }

        return celda;
    }

    private void LimpiarHuerfanas() {
        var huerfanas = _celdas
            .Where(par => par.Value.EsHuerfana())
            .Select(par => par.Key)
            .ToList();

        foreach (var direccion in huerfanas) {
            _celdas.Remove(direccion);
        }
    }

    private void InvalidarCache() {
        _cache.Clear();
        foreach (var celda in _celdas.Values) {
            celda.SetValor(VacioValor.Instance);
        }
    }
}

sealed class Evaluador {
    private readonly Planilla _planilla;

    public Evaluador(Planilla planilla) {
        _planilla = planilla;
    }

    public ValorCelda Evaluar(Nodo nodo, HashSet<DireccionCelda> pila) {
        return nodo switch {
            Vacio => VacioValor.Instance,
            ErrorNodo => ErrorValor.Error,
            Numero(var valor) => new NumeroValor(valor),
            Cadena(var valor) => new CadenaValor(valor),
            Direccion(_, var fila, var columna) => EvaluarReferencia(new DireccionCelda(fila, columna), pila),
            Unario(var operador, var operando) => EvaluarUnario(operador, operando, pila),
            Binario(var operador, var izquierdo, var derecho) => EvaluarBinario(operador, izquierdo, derecho, pila),
            Funcion(var nombre, var argumentos) => EvaluarFuncion(nombre, argumentos, pila),
            _ => ErrorValor.Error
        };
    }

    private ValorCelda EvaluarReferencia(DireccionCelda direccion, HashSet<DireccionCelda> pila) {
        if (!direccion.IsValid) {
            return ErrorValor.Ref;
        }

        return _planilla.EvaluarCelda(direccion, pila);
    }

    private ValorCelda EvaluarUnario(char operador, Nodo operando, HashSet<DireccionCelda> pila) {
        var valorOperando = Evaluar(operando, pila);
        if (!TryComoNumero(valorOperando, out var numero, out var error)) {
            return error!;
        }

        return operador switch {
            '+' => new NumeroValor(numero),
            '-' => new NumeroValor(-numero),
            _ => ErrorValor.Error
        };
    }

    private ValorCelda EvaluarBinario(char operador, Nodo izquierdo, Nodo derecho, HashSet<DireccionCelda> pila) {
        var valorIzquierdo = Evaluar(izquierdo, pila);
        if (valorIzquierdo is ErrorValor errorIzquierdo) {
            return errorIzquierdo;
        }

        var valorDerecho = Evaluar(derecho, pila);
        if (valorDerecho is ErrorValor errorDerecho) {
            return errorDerecho;
        }

        if (!TryComoNumero(valorIzquierdo, out var numeroIzquierdo, out var errorNumeroIzquierdo)) {
            return errorNumeroIzquierdo!;
        }

        if (!TryComoNumero(valorDerecho, out var numeroDerecho, out var errorNumeroDerecho)) {
            return errorNumeroDerecho!;
        }

        return operador switch {
            '+' => new NumeroValor(numeroIzquierdo + numeroDerecho),
            '-' => new NumeroValor(numeroIzquierdo - numeroDerecho),
            '*' => new NumeroValor(numeroIzquierdo * numeroDerecho),
            '/' when Math.Abs(numeroDerecho) < 0.0000000001 => ErrorValor.Error,
            '/' => new NumeroValor(numeroIzquierdo / numeroDerecho),
            _ => ErrorValor.Error
        };
    }

    private ValorCelda EvaluarFuncion(string nombre, List<Nodo> argumentos, HashSet<DireccionCelda> pila) {
        var valores = new List<double>();

        foreach (var argumento in argumentos) {
            var valor = Evaluar(argumento, pila);
            if (!TryComoNumero(valor, out var numero, out var error)) {
                return error!;
            }

            valores.Add(numero);
        }

        return nombre.ToUpperInvariant() switch {
            "SUMA" => new NumeroValor(valores.Sum()),
            "MIN" when valores.Count > 0 => new NumeroValor(valores.Min()),
            "MAX" when valores.Count > 0 => new NumeroValor(valores.Max()),
            _ => ErrorValor.Error
        };
    }

    private static bool TryComoNumero(ValorCelda valor, out double numero, out ErrorValor? error) {
        switch (valor) {
            case NumeroValor(var n):
                numero = n;
                error = null;
                return true;
            case CadenaValor(var texto) when double.TryParse(texto, NumberStyles.Float, CultureInfo.InvariantCulture, out var n):
                numero = n;
                error = null;
                return true;
            case VacioValor:
                numero = 0;
                error = null;
                return true;
            case ErrorValor err:
                numero = 0;
                error = err;
                return false;
            default:
                numero = 0;
                error = ErrorValor.Error;
                return false;
        }
    }
}

static class CompiladorPlanilla {
    public static Nodo Compilar(string expresion) {
        expresion ??= string.Empty;
        expresion = expresion.Trim();

        if (expresion.Length == 0) {
            return new Vacio();
        }

        if (!expresion.StartsWith('=')) {
            if (expresion.StartsWith('"')) {
                return new Cadena(expresion.Length > 1 ? expresion[1..] : string.Empty);
            }

            if (double.TryParse(expresion, NumberStyles.Float, CultureInfo.InvariantCulture, out var numero)) {
                return new Numero(numero);
            }

            return new Cadena(expresion);
        }

        var pos = 1;

        bool IsEnd() => pos >= expresion.Length;
        char Next() => !IsEnd() ? expresion[pos] : '\0';
        char Consume() => !IsEnd() ? expresion[pos++] : '\0';

        void SkipWhitespace() {
            while (char.IsWhiteSpace(Next())) {
                Consume();
            }
        }

        bool Match(char expected) {
            SkipWhitespace();
            return Next() == expected;
        }

        bool ConsumeIf(char expected) {
            if (!Match(expected)) {
                return false;
            }

            Consume();
            return true;
        }

        char Expect(char expected) {
            SkipWhitespace();
            if (Next() == expected) {
                return Consume();
            }

            throw new FormatException($"Se esperaba '{expected}'");
        }

        void ExpectEnd() {
            SkipWhitespace();
            if (!IsEnd()) {
                throw new FormatException($"Token inesperado: {Next()}");
            }
        }

        bool EsNumero() {
            SkipWhitespace();
            return char.IsDigit(Next());
        }

        bool EsLetra(char c) => char.IsLetter(c);

        double LeerNumero() {
            SkipWhitespace();

            var numero = "";
            var tienePunto = false;

            while (char.IsDigit(Next()) || (!tienePunto && Next() == '.')) {
                if (Next() == '.') {
                    tienePunto = true;
                }
                numero += Consume();
            }

            if (numero.Length == 0) {
                throw new FormatException("Se esperaba un número");
            }

            if (!double.TryParse(numero, NumberStyles.Float, CultureInfo.InvariantCulture, out var salida)) {
                throw new FormatException($"Número no válido: {numero}");
            }

            return salida;
        }

        string LeerCadenaEnFormula() {
            SkipWhitespace();
            Expect('"');

            var texto = "";
            while (!IsEnd() && Next() != '"') {
                texto += Consume();
            }

            if (IsEnd()) {
                throw new FormatException("Se esperaba '\"' de cierre");
            }

            Consume();
            return texto;
        }

        string LeerIdentificador() {
            SkipWhitespace();
            if (!EsLetra(Next())) {
                throw new FormatException("Se esperaba un identificador");
            }

            var identificador = "";
            while (EsLetra(Next())) {
                identificador += Consume();
            }

            return identificador.ToUpperInvariant();
        }

        List<Nodo> Argumentos() {
            var resultado = new List<Nodo>();
            if (Match(')')) {
                return resultado;
            }

            resultado.Add(ExpresionNodo());
            while (ConsumeIf(',')) {
                resultado.Add(ExpresionNodo());
            }

            return resultado;
        }

        Nodo ExpresionNodo() => Termino();

        Nodo Termino() {
            var resultado = Factor();

            while (Match('+') || Match('-')) {
                var operador = Consume();
                var derecho = Factor();
                resultado = new Binario(operador, resultado, derecho);
            }

            return resultado;
        }

        Nodo Factor() {
            var resultado = Unario();

            while (Match('*') || Match('/')) {
                var operador = Consume();
                var derecho = Unario();
                resultado = new Binario(operador, resultado, derecho);
            }

            return resultado;
        }

        Nodo Unario() {
            if (ConsumeIf('+')) {
                return Unario();
            }

            if (ConsumeIf('-')) {
                return new Unario('-', Unario());
            }

            return Primario();
        }

        Nodo Primario() {
            if (Match('(')) {
                Consume();
                var resultado = ExpresionNodo();
                Expect(')');
                return resultado;
            }

            if (EsNumero()) {
                return new Numero(LeerNumero());
            }

            if (Match('"')) {
                return new Cadena(LeerCadenaEnFormula());
            }

            if (EsLetra(Next())) {
                var nombre = LeerIdentificador();

                if (char.IsDigit(Next())) {
                    var digitos = "";
                    while (char.IsDigit(Next())) {
                        digitos += Consume();
                    }

                    var direccionTexto = nombre + digitos;
                    var direccion = DireccionCelda.Parse(direccionTexto);
                    return Direccion.Desde(direccion);
                }

                if (ConsumeIf('(')) {
                    var argumentos = Argumentos();
                    Expect(')');
                    return new Funcion(nombre, argumentos);
                }

                throw new FormatException($"Se esperaba '(' después del identificador {nombre}");
            }

            throw new FormatException($"Token inesperado: {Next()}");
        }

        var formula = ExpresionNodo();
        ExpectEnd();
        return formula;
    }

    public static string Imprimir(Nodo nodo) {
        return nodo switch {
            Vacio => string.Empty,
            ErrorNodo(var mensaje) => mensaje,
            Numero(var valor) => valor.ToString(CultureInfo.InvariantCulture),
            Cadena(var valor) => $"\"{valor}\"",
            Direccion(var nombre, _, _) => nombre,
            Unario(var operador, var operando) => $"{operador}{Imprimir(operando)}",
            Binario(var operador, var izquierdo, var derecho) => $"({Imprimir(izquierdo)} {operador} {Imprimir(derecho)})",
            Funcion(var nombre, var argumentos) => $"{nombre}({string.Join(", ", argumentos.Select(Imprimir))})",
            _ => throw new InvalidOperationException("Nodo desconocido")
        };
    }

    public static List<DireccionCelda> ObtenerReferencias(Nodo nodo) {
        var referencias = new HashSet<DireccionCelda>();

        void Visitar(Nodo actual) {
            switch (actual) {
                case Direccion(_, var fila, var columna):
                    referencias.Add(new DireccionCelda(fila, columna));
                    break;
                case Unario(_, var operando):
                    Visitar(operando);
                    break;
                case Binario(_, var izquierdo, var derecho):
                    Visitar(izquierdo);
                    Visitar(derecho);
                    break;
                case Funcion(_, var argumentos):
                    foreach (var argumento in argumentos) {
                        Visitar(argumento);
                    }
                    break;
            }
        }

        Visitar(nodo);
        return referencias.OrderBy(d => d.Fila).ThenBy(d => d.Columna).ToList();
    }
}

abstract record Nodo;
record Vacio : Nodo;
record ErrorNodo(string Mensaje) : Nodo;
record Numero(double Valor) : Nodo;
record Cadena(string Valor) : Nodo;
record Direccion(string Nombre, int Fila = 0, int Columna = 0) : Nodo {
    public DireccionCelda ACoordenada() => new(Fila, Columna);
    public static Direccion Desde(DireccionCelda direccion) => new(direccion.ToString(), direccion.Fila, direccion.Columna);
}
record Unario(char Operador, Nodo Operando) : Nodo;
record Binario(char Operador, Nodo Izquierdo, Nodo Derecho) : Nodo;
record Funcion(string Nombre, List<Nodo> Argumentos) : Nodo;

// celda     := formula | cadena | numero
// formula   := '=' termino
// termino   := factor ('+' | '-' factor)*
// factor    := unario ('*' | '/' unario)*
// unario    := ('+' | '-') unario | primario
// primario  := numero | direccion | cadena | '(' expresion ')' | identificador '(' argumentos ')'
// direccion := letra+(digito)+
// indentificador := letra+
// argumentos := expresion (',' expresion)*