# Terminal.Gui: apuntes para construir aplicaciones TUI en C#

> Estos apuntes usan **Terminal.Gui**, la librería de .NET para construir interfaces de terminal modernas. La persistencia se hace con **SQLite** vía **ADO.NET** + **Dapper** + **Dapper.Contrib**, y el ejemplo final soporta importación y exportación a **JSON**.

El objetivo es que al final puedas construir una aplicación básica de gestión con:

- menú desplegable superior;
- ventanas y paneles;
- búsqueda activa;
- edición mediante cajas de diálogo;
- botones, campos de texto y checks;
- manejo de teclado;
- persistencia en SQLite usando Dapper.Contrib como mini-ORM;
- importación y exportación de los datos en JSON.

El ejemplo final completo está en `agenda-terminal-gui.cs` y se ejecuta con:

``` bash
dotnet run agenda-terminal-gui.cs
```

También se puede pasar la ruta del archivo SQLite:

``` bash
dotnet run agenda-terminal-gui.cs -- agenda.db
```

---

## 1. La idea: una interfaz gráfica dentro de la terminal

Una aplicación de consola tradicional imprime texto y lee texto:

``` csharp
Console.Write("Nombre: ");
string nombre = Console.ReadLine() ?? "";
```

Eso alcanza para programas chicos, pero se vuelve incómodo cuando hay muchas opciones, listas, formularios, búsqueda, edición y navegación.

Una **TUI** (*Terminal User Interface*) usa la terminal completa como superficie visual. En lugar de imprimir líneas una debajo de otra, dibuja ventanas, paneles, menús, botones, listas y formularios.

La terminal sigue siendo texto, pero la experiencia deja de ser una secuencia lineal de preguntas y respuestas.

Terminal.Gui resuelve varias tareas difíciles:

- dibujar correctamente en la pantalla;
- redibujar cuando cambia el tamaño de la terminal;
- manejar foco;
- capturar teclado y mouse;
- organizar componentes visuales;
- abrir diálogos modales;
- evitar que el programador tenga que usar `Console.SetCursorPosition` manualmente.

La idea central es esta:

``` text
Application
└── Runnable / Window
    ├── MenuBar
    ├── FrameView
    │   └── ListView
    ├── FrameView
    │   └── Label
    └── Dialog, cuando se abre uno
```

La aplicación no "imprime pantallas". Construye un árbol de objetos visuales. Terminal.Gui se encarga de dibujarlo, mover el foco, recibir eventos y redibujar cuando haga falta.

---

## 2. Filosofía de diseño

Terminal.Gui se parece más a WinForms, WPF o Avalonia que a `Console.WriteLine`.

Sus decisiones de diseño principales son:

### Todo lo visible es un `View`

Un botón, una etiqueta, una lista, una ventana y un diálogo son `View` o derivan de `View`.

Eso significa que todos comparten conceptos:

- posición: `X`, `Y`;
- tamaño: `Width`, `Height`;
- padre visual: `SuperView`;
- hijos visuales: `SubViews`;
- foco;
- eventos;
- dibujo;
- layout.

Ejemplo:

``` csharp
Label label = new() {
    Text = "Buscar:",
    X = 1,
    Y = 2
};

TextField input = new() {
    X = Pos.Right(label) + 1,
    Y = Pos.Top(label),
    Width = Dim.Percent(40)
};
```

### El layout se expresa, no se calcula a mano

En vez de calcular coordenadas con enteros todo el tiempo, Terminal.Gui usa:

- `Pos` para posiciones;
- `Dim` para tamaños.

Eso permite decir:

``` csharp
X = Pos.Right(label) + 1;
Width = Dim.Fill(2);
```

En castellano: "poné este campo una columna a la derecha del label y hacelo ocupar el resto del ancho, dejando dos columnas libres".

### La aplicación es reactiva

No hay un `while` manual preguntando qué quiere hacer el usuario.

Terminal.Gui ejecuta un bucle de eventos:

1.  dibuja la interfaz;
2.  espera una tecla, mouse o evento interno;
3.  manda ese evento al componente con foco;
4.  ejecuta handlers;
5.  redibuja lo que cambió.

Por eso escribimos código como este:

``` csharp
searchField.ValueChanged += (_, _) => RefreshContacts();
saveButton.Accepting += (_, e) => {
    Save();
    e.Handled = true;
};
```

### El teclado es un ciudadano de primera clase

Una buena TUI se puede usar sin mouse. Terminal.Gui soporta:

- foco con Tab;
- menús con F10 o Alt+letra;
- hotkeys con `_`;
- atajos como `Ctrl+S`;
- eventos de teclado;
- comandos.

### La interfaz no debería contener la lógica de negocio

El `Button` no debería saber cómo se persiste un contacto. El botón llama a un método de la ventana, y ese método delega en una clase que maneja datos.

En el ejemplo final hay cuatro responsabilidades separadas:

- `AgendaWindow` organiza la UI y coordina acciones;
- `ContactDialog` edita un contacto;
- `SqliteAgendaStore` persiste contactos en SQLite con Dapper.Contrib;
- `JsonAgendaIO` lee y escribe JSON para importación y exportación;
- `Contacto` representa los datos.

---

## 3. Crear el proyecto

Para clase conviene una app C# de archivo único (file based) con .NET 10. Las dependencias se declaran al principio del `.cs`:

``` csharp
#!/usr/bin/env -S dotnet run
#:package Terminal.Gui@2.0.1
#:property PublishAot=false

using Terminal.Gui.App;       // Application, IApplication, Runnable
using Terminal.Gui.Drawing;   // LineStyle, colores y dibujo
using Terminal.Gui.Input;     // Key, Command
using Terminal.Gui.ViewBase;  // View, Pos, Dim
using Terminal.Gui.Views;     // Window, FrameView, Button, TextField, etc.
```

