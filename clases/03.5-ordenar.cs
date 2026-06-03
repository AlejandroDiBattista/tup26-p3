

var contactos = new List<Contacto> {
    new Contacto("Juan", "Pérez", "123456789"),
    new Contacto("Ana", "Gómez", "987654321"),
    new Contacto("Pedro", "López", "555555555"),
    new Contacto("Lucía", "Fernández", "444444444"),
    new Contacto("Carlos", "Ruiz", "222222222"),
    new Contacto("Marta", "Alonso", "777777777"),
    new Contacto("Sofía", "Torres", "111111111"),
    new Contacto("Diego", "Navarro", "666666666")
};

int OrdenAlfabetico(Contacto c1, Contacto c2) {
    var apellidoComparacion = string.Compare(c1.Apellido, c2.Apellido);
    var nombreComparacion   = string.Compare(c1.Nombre, c2.Nombre);
    if (apellidoComparacion != 0) {
        return apellidoComparacion;
    }
    return nombreComparacion;
}

int OrdenTelefonico(Contacto c1, Contacto c2) {
    return string.Compare(c1.Telefono, c2.Telefono);
}

int OrdenTotal(Contacto c1, Contacto c2) {
    var resultado = OrdenAlfabetico(c1, c2);
    if (resultado != 0) {
        return resultado;
    }
    return OrdenTelefonico(c1, c2);
}

contactos.Sort(OrdenAlfabetico);
Listar("CONTACTOS ORDENADOS ALFABETICAMENTE:", contactos);

contactos.Sort(OrdenTelefonico);
Listar("CONTACTOS ORDENADOS POR TELÉFONO:", contactos);

contactos.Sort(OrdenTotal);
Listar("CONTACTOS ORDENADOS POR TELÉFONO Y ALFABETICAMENTE:", contactos);

void Listar(string titulo, List<Contacto> contactos) {
    Console.WriteLine($"\n{titulo}");
    Console.WriteLine(FormatearBorde());
    Console.WriteLine(FormatearEncabezado());
    Console.WriteLine(FormatearBorde());

    foreach (var contacto in contactos) {
        Console.WriteLine(Formatear(contacto));
    }

    Console.WriteLine(FormatearBorde());
}

string Formatear(Contacto contacto) {
    return $"| {contacto.Apellido,-15} | {contacto.Nombre,-15} | {contacto.Telefono,-12} |";
}

string FormatearEncabezado() {
    return $"| {"Apellido".PadRight(15)} | {"Nombre".PadRight(15)} | {"Telefono".PadRight(12)} |";
}

string FormatearBorde() {
    return "+-----------------+-----------------+--------------+";
}

record Contacto(string Nombre, string Apellido, string Telefono);
