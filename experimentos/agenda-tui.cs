#:package Terminal.Gui@1.16.3

using Terminal.Gui;

Application.Init();

var contactos = new List<Contacto> {
    new("Ada",      "Lovelace",    "ada@history.dev",           ["+54 381 111-1111"]),
    new("Alan",     "Turing",      "alan@history.dev",          ["+54 381 222-2222"]),
    new("Grace",    "Hopper",      "grace@history.dev",         ["+54 381 333-3333", "+54 381 444-4444"]),
    new("Linus",    "Torvalds",    "linus@linuxfoundation.org", ["+54 381 555-5555"]),
    new("Margaret", "Hamilton",    "margaret@history.dev",      ["+54 381 666-6666"]),
    new("Barbara",  "Liskov",      "barbara@csail.mit.edu",     ["+54 381 777-7777"]),
    new("Donald",   "Knuth",       "donald@stanford.edu",       ["+54 381 888-8888"]),
    new("Edsger",   "Dijkstra",    "edsger@tue.nl",             ["+54 381 999-9999"]),
    new("Ken",      "Thompson",    "ken@bell-labs.com",         ["+54 381 123-4567"]),
    new("Dennis",   "Ritchie",     "dennis@bell-labs.com",      ["+54 381 234-5678"]),
    new("Bjarne",   "Stroustrup",  "bjarne@cpp.org",            ["+54 381 345-6789"]),
    new("James",    "Gosling",     "james@java.com",            ["+54 381 456-7890"]),
    new("Guido",    "van Rossum",  "guido@python.org",          ["+54 381 567-8901"]),
    new("Brendan",  "Eich",        "brendan@javascript.org",    ["+54 381 678-9012"]),
    new("Radia",    "Perlman",     "radia@networking.org",      ["+54 381 789-0123"]),

};

var filtrados = new List<Contacto>();

const int anchoApellido  = 20;
const int anchoNombre    = 20;
const int anchoEmail     = 30;
const int anchoTelefonos = 30;

using var win = new Window("Agenda - Terminal.Gui v1") {
    Width = Dim.Fill(),
    Height = Dim.Fill()
};

var buscar = new TextField {
    X = 10, Y = 1,
    Width = Dim.Fill(2)
};

var lista = new ListView {
    X = 1, Y = 4,
    Width  = Dim.Fill(2),
    Height = Dim.Fill(5)
};

var btnNuevo = new Button {
    Text = "_Nuevo",
    X    = 1, Y = Pos.Bottom(lista) + 1
};

var btnEditar = new Button {
    Text = "_Editar",
    X    = Pos.Right(btnNuevo) + 2, Y = Pos.Top(btnNuevo)
};

var btnSalir = new Button {
    Text = "_Salir",
    X    = Pos.Right(btnEditar) + 2, Y = Pos.Top(btnNuevo)
};

win.Add(
    new Label { Text = "Buscar:", X = 1, Y = 1 },
    buscar,
    new Label { Text = EncabezadoContactos(), X = 1, Y = 3 },
    lista,
    btnNuevo,
    btnEditar,
    btnSalir
);

buscar.SetFocus();

buscar.TextChanged += _ => Refrescar();

btnNuevo.Clicked   += NuevoContacto;

btnEditar.Clicked  += EditarContactoSeleccionado;
btnSalir.Clicked   += Salir;

lista.KeyPress += e => {
    if (e.KeyEvent.Key == Key.Enter) {
        EditarContactoSeleccionado();
        e.Handled = true;
    }
};

win.KeyPress += e => {
    if (e.KeyEvent.Key == Key.Tab) {
        win.FocusNext();
        e.Handled = true;
        return;
    }

    if (e.KeyEvent.Key == (Key.ShiftMask | Key.Tab)) {
        win.FocusPrev();
        e.Handled = true;
        return;
    }

    if (e.KeyEvent.Key == (Key.CtrlMask | Key.N)) {
        NuevoContacto();
        e.Handled = true;
    }

    if (e.KeyEvent.Key == (Key.CtrlMask | Key.E)) {
        EditarContactoSeleccionado();
        e.Handled = true;
    }

    if (e.KeyEvent.Key == (Key.CtrlMask | Key.S) || e.KeyEvent.Key == (Key.CtrlMask | Key.Q)) {
        Salir();
        e.Handled = true;
    }
};

void NuevoContacto() {
    var nuevo = EditarContacto(null);

    if (nuevo is not null) {
        contactos.Add(nuevo);
        Refrescar();
    }
}

void EditarContactoSeleccionado() {
    if (lista.SelectedItem < 0 || lista.SelectedItem >= filtrados.Count)
        return;

    var actual = filtrados[lista.SelectedItem];
    var editado = EditarContacto(actual);

    if (editado is not null) {
        var index = contactos.IndexOf(actual);
        contactos[index] = editado;
        Refrescar();
    }
}