Luego se ejecuta:

``` bash
dotnet agenda-terminal-gui.cs
```

---

## 4. Ciclo de vida de la aplicación

El ciclo de vida mínimo es:

``` csharp
using( IApplication app = Application.Create().Init()){
    Window mainWindow = new() { Title = "Tutorial Terminal.Gui v2" };
    app.Run(mainWindow);
}
```

Qué pasa ahí:

1.  `Application.Create()` crea una instancia de aplicación.
2.  `Init()` inicializa el driver de terminal, el loop principal y el manejo de input.
3.  `Run(...)` ejecuta una pantalla, ventana o diálogo.
4.  Cuando la pantalla pide detenerse, `Run` termina.
5.  El `using` llama a `Dispose()` y restaura la terminal.

---

## 5. Configuración

Terminal.Gui puede usar configuración para temas, colores y bindings. La configuración debe habilitarse temprano, antes de crear la aplicación.

Ejemplo:

``` csharp
using Terminal.Gui.Configuration;

ConfigurationManager.RuntimeConfig = """
{
  "Theme": "Dark",
  "Application.DefaultKeyBindings": { "Quit": { "All": ["Ctrl+Q"] } }
}
""";

ConfigurationManager.Enable(ConfigLocations.Runtime);

using IApplication app = Application.Create().Init();
```

Para una primera aplicación no es obligatorio configurar nada. De hecho, el CRUD final evita temas personalizados para concentrarse en arquitectura, layout y eventos.

Regla práctica:

- si estás aprendiendo, no configures demasiado;
- si la app ya funciona, agregá tema, colores y bindings globales;
- si algo de configuración no se aplica, revisá que `ConfigurationManager.Enable(...)` ocurra antes de `Application.Create().Init()`.

---

## 6. Vistas como organizador visual

Una `View` puede ser:

- un control concreto: `Label`, `TextField`, `Button`, `CheckBox`;
- un contenedor: `FrameView`, `Window`, `Dialog`;
- una pantalla propia creada por nosotros.

Se agregan vistas con `Add`:

``` csharp
FrameView maestro = new() {
    Title = "Maestro",
    Width = Dim.Percent(50), Height = Dim.Fill()
};

FrameView detalle = new() {
    Title = "Detalle",
    X = Pos.Right(maestro),
    Width = Dim.Fill(), Height = Dim.Fill()
};

mainWindow.Add(maestro, detalle);
```

El panel se vuelve el `SuperView` de la lista. La lista se posiciona dentro del área interna del panel, no contra toda la pantalla.

Esto permite pensar por capas:

``` text
MainWindows
├── MenuBar
├── FrameView "Maestro"
│   ├── Label encabezado
│   └── ListView
├── FrameView "Detalle"
│   └── Markdown o Label multilinea
└── Label de estado
```

Esa estructura visual también ayuda a separar responsabilidades en el código.

---

## 7. Runners y pantallas ejecutables

En Terminal.Gui no se ejecuta cualquier objeto: se ejecuta un objeto que participa del ciclo de vida de la app. Para eso existe `IRunnable`; la clase cómoda para empezar es `Runnable`.

En el ejemplo final:

``` csharp
class Agenda : Window {
    public Agenda() {
        Title = "Agenda";

        Button boton     = new() { Text = "¡Confirmar!", X = Pos.Center(), Y = Pos.Center() };
        Label  resultado = new() { Text = "Resultado: ", X = Pos.Center(), Y = Pos.Bottom(boton) + 1 };

        boton.Accepting += (_,_) => {
            using Confirmar confirmar = new("¿Estás seguro de que quieres confirmar esta acción?");
            App!.Run(confirmar);
            resultado.Text = "Resultado " + (confirmar.Result==1 ? "¡Confirmado!" : "Cancelado.");
        };

        Add(boton);
        Add(resultado);
    }
}

class Confirmar : Dialog { 
    public Confirmar(string Mensaje){
        Title = "Confirmar acción";
        Width = 60; Height = 10;

        Add(new Label() { Text = Mensaje, X = Pos.Center(), Y = 2 });

        AddButton(new Button(){Title = "Cancelar" }); // Resultado = 0
        AddButton(new Button(){Title = "Confirmar"}); // Resultado = 1
    }
}

```

Y se ejecuta así:

``` csharp
using (IApplication app = Application.Create().Init()) {
    app.Run(new Agenda());
}
```
o 
``` csharp
using (IApplication app = Application.Create().Init()) {
    app.Run<Agenda>();
}

```
o 
```csharp
using IApplication app = Application.Create().Init());
using Agenda agenda = new();
app.Run(agenda);
```

Cuando una pantalla necesita abrir un diálogo:

``` csharp
// Crea la caja de dialogo
Confirmar confirmar = new("¿Estás seguro de que quieres confirmar esta acción?");
// 'Apila' la caja de dialogo y espera a que el usuario elija una opción
App!.Run(confirmar);
// Al cerrar el diálogo, el control vuelve a esta línea, y se puede leer el resultado
resultado.Text = "Resultado " + (confirmar.Result==1 ? "¡Confirmado!" : "Cancelado.");
```

`App!.Run(dialog)` abre un loop modal encima del actual. Mientras el diálogo está abierto, la ventana de atrás no recibe input. Cuando el diálogo llama a `App!.RequestStop()`, el control vuelve a la línea siguiente.

---

## 8. Ventanas, paneles y diálogos

Hay tres contenedores que conviene distinguir.

### `Window`

Sirve como ventana principal tradicional. Tiene borde y título.

### `Runnable`

