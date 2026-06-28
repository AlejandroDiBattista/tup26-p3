#:package Terminal.Gui@1.*
#:package Microsoft.Data.Sqlite@10.*
#:package Dapper@2.*
#:package Dapper.Contrib@2.0.78
#:property LangVersion=preview

using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;
using System.Text.Json;
using Terminal.Gui;

var db = args.Length > 0 ? args[0] : "agenda.db";
var store = new Store(db);

Application.Init();
Application.Run(new Agenda(store));
Application.Shutdown();

class Agenda : Window
{
    Store store;
    List<Contacto> contactos = [];
    List<Contacto> filtrados = [];
    ListView lista;
    TextField buscar;
    Label detalle;
    bool fav;

    public Agenda(Store s) : base("AgendaT")
    {
        store = s;
        Width = Dim.Fill();
        Height = Dim.Fill();

        contactos = store.All();

        Add(new MenuBar(new MenuBarItem[] {
            new("_Archivo", new MenuItem[] {
                new("Importar", "", Importar),
                new("Exportar", "", Exportar),
                new("Salir", "", () => Application.RequestStop())
            }),
            new("_Contactos", new MenuItem[] {
                new("Nuevo", "", Nuevo),
                new("Editar", "", Editar),
                new("Eliminar", "", Eliminar)
            }),
            new("_Ver", new MenuItem[] {
                new("Solo favoritos", "", () => { fav = !fav; Filtrar(); })
            })
        }));

        Add(new Label("Buscar:"){ X=1,Y=1 });

        buscar = new("")
        {
            X=10,
            Y=1,
            Width=40
        };

        buscar.TextChanged += _ => Filtrar();

        Add(buscar);

        lista = new()
        {
            X=1,
            Y=3,
            Width=30,
            Height=Dim.Fill()-1
        };

        lista.SelectedItemChanged += _ => Mostrar();

        lista.OpenSelectedItem += _ => Editar();

        Add(lista);

        detalle = new("")
        {
            X=35,
            Y=3,
            Width=Dim.Fill(),
            Height=Dim.Fill()
        };

        Add(detalle);

        Add(new StatusBar([
            new(Key.F2,"Nuevo",Nuevo),
            new(Key.F3,"Editar",Editar),
            new(Key.DeleteChar,"Eliminar",Eliminar),
            new(Key.CtrlMask | Key.Q,"Salir",()=>Application.RequestStop())
        ]));

        Filtrar();
    }

    void Filtrar()
    {
        var q = buscar.Text.ToString()!.ToLower();

        filtrados = contactos.Where(c =>
        (
            c.Nombre.ToLower().Contains(q) ||
            c.Telefonos.ToLower().Contains(q) ||
            c.Email.ToLower().Contains(q)
        ) && (!fav || c.Favorito)).ToList();

        lista.SetSource(
            filtrados.Select(c =>
            $"{(c.Favorito ? "★" : " ")} {c.Nombre}").ToList()
        );

        Mostrar();
    }

    void Mostrar()
    {
        if (lista.SelectedItem < 0 || filtrados.Count == 0)
        {
            detalle.Text = "";
            return;
        }

        var c = filtrados[lista.SelectedItem];

        detalle.Text =
$"""
Nombre: {c.Nombre}

Telefonos: {c.Telefonos}

Email: {c.Email}

Favorito: {(c.Favorito ? "Si" : "No")}

Notas:
{c.Notas}
""";
    }

    void Nuevo()
    {
        var d = new Dialogo(new());
        Application.Run(d);

        if (!d.Ok) return;

        store.Insert(d.C);
        contactos = store.All();
        Filtrar();
    }

    void Editar()
    {
        if (lista.SelectedItem < 0 || filtrados.Count == 0)
            return;

        var d = new Dialogo(filtrados[lista.SelectedItem].Clone());

        Application.Run(d);

        if (!d.Ok) return;

        store.Update(d.C);
        contactos = store.All();
        Filtrar();
    }

    void Eliminar()
    {
        if (lista.SelectedItem < 0 || filtrados.Count == 0)
            return;

        var c = filtrados[lista.SelectedItem];

        if (MessageBox.Query("Eliminar",$"Eliminar {c.Nombre}?","Si","No") != 0)
            return;

        store.Delete(c);
        contactos = store.All();
        Filtrar();
    }