void Salir() {
    Application.RequestStop();
}

Refrescar();
Application.Run(win);
Application.Shutdown();

void Refrescar() {
    var q = buscar.Text?.ToString()?.Trim().ToLowerInvariant() ?? "";

    filtrados = contactos
        .Where(c =>
            c.Nombre.ToLowerInvariant().Contains(q) ||
            c.Apellido.ToLowerInvariant().Contains(q) ||
            c.Email.ToLowerInvariant().Contains(q) ||
            c.Telefonos.Any(t => t.ToLowerInvariant().Contains(q)))
        .OrderBy(c => c.Apellido)
        .ThenBy(c => c.Nombre)
        .ToList();

    lista.SetSource(filtrados.Select(FormatearContacto).ToList());
}

string FormatearContacto(Contacto c) {
    var telefonos = c.Telefonos.Count == 0 ? "sin teléfonos" : string.Join(" / ", c.Telefonos);

    return $"{Recortar(c.Apellido, anchoApellido),-anchoApellido} {Recortar(c.Nombre, anchoNombre),-anchoNombre} {Recortar(c.Email, anchoEmail),-anchoEmail} {Recortar(telefonos, anchoTelefonos),-anchoTelefonos}";
}

string EncabezadoContactos() {
    return $"{"Apellido",-anchoApellido} {"Nombre",-anchoNombre} {"Email",-anchoEmail} {"Teléfonos",-anchoTelefonos}";
}

string Recortar(string texto, int ancho) {
    if (texto.Length <= ancho) {
        return texto;
    }

    return ancho <= 1 ? texto[..ancho] : texto[..(ancho - 1)] + "…";
}

Contacto? EditarContacto(Contacto? original) {
    var dlg = new Dialog(original is null ? "Nuevo contacto" : "Editar contacto", 70, 22);

    dlg.KeyPress += e => {
        if (e.KeyEvent.Key == Key.Tab || e.KeyEvent.Key == Key.Enter) {
            if(e.KeyEvent.Key == (Key.ShiftMask)) {
                dlg.FocusPrev();
            } else {
                dlg.FocusNext();
            }
            e.Handled = true;
        }
    };

    var nombre   = Campo(dlg, "Nombre:", 1,   original?.Nombre ?? "");
    var apellido = Campo(dlg, "Apellido:", 3, original?.Apellido ?? "");
    var email    = Campo(dlg, "Email:", 5,    original?.Email ?? "");

    dlg.Add(new Label {
        Text = "Teléfonos:",
        X = 2, Y = 7
    });

    var telefonoFields = new List<TextField>();

    void AgregarTelefonoField(string valor = "") {
        var input = new TextField {
            X = 14, Y = 7 + telefonoFields.Count,
            Width = Dim.Fill(2),
            Text = valor
        };

        input.TextChanged += _ => {
            var esUltimo = telefonoFields.LastOrDefault() == input;
            var tieneTexto = !string.IsNullOrWhiteSpace(input.Text?.ToString());

            if (esUltimo && tieneTexto) {
                AgregarTelefonoField();
            }
        };

        telefonoFields.Add(input);
        dlg.Add(input);
    }

    foreach (var tel in original?.Telefonos ?? [])
        AgregarTelefonoField(tel);

    AgregarTelefonoField();

    Contacto? resultado = null;

    var btnAceptar = new Button {
        Text = "Aceptar",
        X = 18, Y = 18
    };

    var btnCancelar = new Button {
        Text = "Cancelar",
        X = Pos.Right(btnAceptar) + 2, Y = 18
    };

    btnAceptar.Clicked += () => {
        resultado = new Contacto(
            nombre.Text?.ToString()?.Trim() ?? "",
            apellido.Text?.ToString()?.Trim() ?? "",
            email.Text?.ToString()?.Trim() ?? "",
            telefonoFields
                .Select(t => t.Text?.ToString()?.Trim() ?? "")
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList()
        );

        Application.RequestStop();
    };

    btnCancelar.Clicked += () => {
        Application.RequestStop();
    };

    dlg.Add(btnAceptar, btnCancelar);

    Application.Run(dlg);

    return resultado;
}

TextField Campo(Dialog dlg, string etiqueta, int y, string valor) {
    dlg.Add(new Label {
        Text = etiqueta,
        X = 2, Y = y
    });

    var input = new TextField {
        X = 14, Y = y,
        Width = Dim.Fill(2),
        Text = valor
    };

    dlg.Add(input);

    return input;
}

record Contacto( string Nombre, string Apellido, string Email, List<string> Telefonos );