Es útil como pantalla ejecutable. Puede ocupar todo el terminal y contener otros views. En el ejemplo final se usa `Runnable` como raíz.

### `FrameView`

Es un panel con borde y título, ideal para organizar áreas dentro de una pantalla.

``` csharp
FrameView detailsPanel = new() {
    Title = "Detalle",
    X = Pos.Right(listPanel), Y = Pos.Top(listPanel),
    Width = Dim.Fill(),       Height = Dim.Fill(2)
};
```

### `Dialog`

Es una ventana modal. Se usa para edición, confirmaciones o formularios cortos.

``` csharp
class Editar : Dialog<Contacto?> {
    public Editar(){
        Title  = "Editar Contacto";
        Width  = 60; Height = 10;

        // Agrego el campo Nombre
        Label labelNombre       = new() { Text = "  Nombre:", X = 1, Y = 1 };
        TextField inputNombre   = new() { X = Pos.Right(labelNombre) + 1, Y = 1, Width = 30 };
        Add(labelNombre, inputNombre);

        // Agrego el campo Teléfono
        Label labelTelefono     = new() { Text = "Teléfono:", X = 1, Y = 3 };
        TextField inputTelefono = new() { X = Pos.Right(labelTelefono) + 1, Y = 3, Width = 15 };
        Add(labelTelefono, inputTelefono);

        // Si cancelo retorno null
        Button btnCancelar = new() { Title = "Cancelar" };
        btnCancelar.Accepting += (_,_) => Result = null;
        AddButton(btnCancelar);

        // Si confirmo, valido y retorno el contacto
        Button btnConfirmar = new() { Title = "Confirmar" };
        btnConfirmar.Accepting += (_, e) => {
            e.Handled = true; // Evito que el diálogo se cierre automáticamente
            if(string.IsNullOrWhiteSpace(inputNombre.Text) || inputNombre.Text.Length < 3) {
                inputNombre.SetFocus();
                return;
            }

            if(string.IsNullOrWhiteSpace(inputTelefono.Text) || !inputTelefono.Text.ToString()!.All(char.IsDigit)) {
                inputTelefono.SetFocus();
                return;
            }
            
            Result = new Contacto(inputNombre.Text.ToString(), inputTelefono.Text.ToString());
            App!.RequestStop();
        };
        AddButton(btnConfirmar);
    }
}
```

La diferencia conceptual:

- `FrameView` organiza una pantalla;
- `Dialog` interrumpe la pantalla para pedir una decisión o datos;
- `Runnable`/`Window` es lo que corre en `app.Run(...)`.

---

## 9. Layout: ubicación con `Pos` y tamaño con `Dim`

La pantalla de terminal se mide en celdas de caracteres. Una coordenada no es un pixel; es una columna y fila de texto.

Cada `View` tiene:

``` csharp
X      // posición horizontal
Y      // posición vertical
Width  // ancho
Height // alto
```

Pero no conviene pensar que siempre son enteros. En Terminal.Gui son expresiones.

### Posiciones absolutas

``` csharp
X = 1;
Y = 2;
```

Útil para cosas simples.

### Posiciones relativas a otros views

``` csharp
X = Pos.Right(searchLabel) + 1;
Y = Pos.Top(searchLabel);
```

Esto dice: "a la derecha del label, en la misma fila".

### Debajo de otro view

``` csharp
Y = Pos.Bottom(emailLabel) + 1;
```

Muy usado en formularios.

### Anclar al final

``` csharp
Y = Pos.AnchorEnd(1);
```

Sirve para una barra de estado en la última fila.

### Tamaño fijo

``` csharp
Width = 74;
Height = 22;
```

Es razonable para diálogos.

### Porcentaje

``` csharp
Width = Dim.Percent(50);
```

Sirve para partir la pantalla en paneles.

### Rellenar el espacio disponible

``` csharp
Width  = Dim.Fill();
Height = Dim.Fill(2);
```

`Dim.Fill(2)` significa ocupar el espacio disponible dejando 2 celdas libres al final.

Ejemplo completo de formulario:

``` csharp
Label nameLabel = new() { Text = "Nombre:", X = 2, Y = 1 };

TextField nameField = new() {
    X = 14, Y = Pos.Top(nameLabel),
    Width = Dim.Fill(2)
};

Label phoneLabel = new() {
    Text = "Teléfono:",
    X = Pos.Left(nameLabel),    Y = Pos.Bottom(nameLabel) + 1
};

TextField phoneField = new() {
    X = Pos.Left(nameField),    Y = Pos.Top(phoneLabel),
    Width = Dim.Fill(2)
};
```

La ventaja de este estilo es que si movemos `nameLabel`, lo demás sigue acomodándose de manera coherente.

---

## 10. Componentes básicos de entrada

### `Label`

Muestra texto fijo.

``` csharp
new Label { Text = "Buscar:", X = 1, Y = 2 };
```

### `TextField`

Entrada de una línea.

``` csharp
TextField searchField = new() {
    X = 10, Y = 2,
    Width = Dim.Percent(42)
};

searchField.ValueChanged += (_, _) => RefreshContacts();
```

`ValueChanged` es lo que permite búsqueda activa: cada vez que cambia el texto, se recalcula la lista.

### `TextView`

Entrada o visualización de texto multilinea.

``` csharp
TextView notesField = new() {
    X = 14, Y = 11,
    Width = Dim.Fill(2),  Height = 4
};
```

### `Button`

Ejecuta una acción.

``` csharp
Button saveButton = new() {
    Text = "Guardar",
    IsDefault = true
};

saveButton.Accepting += (_, e) => {
    Save();
    e.Handled = true;
};
```