    void Exportar()
    {
        var d = new SaveDialog("Exportar","Guardar");

        Application.Run(d);

        if (d.Canceled || d.FilePath is null)
            return;

        File.WriteAllText(
            d.FilePath.ToString()!,
            JsonSerializer.Serialize(contactos,new JsonSerializerOptions
            {
                WriteIndented = true
            })
        );
    }

    void Importar()
    {
        var d = new OpenDialog("Importar","Abrir");

        Application.Run(d);

        if (d.Canceled || d.FilePath is null)
            return;

        try
        {
            var json = File.ReadAllText(d.FilePath.ToString()!);

            var lista = JsonSerializer.Deserialize<List<Contacto>>(json);

            if (lista is null) return;

            if (MessageBox.Query("Importar",$"Agregar {lista.Count} contactos?","Si","No") != 0)
                return;

            foreach (var c in lista)
            {
                c.Id = 0;
                store.Insert(c);
            }

            contactos = store.All();
            Filtrar();
        }
        catch(Exception ex)
        {
            MessageBox.ErrorQuery("Error",ex.Message,"OK");
        }
    }
}

class Dialogo : Dialog
{
    public Contacto C;
    public bool Ok;

    TextField n,e,t;
    TextView no;
    CheckBox f;

    public Dialogo(Contacto c) : base("Contacto",60,20)
    {
        C = c;

        Add(new Label("Nombre"){X=1,Y=1});
        Add(new Label("Telefonos"){X=1,Y=3});
        Add(new Label("Email"){X=1,Y=5});
        Add(new Label("Notas"){X=1,Y=9});

        n = new(c.Nombre){X=15,Y=1,Width=30};
        t = new(c.Telefonos){X=15,Y=3,Width=30};
        e = new(c.Email){X=15,Y=5,Width=30};

        f = new("Favorito")
        {
            X=15,
            Y=7,
            Checked = c.Favorito
        };

        no = new()
        {
            X=15,
            Y=9,
            Width=30,
            Height=4,
            Text=c.Notas
        };

        Add(n,t,e,f,no);

        var g = new Button("Guardar"){X=15,Y=15};

        g.Clicked += () =>
        {
            if (string.IsNullOrWhiteSpace(n.Text.ToString()))
            {
                MessageBox.ErrorQuery("Error","Nombre obligatorio","OK");
                return;
            }

            if (!string.IsNullOrWhiteSpace(e.Text.ToString()) &&
                !e.Text.ToString()!.Contains("@"))
            {
                MessageBox.ErrorQuery("Error","Email inválido","OK");
                return;
            }

            C.Nombre = n.Text.ToString()!;
            C.Telefonos = t.Text.ToString()!;
            C.Email = e.Text.ToString()!;
            C.Notas = no.Text.ToString()!;
            C.Favorito = f.Checked;

            Ok = true;

            Application.RequestStop();
        };

        var c2 = new Button("Cancelar"){X=30,Y=15};

        c2.Clicked += () => Application.RequestStop();

        AddButton(g);
        AddButton(c2);
    }
}

class Store
{
    string cs;

    public Store(string db)
    {
        cs = $"Data Source={db}";

        using var c = new SqliteConnection(cs);

        c.Execute("""
        CREATE TABLE IF NOT EXISTS Contactos(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Nombre TEXT NOT NULL,
            Telefonos TEXT,
            Email TEXT,
            Notas TEXT,
            Favorito INTEGER NOT NULL DEFAULT 0
        )
        """);
    }

    public List<Contacto> All()
    {
        using var c = new SqliteConnection(cs);
        return c.GetAll<Contacto>().ToList();
    }

    public void Insert(Contacto x)
    {
        using var c = new SqliteConnection(cs);
        c.Insert(x);
    }

    public void Update(Contacto x)
    {
        using var c = new SqliteConnection(cs);
        c.Update(x);
    }

    public void Delete(Contacto x)
    {
        using var c = new SqliteConnection(cs);
        c.Delete(x);
    }
}

[Table("Contactos")]
class Contacto
{
    [Key]
    public int Id { get; set; }

    public string Nombre { get; set; } = "";
    public string Telefonos { get; set; } = "";
    public string Email { get; set; } = "";
    public string Notas { get; set; } = "";
    public bool Favorito { get; set; }

    public Contacto Clone() => new()
    {
        Id = Id,
        Nombre = Nombre,
        Telefonos = Telefonos,
        Email = Email,
        Notas = Notas,
        Favorito = Favorito
    };
}