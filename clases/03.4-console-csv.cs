
var texto = File.ReadAllText(args[0]);
var datos = ParseCSV(texto);
Listar(datos);

Dictionary<string, List<string>> ParseCSV(string texto){
    var lineas = texto.Split(Environment.NewLine);
    List<string> cabeceras = new List<string>();
    List<List<string>> filas = new List<List<string>>();
    for (int i = 0; i < lineas.Length; i++) {
        var campos = lineas[i].Split(',');
        if (i == 0) {
            cabeceras.AddRange(campos);
        } else {
            filas.Add(campos.ToList());
        }
    }

    var datos = cabeceras.ToDictionary(c => c, _ => new List<string>());

    foreach (var fila in filas) {
        for (int i = 0; i < cabeceras.Count; i++) {
            var valor = i < fila.Count ? fila[i] : string.Empty;
            datos[cabeceras[i]].Add(valor);
        }
    }

    return datos;
}

void Listar(Dictionary<string, List<string>> datos) {
    Console.Clear();

    var cabeceras = datos.Keys.ToList();
    var cantidadFilas = datos.Count == 0 ? 0 : datos.First().Value.Count;

    Console.WriteLine($"|{string.Join("|", cabeceras.Select(c => c.PadRight(20).ToUpper()))}|");

    for (int i = 0; i < cantidadFilas; i++) {
        var fila = cabeceras.Select(c => datos[c][i].PadRight(20));
        Console.WriteLine($"|{string.Join("|", fila)}|");
    }
}