`IsDefault = true` permite que Enter active ese botón cuando el diálogo lo usa como acción principal.

### `CheckBox`

Entrada booleana.

``` csharp
CheckBox favoriteField = new() {
    Text = "_Favorito",
    Value = contact.Favorito ? CheckState.Checked : CheckState.UnChecked
};
```

El estado se lee con `Value`:

``` csharp
bool favorito = favoriteField.Value == CheckState.Checked;
```

---

## 11. Listas y búsqueda activa

`ListView` muestra una colección de filas.

Es cómodo usar `ObservableCollection<string>`:

``` csharp
contactsList.SetSource(new ObservableCollection<string>(rows));
```

Para una agenda, conviene mantener dos listas en memoria:

``` csharp
private List<Contacto> contacts         = new();
private List<Contacto> filteredContacts = new();
```

`contacts` contiene todos los contactos cargados desde la base.

`filteredContacts` contiene solo los que coinciden con la búsqueda actual.

Así se refresca:

``` csharp
private void RefreshContacts(int? selectedId = null) {
    string search        = searchField.Text?.ToString()?.Trim() ?? "";
    bool   onlyFavorites = favoritesOnly.Value == CheckState.Checked;

    filteredContacts = contacts
        .Where(contact => Matches(contact, search, onlyFavorites))
        .OrderByDescending(contact => contact.Favorito)
        .ThenBy(contact => contact.Nombre)
        .ToList();

    contactsList.SetSource(ToObservable(filteredContacts.Select(ContactRow.Format)));
}
```

La lista visual muestra strings, pero la selección real se resuelve usando el mismo índice sobre `filteredContacts`:

``` csharp
private Contacto? GetSelectedContact() {
    int index = contactsList.SelectedItem ?? -1;

    return index >= 0 && index < filteredContacts.Count
        ? filteredContacts[index]
        : null;
}
```

Ese detalle es importante: nunca conviene intentar reconstruir el objeto seleccionado a partir del texto visible.

> **Nota arquitectónica.** Aunque la persistencia ya es SQLite, mantenemos la lista en memoria por dos razones: la búsqueda activa filtra cientos de veces por segundo y no queremos golpear la DB con tantas queries; y la UI necesita un orden estable durante la edición. Cada operación CRUD modifica la DB **y** la lista en memoria, en ese orden.

---

## 12. Cajas de diálogo para edición

Una caja de diálogo de edición tiene tres responsabilidades:

1.  mostrar campos;
2.  validar;
3.  devolver un resultado.

Estructura:

``` csharp
public sealed class ContactDialog : Dialog {
    private readonly TextField nameField;
    private readonly CheckBox  favoriteField;

    public bool      Saved   { get; private set; }
    public Contacto? Contact { get; private set; }
}
```

Al guardar:

``` csharp
private void Save() {
    string name = Clean(nameField.Text);

    if (string.IsNullOrWhiteSpace(name)) {
        MessageBox.Query(App!, "Validación", "El nombre es obligatorio.", "OK");
        nameField.SetFocus();
        return;
    }

    Contact!.Nombre   = name;
    Contact.Favorito  = favoriteField.Value == CheckState.Checked;

    Saved = true;
    App!.RequestStop();
}
```

Puntos importantes:

- el diálogo no toca la base de datos;
- el diálogo no modifica la lista global;
- el diálogo solo devuelve un contacto editado;
- la ventana principal decide qué hacer con ese resultado, incluyendo persistirlo.

Uso desde la ventana:

``` csharp
ContactDialog dialog = new("Editar contacto", selected.Clone());
App!.Run(dialog);

if (dialog.Saved && dialog.Contact is not null) {
    store.Update(dialog.Contact);                                // primero la DB
    int index = contacts.FindIndex(c => c.Id == selected.Id);
    contacts[index] = dialog.Contact;                            // después la memoria
    SetStatus("Contacto actualizado.");
    RefreshContacts(dialog.Contact.Id);
}
```

El `Clone()` evita editar el objeto original mientras el usuario todavía puede cancelar.

---

## 13. Menús desplegables

Un menú superior se arma con:

- `MenuBar`: la barra;
- `MenuBarItem`: cada menú principal;
- `MenuItem`: cada acción.

Ejemplo:

``` csharp
MenuBar menu = new() {
    Menus = [
        new MenuBarItem("_Archivo", [
            new MenuItem("_Importar JSON...", "Ctrl+I", ImportFromJson),
            new MenuItem("_Exportar JSON...", "Ctrl+E", ExportToJson),
            new MenuItem("_Recargar",         "Ctrl+R", ReloadContacts),
            new MenuItem("_Salir",            "Ctrl+Q", RequestExit) ]),
        new MenuBarItem("_Contactos", [
            new MenuItem("_Nuevo",     "F2 / Ctrl+N", NewContact),
            new MenuItem("_Editar",    "F3 / Enter",  EditSelectedContact),
            new MenuItem("E_liminar",  "Del",         DeleteSelectedContact) ])
    ]
};
```

El guion bajo marca la letra rápida:

- `_Archivo` habilita Alt+A;
- `_Importar JSON...` marca la I;
- `_Eliminar` marca la E.

El segundo texto, por ejemplo `"Ctrl+S"`, es ayuda visual. No reemplaza necesariamente al manejo de teclado. En el ejemplo final los atajos se implementan en `OnKeyDown`.

Regla de diseño: el menú no debería tener lógica larga. El menú llama métodos:

``` csharp
new MenuItem("_Importar JSON...", "Ctrl+I", ImportFromJson)
```

La lógica real vive en `ImportFromJson`.

---

## 14. Manejo de teclado

Terminal.Gui ya maneja muchas teclas:

- Tab cambia el foco;
- flechas navegan listas;
- Enter acepta botones o abre elementos;
- F10 activa menú;
- Escape cierra menús o diálogos según contexto.

Cuando queremos atajos propios, podemos sobrescribir `OnKeyDown`.

``` csharp
protected override bool OnKeyDown(Key key) {
    switch (key) {
        case var k when k == Key.F2 || k == Key.N.WithCtrl:
            NewContact();
            return true;

        case var k when k == Key.I.WithCtrl:
            ImportFromJson();
            return true;

        case var k when k == Key.E.WithCtrl:
            ExportToJson();
            return true;

        case var k when k == Key.R.WithCtrl:
            ReloadContacts();
            return true;

        default:
            return base.OnKeyDown(key);
    }
}
```

`return true` significa: "esta tecla ya fue manejada".

`return base.OnKeyDown(key)` permite que Terminal.Gui siga con su comportamiento normal.

Atajos del CRUD final:

``` text
F2 / Ctrl+N     Nuevo contacto
F3              Editar contacto
Enter           Editar, si la lista tiene foco
Delete / Ctrl+D Eliminar contacto
Ctrl+I          Importar desde JSON
Ctrl+E          Exportar a JSON
Ctrl+R          Recargar desde la base
Ctrl+Q          Salir
F4              Foco en búsqueda
```

Un detalle útil:

``` csharp
key == Key.N.WithCtrl
```

Las teclas son objetos `Key`. Los modificadores se expresan con propiedades como `.WithCtrl`, `.WithAlt` y `.WithShift`.

---

## 15. Persistencia con SQLite y Dapper.Contrib

### Por qué este stack

Para una agenda no necesitamos un ORM completo como Entity Framework. Tampoco queremos escribir SQL crudo para cada operación CRUD. Dapper resuelve el medio:

- **ADO.NET** + **`SqliteConnection`** abre la conexión a la base.
- **Dapper** convierte filas en objetos con `Query<T>` y ejecuta SQL con `Execute`.
- **Dapper.Contrib** agrega métodos de extensión `GetAll`, `Insert`, `Update`, `Delete` que generan el SQL básico por convención, basándose en atributos del modelo.

El resultado es un store muy chico: una clase, una conexión, y métodos de una línea para el 90% de los casos.

### El modelo

El modelo se anota con atributos de Dapper.Contrib:

```csharp
#:package Terminal.Gui@2.0.1
#:package Microsoft.Data.Sqlite@9.0.0
#:package Dapper@2.1.35
#:package Dapper.Contrib@2.0.78

using Microsoft.Data.Sqlite;  // SqliteConnection
using Dapper;                 // Execute, Query
using Dapper.Contrib.Extensions; // GetAll, Insert, Update, Delete, [Key], [Table]

using System.Text.Json;       // JsonSerializer
```

```csharp
[Table("Contactos")]
public sealed class Contacto {
    [Key]  public int    Id        { get; set; }
           public string Nombre    { get; set; } = "";
           public string Telefono  { get; set; } = "";
           public string Email     { get; set; } = "";
           public string Notas     { get; set; } = "";
           public bool   Favorito  { get; set; }

    public Contacto Clone() => new() {
        Id        = this.Id,
        Nombre    = this.Nombre,
        Telefono  = this.Telefono,
        Email     = this.Email,
        Notas     = this.Notas,
        Favorito  = this.Favorito
    };
}
```

Qué hace cada atributo:

- `[Table("Contactos")]` indica el nombre real de la tabla. Sin esto, Dapper.Contrib pluraliza el nombre de la clase, lo que no siempre acierta en español. Mejor explicitarlo siempre.
- `[Key]` marca la columna de clave primaria **auto-generada por la base** (en SQLite, `INTEGER PRIMARY KEY AUTOINCREMENT`). En `Insert`, esa propiedad se rellena con el id devuelto por la base.

### El store

``` csharp
public sealed class SqliteAgendaStore {
    private readonly string connectionString;

    public SqliteAgendaStore(string dbPath) {
        this.connectionString = $"Data Source={dbPath}";
        EnsureSchema();
    }

    private SqliteConnection Open() {
        SqliteConnection connection = new(this.connectionString);
        connection.Open();
        return connection;
    }

    private void EnsureSchema() {
        using SqliteConnection db = Open();
        db.Execute("""
            CREATE TABLE IF NOT EXISTS Contactos (
                Id       INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre   TEXT    NOT NULL,
                Telefono TEXT    NOT NULL DEFAULT '',
                Email    TEXT    NOT NULL DEFAULT '',
                Notas    TEXT    NOT NULL DEFAULT '',
                Favorito INTEGER NOT NULL DEFAULT 0
            );
        """);
    }

    public List<Contacto> GetAll() {
        using SqliteConnection db = Open();
        return db.GetAll<Contacto>().ToList();
    }

    public int Insert(Contacto contact) {
        using SqliteConnection db = Open();
        return (int)db.Insert(contact);
    }

    public bool Update(Contacto contact) {
        using SqliteConnection db = Open();
        return db.Update(contact);
    }

    public bool Delete(int id) {
        using SqliteConnection db = Open();
        return db.Delete(new Contacto { Id = id });
    }

    public void DeleteAll() {
        using SqliteConnection db = Open();
        db.Execute("DELETE FROM Contactos;");
    }
}
```

### Cómo se traduce cada operación

| Método del store        | Lo que hace Dapper.Contrib                                  |
| ----------------------- | ----------------------------------------------------------- |
| `db.GetAll<Contacto>()` | `SELECT * FROM Contactos`                                   |
| `db.Get<Contacto>(id)`  | `SELECT * FROM Contactos WHERE Id = @Id`                    |
| `db.Insert(c)`          | `INSERT INTO Contactos (...) VALUES (...)` y devuelve el id |
| `db.Update(c)`          | `UPDATE Contactos SET ... WHERE Id = @Id`                   |
| `db.Delete(c)`          | `DELETE FROM Contactos WHERE Id = @Id`                      |

El detalle a recordar es que `Delete` recibe **el objeto**, no el id. Cuando solo tenemos el id (como en una eliminación desde la lista), construimos un contacto vacío con ese id:

``` csharp
db.Delete(new Contacto { Id = id });
```

### Mapeo de tipos

SQLite no tiene tipos estrictos, pero Dapper resuelve el mapeo común:

| Tipo C#    | Tipo SQLite | Comentario                        |
| ---------- | ----------- | --------------------------------- |
| `int`      | `INTEGER`   | Directo                           |
| `string`   | `TEXT`      | Directo                           |
| `bool`     | `INTEGER`   | `1` para `true`, `0` para `false` |
| `DateTime` | `TEXT`      | Formato ISO 8601                  |
| `decimal`  | `TEXT`      | Para evitar pérdida de precisión  |

### Por qué abrir y cerrar la conexión cada vez

`Open()` se llama en cada método y se libera con `using`. Para una app de escritorio con un solo usuario y una base SQLite local, esto es trivial en costo y elimina toda una categoría de bugs (conexiones colgadas, transacciones huérfanas). En aplicaciones servidor sí tendría sentido un pool, pero acá no.

### Integración con la ventana

La ventana ya no tiene un flag `hasUnsavedChanges`. Cada operación CRUD persiste de inmediato:

``` csharp
private void NewContact() {
    Contacto contact = new();   // sin Id; SQLite lo asignará

    ContactDialog dialog = new("Nuevo contacto", contact);
    App!.Run(dialog);

    if (!dialog.Saved || dialog.Contact is null) {
        return;
    }

    int newId = store.Insert(dialog.Contact);
    dialog.Contact.Id = newId;
    contacts.Add(dialog.Contact);

    SetStatus($"Contacto '{dialog.Contact.Nombre}' creado.");
    RefreshContacts(newId);
}
```

---

## 16. Importación y exportación JSON

La base de datos es la fuente de verdad. JSON sirve para dos cosas distintas:

- **Exportar**: dar al usuario un archivo legible para versionar, mandar por mail o respaldar.
- **Importar**: cargar contactos provistos en un archivo, por ejemplo al iniciar el sistema o al recibir datos de otro lado.

Estas dos operaciones no son persistencia: son **interoperabilidad**. Por eso viven en una clase separada del store.

### Opciones en el menú

La importación y la exportación tienen que aparecer como acciones del menú `Archivo`, junto con guardar, recargar y salir:

``` csharp
new MenuBarItem("_Archivo", [
    new MenuItem("_Importar JSON...", "Ctrl+I", ImportFromJson),
    new MenuItem("_Exportar JSON...", "Ctrl+E", ExportToJson),
    new MenuItem("_Recargar",         "Ctrl+R", ReloadContacts),
    new MenuItem("_Salir",            "Ctrl+Q", RequestExit)
])
```

El menú no implementa la operación. Solo conecta la opción visual con un método de la ventana principal:

- `ImportFromJson` lee un archivo JSON y agrega esos contactos a la base;
- `ExportToJson` escribe los contactos actuales en un archivo JSON.

Los mismos métodos se invocan desde el teclado con `Ctrl+I` y `Ctrl+E`.

### El conversor JSON

``` csharp
public sealed class JsonAgendaIO {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        WriteIndented = true,
        Encoder       = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public List<Contacto> Read(string path) {
        if (!File.Exists(path)) {
            throw new FileNotFoundException($"Archivo no encontrado: '{path}'");
        }

        string json = File.ReadAllText(path, Encoding.UTF8);
        return JsonSerializer.Deserialize<List<Contacto>>(json, JsonOptions) ?? new();
    }

    public void Write(string path, IEnumerable<Contacto> contacts) {
        string json = JsonSerializer.Serialize(
            contacts.OrderBy(contact => contact.Id),
            JsonOptions);

        File.WriteAllText(path, json, Encoding.UTF8);
    }
}
```

`UnsafeRelaxedJsonEscaping` permite que las tildes y la `ñ` se escriban como tales en el archivo, en vez de aparecer como `\u00f1`. Para datos en español es lo que casi siempre se quiere.

### Pedirle un path al usuario

Necesitamos un diálogo simple para que el usuario tipee la ruta del archivo. Se puede hacer con `FileDialog`, pero un diálogo propio con un `TextField` ya alcanza para el ejemplo:

``` csharp
private string? AskFilePath(string title, string prompt, string defaultPath) {
    Dialog dialog = new() {
        Title  = title,
        Width  = 60, Height = 8
    };

    Label label = new() { Text = prompt, X = 1, Y = 1 };

    TextField input = new() {
        Text = defaultPath,
        X = 1, Y = Pos.Bottom(label) + 1,
        Width = Dim.Fill(2)
    };

    string? result = null;

    Button ok = new() { Text = "_Aceptar", IsDefault = true };
    ok.Accepting += (_, e) => {
        result = input.Text?.ToString();
        e.Handled = true;
        dialog.RequestStop();
    };

    Button cancel = new() { Text = "_Cancelar" };
    cancel.Accepting += (_, e) => {
        result = null;
        e.Handled = true;
        dialog.RequestStop();
    };

    dialog.AddButton(ok);
    dialog.AddButton(cancel);
    dialog.Add(label, input);

    App!.Run(dialog);
    return result;
}
```

### Exportar

Tomamos los contactos en memoria y los escribimos al archivo:

``` csharp
private void ExportToJson() {
    string? path = AskFilePath(
        "Exportar JSON",
        "Ruta de salida:",
        "agenda-export.json");

    if (string.IsNullOrWhiteSpace(path)) {
        return;
    }

    try {
        json.Write(path, contacts);
        SetStatus($"Exportados {contacts.Count} contactos a '{path}'.");
    } catch (Exception ex) {
        MessageBox.ErrorQuery(App!, "Exportar", $"Error: {ex.Message}", "OK");
    }
}
```

### Importar

Importar es más delicado: hay que decidir qué pasa con los contactos ya existentes. La estrategia más simple es **agregar**: cada contacto del JSON entra como nuevo registro en la base, con un id fresco asignado por SQLite.

``` csharp
private void ImportFromJson() {
    string? path = AskFilePath(
        "Importar JSON",
        "Ruta del archivo a importar:",
        "agenda-export.json");

    if (string.IsNullOrWhiteSpace(path)) {
        return;
    }

    try {
        List<Contacto> imported = json.Read(path);

        int answer = MessageBox.Query(
            App!,
            "Importar",
            $"Se agregarán {imported.Count} contactos del archivo a la base. ¿Continuar?",
            "No", "Sí") ?? 0;

        if (answer != 1) {
            return;
        }

        foreach (Contacto contact in imported) {
            contact.Id = 0;                   // SQLite asigna id nuevo
            int newId = store.Insert(contact);
            contact.Id = newId;
        }

        LoadContacts();
        SetStatus($"{imported.Count} contactos importados desde '{path}'.");
    } catch (Exception ex) {
        MessageBox.ErrorQuery(App!, "Importar", $"Error: {ex.Message}", "OK");
    }
}
```

Puntos clave:

- **Reseteamos `Id = 0` antes de insertar** para que SQLite genere un id nuevo. Si dejáramos los ids del archivo, podríamos chocar con ids ya existentes en la base.
- **Confirmación explícita**: importar es una operación destructiva en el sentido de que agrega datos que después hay que limpiar si fue un error.
- **Recarga completa al final**: después de importar, refrescamos `contacts` desde la base para que la lista en memoria refleje exactamente lo que hay persistido (incluidos los ids nuevos).
- **Manejo de errores con `MessageBox.ErrorQuery`**: archivos inexistentes, JSON malformado o problemas de permisos no deben tirar el programa.

### Dos formatos, un solo modelo

Lo importante es que el `Contacto` se serializa y deserializa **igual** en JSON y en SQLite. No hay dos versiones del modelo. Los atributos `[Table]` y `[Key]` no afectan la serialización JSON, y la propiedad `Favorito` se mapea a `INTEGER` en SQLite y a `true`/`false` en JSON sin que tengamos que hacer nada.

---

## 17. El CRUD final

El archivo `agenda-terminal-gui.cs` contiene una agenda completa.

Partes principales:

``` text
agenda-terminal-gui.cs
├── arranque de la app
├── AgendaWindow
│   ├── BuildLayout
│   ├── OnKeyDown
│   ├── LoadContacts / ReloadContacts
│   ├── NewContact / EditSelectedContact / DeleteSelectedContact
│   ├── ImportFromJson / ExportToJson
│   ├── RefreshContacts / RefreshDetails
│   ├── AskFilePath
│   └── RequestExit
├── ContactDialog
├── SqliteAgendaStore
├── JsonAgendaIO
├── Contacto
└── ContactRow
```

### Arranque

``` csharp
string dbPath = args.Length > 0 ? args[0] : "agenda.db";

SqliteAgendaStore store = new(dbPath);
JsonAgendaIO      json  = new();

using IApplication app = Application.Create().Init();
app.Run(new AgendaWindow(store, json));
```

### Crear

``` csharp
private void NewContact() {
    Contacto contact = new();   // sin Id; SQLite lo asignará al insertar

    ContactDialog dialog = new("Nuevo contacto", contact);
    App!.Run(dialog);

    if (!dialog.Saved || dialog.Contact is null) {
        return;
    }

    int newId = store.Insert(dialog.Contact);
    dialog.Contact.Id = newId;
    contacts.Add(dialog.Contact);

    SetStatus($"Contacto '{dialog.Contact.Nombre}' creado.");
    RefreshContacts(newId);
}
```

### Leer

La lectura ocurre al arrancar y al recargar:

``` csharp
private void LoadContacts() {
    contacts = store.GetAll();
    RefreshContacts();
}

private void ReloadContacts() {
    LoadContacts();
    SetStatus("Datos recargados desde la base.");
}
```

### Actualizar

``` csharp
private void EditSelectedContact() {
    Contacto? selected = GetSelectedContact();
    if (selected is null) {
        MessageBox.Query(App!, "Editar", "Seleccioná un contacto.", "OK");
        return;
    }

    ContactDialog dialog = new("Editar contacto", selected.Clone());
    App!.Run(dialog);

    if (!dialog.Saved || dialog.Contact is null) {
        return;
    }

    store.Update(dialog.Contact);
    int index = contacts.FindIndex(contact => contact.Id == selected.Id);
    contacts[index] = dialog.Contact;

    SetStatus($"Contacto '{dialog.Contact.Nombre}' actualizado.");
    RefreshContacts(dialog.Contact.Id);
}
```

### Eliminar

``` csharp
private void DeleteSelectedContact() {
    Contacto? selected = GetSelectedContact();
    if (selected is null) {
        return;
    }

    int answer = MessageBox.Query(
        App!,
        "Eliminar contacto",
        $"¿Eliminar a {selected.Nombre}?",
        "No",
        "Sí") ?? 0;

    if (answer == 1) {
        store.Delete(selected.Id);
        contacts.RemoveAll(contact => contact.Id == selected.Id);

        SetStatus("Contacto eliminado.");
        RefreshContacts();
    }
}
```

---

## 18. Orden recomendado para construir una TUI

Para no perderse, conviene construir en este orden:

1.  Modelo de datos (`Contacto` con `[Table]` y `[Key]`).
2.  Store de persistencia (`SqliteAgendaStore`) y verificar con un script chico que `Insert/GetAll/Update/Delete` funcionan.
3.  Conversor JSON (`JsonAgendaIO`) y verificar con un round-trip.
4.  Ventana principal vacía.
5.  Menú superior.
6.  Panel izquierdo con `ListView`.
7.  Panel derecho con detalle.
8.  Búsqueda activa.
9.  Diálogo de edición.
10. Acciones CRUD conectadas al store.
11. Importación y exportación JSON.
12. Atajos de teclado.
13. Recargar y confirmar salida.

Si se intenta hacer todo junto, los errores de layout, eventos y persistencia se mezclan. Especialmente el store: conviene probarlo desde un archivo aparte antes de integrarlo a la UI.

---

## 19. Errores comunes

### Olvidar `[Table]` o `[Key]` en el modelo

Síntoma: `Insert` falla con un error sobre tabla inexistente, o el id no se rellena después de insertar.

Solución: anotar la clase con `[Table("Contactos")]` y la propiedad clave con `[Key]`. Sin `[Key]`, Dapper.Contrib busca una propiedad llamada `Id` por convención, pero ser explícito evita sorpresas.

### `Delete` recibe el objeto, no el id

`db.Delete(5)` no compila o no hace lo que parece. Hay que pasar un objeto con el id seteado:

``` csharp
db.Delete(new Contacto { Id = id });
```

### Conexión no cerrada

Síntoma: el archivo `.db` queda bloqueado, o las pruebas dejan procesos colgados.

Solución: siempre usar `using SqliteConnection db = Open();`. El `using` garantiza que la conexión se cierra incluso si la operación lanza excepción.

### Importar JSON conservando los ids del archivo

Si el JSON viene con ids `1, 2, 3` y la base ya tiene contactos con esos ids, `Insert` falla por clave duplicada. Conviene resetear `contact.Id = 0` antes de insertar y dejar que SQLite asigne ids frescos.

### Mantener la lista en memoria desactualizada

Después de cada operación CRUD, hay que actualizar **dos** lugares: la base con el `store`, y la lista `contacts` en memoria. Si solo se actualiza uno, la UI queda inconsistente con la base hasta el próximo `ReloadContacts`. La regla: persistir primero, actualizar memoria después, refrescar la lista por último.

### Guardar directamente desde el diálogo

No conviene. El diálogo debe devolver datos. La ventana decide cuándo persistir. Esto es lo que hace posible que el botón "Cancelar" del diálogo funcione: no hay nada para deshacer porque nada se persistió todavía.

### Perder la selección al filtrar

Si al refrescar la lista siempre seleccionás el primer elemento, la UI se siente errática. Conviene mantener el `Id` seleccionado:

``` csharp
int? idToKeep = selectedId ?? GetSelectedContact()?.Id;
```

### Intentar parsear el texto de la fila

No uses la fila formateada como fuente de verdad. La fuente de verdad es el objeto `Contacto` en `filteredContacts`.

---

## 20. Ejercicios para alumnos

1.  Agregar un campo `FechaNacimiento` al contacto. Hace falta tocar el modelo, el schema (`ALTER TABLE` o regenerar la base), el diálogo y la fila de la lista.
2.  Validar que el email contenga `@` antes de guardar.
3.  Agregar un menú `Ver -> Solo favoritos` que filtre la lista.
4.  Agregar un botón `Limpiar búsqueda`.
5.  Implementar la importación JSON con una opción extra: "Reemplazar todo" además de "Agregar". Usar `store.DeleteAll()` antes de insertar.
6.  Reemplazar `ListView` por `TableView` para mostrar columnas.
7.  Agregar importación y exportación CSV, reusando `Dapper.Contrib` para la lectura de la base.
8.  Envolver la importación JSON en una transacción de SQLite (`BEGIN/COMMIT/ROLLBACK`) para que sea todo o nada.
9.  Usar `db.Get<Contacto>(id)` para refrescar el detalle desde la base cada vez que cambia la selección, en vez de leer la lista en memoria.
10. Agregar un campo `FechaUltimaModificacion` que se actualice automáticamente en `Insert` y `Update`, mostrarlo en una columna del `TableView`.

---

## 21. Fuentes oficiales consultadas

- Terminal.Gui — inicio: https://gui-cs.github.io/Terminal.Gui/docs/getting-started.html
- Terminal.Gui — arquitectura de aplicación: https://gui-cs.github.io/Terminal.Gui/docs/application.html
- Terminal.Gui — configuración: https://gui-cs.github.io/Terminal.Gui/docs/config.html
- Terminal.Gui — layout: https://gui-cs.github.io/Terminal.Gui/docs/layout.html
- Terminal.Gui — View: https://gui-cs.github.io/Terminal.Gui/docs/View.html
- Terminal.Gui — menús: https://gui-cs.github.io/Terminal.Gui/docs/menus.html
- Terminal.Gui — teclado: https://gui-cs.github.io/Terminal.Gui/docs/keyboard.html
- Microsoft.Data.Sqlite: https://learn.microsoft.com/dotnet/standard/data/sqlite/
- Dapper: https://github.com/DapperLib/Dapper
- Dapper.Contrib: https://github.com/DapperLib/Dapper.Contrib
- System.Text.Json: https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/
