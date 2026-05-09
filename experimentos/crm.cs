#!/usr/bin/env dotnet
#:package Terminal.Gui@2.*
#:package Dapper@*
#:package Microsoft.Data.Sqlite@*
#:property PublishAot=false

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

// Mini CRM como C# 14 file-based app.
// UI: Terminal.Gui v2
// Datos: SQLite + Dapper
//
// Requisitos:
//   .NET 10 SDK o superior
//
// Guardar como crm.cs y ejecutar:
//   dotnet run crm.cs
//
// En macOS/Linux también puede ejecutarse como script:
//   chmod +x crm.cs
//   ./crm.cs

const string DatabaseFile = "crm.db";
const string ConnectionString = $"Data Source={DatabaseFile}";

Console.OutputEncoding = System.Text.Encoding.UTF8;
Menu.DefaultBorderStyle = LineStyle.Single;

CrmDatabase database = new(ConnectionString);
database.Initialize();
database.SeedIfEmpty();

using IApplication app = Application.Create().Init();
app.Run(new CrmWindow(database));

public sealed class CrmWindow : Runnable
{
    private readonly CrmDatabase database;

    private readonly ListView contactsList = new();
    private readonly ListView opportunitiesList = new();
    private readonly ListView activitiesList = new();

    private readonly TextField searchField = new();
    private readonly Label contactDetails = new();
    private readonly Label statusLabel = new();

    private List<Contact> contacts = new();
    private Contact? selectedContact;

    public CrmWindow(CrmDatabase database)
    {
        this.database = database;

        Title = "Mini CRM - Terminal.Gui v2 + Dapper + SQLite";
        Width = Dim.Fill();
        Height = Dim.Fill();

        BuildLayout();
        RefreshContacts();
    }

    private void BuildLayout()
    {
        MenuBar menuBar = new()
        {
            Menus =
            [
                new MenuBarItem("Archivo",
                [
                    new MenuItem { Title = "Salir", Action = () => App!.RequestStop() }
                ]),
                new MenuBarItem("Contactos",
                [
                    new MenuItem("Nuevo", "Crear contacto", AddContact),
                    new MenuItem("Editar", "Editar contacto seleccionado", EditSelectedContact),
                    new MenuItem("Eliminar", "Eliminar contacto seleccionado", DeleteSelectedContact)
                ]),
                new MenuBarItem("Ventas",
                [
                    new MenuItem("Nueva oportunidad", "Crear oportunidad", AddOpportunity),
                    new MenuItem("Editar oportunidad", "Editar oportunidad", EditSelectedOpportunity),
                    new MenuItem("Eliminar oportunidad", "Eliminar oportunidad", DeleteSelectedOpportunity)
                ]),
                new MenuBarItem("Actividades",
                [
                    new MenuItem("Nueva actividad", "Registrar seguimiento", AddActivity),
                    new MenuItem("Marcar como hecha", "Completar actividad", CompleteSelectedActivity),
                    new MenuItem("Eliminar actividad", "Eliminar actividad", DeleteSelectedActivity)
                ])
            ]
        };

        Label searchLabel = new()
        {
            Text = "Buscar:",
            X = 1,
            Y = 2
        };

        searchField.X = 10;
        searchField.Y = 2;
        searchField.Width = Dim.Percent(35);
        searchField.TextChanged += (_, _) => RefreshContacts();

        Button newContactButton = new()
        {
            Text = "Nuevo",
            X = Pos.Right(searchField) + 2,
            Y = 2
        };
        newContactButton.Accepted += (_, _) => AddContact();

        Button editContactButton = new()
        {
            Text = "Editar",
            X = Pos.Right(newContactButton) + 1,
            Y = 2
        };
        editContactButton.Accepted += (_, _) => EditSelectedContact();

        Button deleteContactButton = new()
        {
            Text = "Eliminar",
            X = Pos.Right(editContactButton) + 1,
            Y = 2
        };
        deleteContactButton.Accepted += (_, _) => DeleteSelectedContact();

        FrameView contactsFrame = new()
        {
            Title = "Contactos",
            X = 0,
            Y = 4,
            Width = Dim.Percent(38),
            Height = Dim.Fill(2)
        };
        contactsFrame.BorderStyle = LineStyle.Single;

        contactsList.X = 0;
        contactsList.Y = 0;
        contactsList.Width = Dim.Fill();
        contactsList.Height = Dim.Fill();
        contactsList.ValueChanged += (_, _) => SelectContact(contactsList.SelectedItem ?? -1);
        contactsList.Accepted += (_, _) => EditSelectedContact();
        contactsFrame.Add(contactsList);

        FrameView detailsFrame = new()
        {
            Title = "Detalle del contacto",
            X = Pos.Right(contactsFrame),
            Y = 4,
            Width = Dim.Fill(),
            Height = 9
        };
        detailsFrame.BorderStyle = LineStyle.Single;

        contactDetails.X = 1;
        contactDetails.Y = 0;
        contactDetails.Width = Dim.Fill(1);
        contactDetails.Height = Dim.Fill();
        contactDetails.Text = "Sin contacto seleccionado";
        detailsFrame.Add(contactDetails);

        FrameView opportunitiesFrame = new()
        {
            Title = "Oportunidades",
            X = Pos.Right(contactsFrame),
            Y = Pos.Bottom(detailsFrame),
            Width = Dim.Fill(),
            Height = Dim.Percent(36)
        };
        opportunitiesFrame.BorderStyle = LineStyle.Single;

        opportunitiesList.X = 0;
        opportunitiesList.Y = 0;
        opportunitiesList.Width = Dim.Fill();
        opportunitiesList.Height = Dim.Fill(1);
        opportunitiesList.Accepted += (_, _) => EditSelectedOpportunity();

        Button newOpportunityButton = new()
        {
            Text = "Nueva",
            X = 0,
            Y = Pos.Bottom(opportunitiesList)
        };
        newOpportunityButton.Accepted += (_, _) => AddOpportunity();

        Button editOpportunityButton = new()
        {
            Text = "Editar",
            X = Pos.Right(newOpportunityButton) + 1,
            Y = Pos.Bottom(opportunitiesList)
        };
        editOpportunityButton.Accepted += (_, _) => EditSelectedOpportunity();

        Button deleteOpportunityButton = new()
        {
            Text = "Eliminar",
            X = Pos.Right(editOpportunityButton) + 1,
            Y = Pos.Bottom(opportunitiesList)
        };
        deleteOpportunityButton.Accepted += (_, _) => DeleteSelectedOpportunity();

        opportunitiesFrame.Add(opportunitiesList, newOpportunityButton, editOpportunityButton, deleteOpportunityButton);

        FrameView activitiesFrame = new()
        {
            Title = "Actividades / Seguimientos",
            X = Pos.Right(contactsFrame),
            Y = Pos.Bottom(opportunitiesFrame),
            Width = Dim.Fill(),
            Height = Dim.Fill(2)
        };
        activitiesFrame.BorderStyle = LineStyle.Single;

        activitiesList.X = 0;
        activitiesList.Y = 0;
        activitiesList.Width = Dim.Fill();
        activitiesList.Height = Dim.Fill(1);

        Button newActivityButton = new()
        {
            Text = "Nueva",
            X = 0,
            Y = Pos.Bottom(activitiesList)
        };
        newActivityButton.Accepted += (_, _) => AddActivity();

        Button completeActivityButton = new()
        {
            Text = "Hecha",
            X = Pos.Right(newActivityButton) + 1,
            Y = Pos.Bottom(activitiesList)
        };
        completeActivityButton.Accepted += (_, _) => CompleteSelectedActivity();

        Button deleteActivityButton = new()
        {
            Text = "Eliminar",
            X = Pos.Right(completeActivityButton) + 1,
            Y = Pos.Bottom(activitiesList)
        };
        deleteActivityButton.Accepted += (_, _) => DeleteSelectedActivity();

        activitiesFrame.Add(activitiesList, newActivityButton, completeActivityButton, deleteActivityButton);

        statusLabel.X = 1;
        statusLabel.Y = Pos.AnchorEnd(1);
        statusLabel.Width = Dim.Fill();
        statusLabel.Text = "Listo. Ctrl+Q para salir.";

        Add(
            menuBar,
            searchLabel,
            searchField,
            newContactButton,
            editContactButton,
            deleteContactButton,
            contactsFrame,
            detailsFrame,
            opportunitiesFrame,
            activitiesFrame,
            statusLabel
        );
    }

    private void RefreshContacts()
    {
        string search = searchField.Text?.ToString() ?? "";
        contacts = database.SearchContacts(search).ToList();

        contactsList.SetSource(ToObservable(contacts.Select(FormatContactRow)));

        if (contacts.Count == 0)
        {
            selectedContact = null;
            RefreshSelectedContactArea();
            return;
        }

        int index = Math.Clamp(contactsList.SelectedItem ?? 0, 0, contacts.Count - 1);
        SelectContact(index);
    }

    private void SelectContact(int index)
    {
        selectedContact = index >= 0 && index < contacts.Count ? contacts[index] : null;
        RefreshSelectedContactArea();
    }

    private void RefreshSelectedContactArea()
    {
        if (selectedContact is null)
        {
            contactDetails.Text = "Sin contacto seleccionado";
            opportunitiesList.SetSource(new ObservableCollection<string>());
            activitiesList.SetSource(new ObservableCollection<string>());
            return;
        }

        contactDetails.Text = $"""
            Nombre:   {selectedContact.Name}
            Empresa:  {selectedContact.Company}
            Email:    {selectedContact.Email}
            Teléfono: {selectedContact.Phone}
            Estado:   {selectedContact.Status}
            Notas:    {selectedContact.Notes}
            """;

        RefreshOpportunities();
        RefreshActivities();
    }

    private void RefreshOpportunities()
    {
        if (selectedContact is null)
        {
            opportunitiesList.SetSource(new ObservableCollection<string>());
            return;
        }

        List<string> rows = database.GetOpportunities(selectedContact.Id)
            .Select(FormatOpportunityRow)
            .ToList();

        opportunitiesList.SetSource(ToObservable(rows));
    }

    private void RefreshActivities()
    {
        if (selectedContact is null)
        {
            activitiesList.SetSource(new ObservableCollection<string>());
            return;
        }

        List<string> rows = database.GetActivities(selectedContact.Id)
            .Select(FormatActivityRow)
            .ToList();

        activitiesList.SetSource(ToObservable(rows));
    }

    private void AddContact()
    {
        ContactDialog dialog = new("Nuevo contacto", Contact.Empty());
        App!.Run(dialog);

        if (!dialog.Saved || dialog.Contact is null)
        {
            return;
        }

        database.InsertContact(dialog.Contact);
        SetStatus("Contacto creado.");
        RefreshContacts();
    }

    private void EditSelectedContact()
    {
        if (!RequireContact(out Contact contact))
        {
            return;
        }

        ContactDialog dialog = new("Editar contacto", contact.Clone());
        App!.Run(dialog);

        if (!dialog.Saved || dialog.Contact is null)
        {
            return;
        }

        database.UpdateContact(dialog.Contact);
        SetStatus("Contacto actualizado.");
        RefreshContacts();
    }

    private void DeleteSelectedContact()
    {
        if (!RequireContact(out Contact contact))
        {
            return;
        }

        int answer = MessageBox.Query(App!, "Eliminar contacto", $"¿Eliminar a {contact.Name}?\nTambién se eliminarán sus oportunidades y actividades.", "No", "Sí") ?? 0;
        if (answer != 1)
        {
            return;
        }

        database.DeleteContact(contact.Id);
        SetStatus("Contacto eliminado.");
        RefreshContacts();
    }

    private void AddOpportunity()
    {
        if (!RequireContact(out Contact contact))
        {
            return;
        }

        OpportunityDialog dialog = new("Nueva oportunidad", Opportunity.Empty(contact.Id));
        App!.Run(dialog);

        if (!dialog.Saved || dialog.Opportunity is null)
        {
            return;
        }

        database.InsertOpportunity(dialog.Opportunity);
        SetStatus("Oportunidad creada.");
        RefreshOpportunities();
    }

    private void EditSelectedOpportunity()
    {
        if (!RequireContact(out Contact contact))
        {
            return;
        }

        Opportunity? opportunity = GetSelectedOpportunity(contact.Id);
        if (opportunity is null)
        {
            MessageBox.Query(App!, "Oportunidad", "Seleccioná una oportunidad.", "OK");
            return;
        }

        OpportunityDialog dialog = new("Editar oportunidad", opportunity.Clone());
        App!.Run(dialog);

        if (!dialog.Saved || dialog.Opportunity is null)
        {
            return;
        }

        database.UpdateOpportunity(dialog.Opportunity);
        SetStatus("Oportunidad actualizada.");
        RefreshOpportunities();
    }

    private void DeleteSelectedOpportunity()
    {
        if (!RequireContact(out Contact contact))
        {
            return;
        }

        Opportunity? opportunity = GetSelectedOpportunity(contact.Id);
        if (opportunity is null)
        {
            MessageBox.Query(App!, "Oportunidad", "Seleccioná una oportunidad.", "OK");
            return;
        }

        int answer = MessageBox.Query(App!, "Eliminar oportunidad", $"¿Eliminar '{opportunity.Title}'?", "No", "Sí") ?? 0;
        if (answer != 1)
        {
            return;
        }

        database.DeleteOpportunity(opportunity.Id);
        SetStatus("Oportunidad eliminada.");
        RefreshOpportunities();
    }

    private void AddActivity()
    {
        if (!RequireContact(out Contact contact))
        {
            return;
        }

        ActivityDialog dialog = new("Nueva actividad", Activity.Empty(contact.Id));
        App!.Run(dialog);

        if (!dialog.Saved || dialog.Activity is null)
        {
            return;
        }

        database.InsertActivity(dialog.Activity);
        SetStatus("Actividad creada.");
        RefreshActivities();
    }

    private void CompleteSelectedActivity()
    {
        if (!RequireContact(out Contact contact))
        {
            return;
        }

        Activity? activity = GetSelectedActivity(contact.Id);
        if (activity is null)
        {
            MessageBox.Query(App!, "Actividad", "Seleccioná una actividad.", "OK");
            return;
        }

        database.CompleteActivity(activity.Id);
        SetStatus("Actividad marcada como hecha.");
        RefreshActivities();
    }

    private void DeleteSelectedActivity()
    {
        if (!RequireContact(out Contact contact))
        {
            return;
        }

        Activity? activity = GetSelectedActivity(contact.Id);
        if (activity is null)
        {
            MessageBox.Query(App!, "Actividad", "Seleccioná una actividad.", "OK");
            return;
        }

        int answer = MessageBox.Query(App!, "Eliminar actividad", $"¿Eliminar '{activity.Subject}'?", "No", "Sí") ?? 0;
        if (answer != 1)
        {
            return;
        }

        database.DeleteActivity(activity.Id);
        SetStatus("Actividad eliminada.");
        RefreshActivities();
    }

    private Contact? GetSelectedContact()
    {
        int index = contactsList.SelectedItem ?? -1;
        return index >= 0 && index < contacts.Count ? contacts[index] : null;
    }

    private Opportunity? GetSelectedOpportunity(long contactId)
    {
        List<Opportunity> opportunities = database.GetOpportunities(contactId).ToList();
        int index = opportunitiesList.SelectedItem ?? -1;
        return index >= 0 && index < opportunities.Count ? opportunities[index] : null;
    }

    private Activity? GetSelectedActivity(long contactId)
    {
        List<Activity> activities = database.GetActivities(contactId).ToList();
        int index = activitiesList.SelectedItem ?? -1;
        return index >= 0 && index < activities.Count ? activities[index] : null;
    }

    private bool RequireContact(out Contact contact)
    {
        Contact? candidate = GetSelectedContact();
        if (candidate is not null)
        {
            contact = candidate;
            return true;
        }

        contact = null!;
        MessageBox.Query(App!, "Contacto", "Seleccioná un contacto primero.", "OK");
        return false;
    }

    private void SetStatus(string text)
    {
        statusLabel.Text = text;
    }

    private static ObservableCollection<string> ToObservable(IEnumerable<string> values)
    {
        return new ObservableCollection<string>(values.ToList());
    }

    private static string FormatContactRow(Contact contact)
    {
        string company = string.IsNullOrWhiteSpace(contact.Company) ? "sin empresa" : contact.Company;
        return $"{FitColumn(contact.Name, 16)} | {FitColumn(company, 14)} | {FitColumn(contact.Status, 12)}";
    }

    private static string FitColumn(string value, int width)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty.PadRight(width);
        }

        return value.Length > width
            ? value[..width]
            : value.PadRight(width);
    }

    private static string FormatOpportunityRow(Opportunity opportunity)
    {
        return $"{opportunity.Stage,-14} | {opportunity.Value,10:C0} | {opportunity.Title} | Cierre: {opportunity.ExpectedCloseDate}";
    }

    private static string FormatActivityRow(Activity activity)
    {
        string done = activity.IsDone ? "✓" : " ";
        return $"[{done}] {activity.DueDate} | {activity.Type,-8} | {activity.Subject}";
    }
}

public sealed class ContactDialog : Dialog
{
    private readonly TextField nameField;
    private readonly TextField companyField;
    private readonly TextField emailField;
    private readonly TextField phoneField;
    private readonly TextField statusField;
    private readonly TextView notesView;

    public bool Saved { get; private set; }
    public Contact? Contact { get; private set; }

    public ContactDialog(string title, Contact contact)
    {
        Contact = contact;
        Title = title;
        Width = 72;
        Height = 22;

        nameField = new TextField { Text = contact.Name, X = 12, Y = 1, Width = 48 };
        companyField = new TextField { Text = contact.Company, X = 12, Y = 3, Width = 48 };
        emailField = new TextField { Text = contact.Email, X = 12, Y = 5, Width = 48 };
        phoneField = new TextField { Text = contact.Phone, X = 12, Y = 7, Width = 48 };
        statusField = new TextField { Text = contact.Status, X = 12, Y = 9, Width = 48 };
        notesView = new TextView { Text = contact.Notes, X = 12, Y = 11, Width = 48, Height = 4 };

        Button saveButton = new() { Text = "Guardar", X = 20, Y = 17 };
        saveButton.Accepted += (_, _) => Save();

        Button cancelButton = new() { Text = "Cancelar", X = Pos.Right(saveButton) + 2, Y = 17 };
        cancelButton.Accepted += (_, _) => App!.RequestStop();

        Add(new Label { Text = "Nombre:", X = 2, Y = 1 }, nameField,
            new Label { Text = "Empresa:", X = 2, Y = 3 }, companyField,
            new Label { Text = "Email:", X = 2, Y = 5 }, emailField,
            new Label { Text = "Teléfono:", X = 2, Y = 7 }, phoneField,
            new Label { Text = "Estado:", X = 2, Y = 9 }, statusField,
            new Label { Text = "Notas:", X = 2, Y = 11 }, notesView,
            saveButton,
            cancelButton
        );
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(nameField.Text?.ToString()))
        {
            MessageBox.Query(App!, "Validación", "El nombre es obligatorio.", "OK");
            nameField.SetFocus();
            return;
        }

        Contact!.Name = Clean(nameField.Text);
        Contact.Company = Clean(companyField.Text);
        Contact.Email = Clean(emailField.Text);
        Contact.Phone = Clean(phoneField.Text);
        Contact.Status = Clean(statusField.Text, "Nuevo");
        Contact.Notes = Clean(notesView.Text);

        Saved = true;
        App!.RequestStop();
    }

    private static string Clean(object? value, string fallback = "")
    {
        string text = value?.ToString()?.Trim() ?? "";
        return string.IsNullOrWhiteSpace(text) ? fallback : text;
    }
}

public sealed class OpportunityDialog : Dialog
{
    private readonly TextField titleField;
    private readonly TextField stageField;
    private readonly TextField valueField;
    private readonly TextField closeDateField;
    private readonly TextView notesView;

    public bool Saved { get; private set; }
    public Opportunity? Opportunity { get; private set; }

    public OpportunityDialog(string title, Opportunity opportunity)
    {
        Opportunity = opportunity;
        Title = title;
        Width = 74;
        Height = 20;

        titleField = new TextField { Text = opportunity.Title, X = 14, Y = 1, Width = 50 };
        stageField = new TextField { Text = opportunity.Stage, X = 14, Y = 3, Width = 30 };
        valueField = new TextField { Text = opportunity.Value.ToString(CultureInfo.InvariantCulture), X = 14, Y = 5, Width = 20 };
        closeDateField = new TextField { Text = opportunity.ExpectedCloseDate, X = 14, Y = 7, Width = 20 };
        notesView = new TextView { Text = opportunity.Notes, X = 14, Y = 9, Width = 50, Height = 4 };

        Button saveButton = new() { Text = "Guardar", X = 20, Y = 15 };
        saveButton.Accepted += (_, _) => Save();

        Button cancelButton = new() { Text = "Cancelar", X = Pos.Right(saveButton) + 2, Y = 15 };
        cancelButton.Accepted += (_, _) => App!.RequestStop();

        Add(new Label { Text = "Título:", X = 2, Y = 1 }, titleField,
            new Label { Text = "Etapa:", X = 2, Y = 3 }, stageField,
            new Label { Text = "Valor:", X = 2, Y = 5 }, valueField,
            new Label { Text = "Cierre:", X = 2, Y = 7 }, closeDateField,
            new Label { Text = "Notas:", X = 2, Y = 9 }, notesView,
            saveButton,
            cancelButton
        );
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(titleField.Text?.ToString()))
        {
            MessageBox.Query(App!, "Validación", "El título es obligatorio.", "OK");
            titleField.SetFocus();
            return;
        }

        if (!decimal.TryParse(valueField.Text?.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
        {
            MessageBox.Query(App!, "Validación", "El valor debe ser numérico. Usá punto para decimales.", "OK");
            valueField.SetFocus();
            return;
        }

        string closeDate = Clean(closeDateField.Text);
        if (!DateOnly.TryParseExact(closeDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            MessageBox.Query(App!, "Validación", "La fecha debe tener formato yyyy-MM-dd.", "OK");
            closeDateField.SetFocus();
            return;
        }

        Opportunity!.Title = Clean(titleField.Text);
        Opportunity.Stage = Clean(stageField.Text, "Nueva");
        Opportunity.Value = value;
        Opportunity.ExpectedCloseDate = closeDate;
        Opportunity.Notes = Clean(notesView.Text);

        Saved = true;
        App!.RequestStop();
    }

    private static string Clean(object? value, string fallback = "")
    {
        string text = value?.ToString()?.Trim() ?? "";
        return string.IsNullOrWhiteSpace(text) ? fallback : text;
    }
}

public sealed class ActivityDialog : Dialog
{
    private readonly TextField typeField;
    private readonly TextField subjectField;
    private readonly TextField dueDateField;
    private readonly TextView notesView;

    public bool Saved { get; private set; }
    public Activity? Activity { get; private set; }

    public ActivityDialog(string title, Activity activity)
    {
        Activity = activity;
        Title = title;
        Width = 74;
        Height = 18;

        typeField = new TextField { Text = activity.Type, X = 14, Y = 1, Width = 30 };
        subjectField = new TextField { Text = activity.Subject, X = 14, Y = 3, Width = 50 };
        dueDateField = new TextField { Text = activity.DueDate, X = 14, Y = 5, Width = 20 };
        notesView = new TextView { Text = activity.Notes, X = 14, Y = 7, Width = 50, Height = 4 };

        Button saveButton = new() { Text = "Guardar", X = 20, Y = 13 };
        saveButton.Accepted += (_, _) => Save();

        Button cancelButton = new() { Text = "Cancelar", X = Pos.Right(saveButton) + 2, Y = 13 };
        cancelButton.Accepted += (_, _) => App!.RequestStop();

        Add(new Label { Text = "Tipo:", X = 2, Y = 1 }, typeField,
            new Label { Text = "Asunto:", X = 2, Y = 3 }, subjectField,
            new Label { Text = "Fecha:", X = 2, Y = 5 }, dueDateField,
            new Label { Text = "Notas:", X = 2, Y = 7 }, notesView,
            saveButton,
            cancelButton
        );
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(subjectField.Text?.ToString()))
        {
            MessageBox.Query(App!, "Validación", "El asunto es obligatorio.", "OK");
            subjectField.SetFocus();
            return;
        }

        string dueDate = Clean(dueDateField.Text);
        if (!DateOnly.TryParseExact(dueDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            MessageBox.Query(App!, "Validación", "La fecha debe tener formato yyyy-MM-dd.", "OK");
            dueDateField.SetFocus();
            return;
        }

        Activity!.Type = Clean(typeField.Text, "Tarea");
        Activity.Subject = Clean(subjectField.Text);
        Activity.DueDate = dueDate;
        Activity.Notes = Clean(notesView.Text);

        Saved = true;
        App!.RequestStop();
    }

    private static string Clean(object? value, string fallback = "")
    {
        string text = value?.ToString()?.Trim() ?? "";
        return string.IsNullOrWhiteSpace(text) ? fallback : text;
    }
}

public sealed class CrmDatabase
{
    private readonly string connectionString;

    public CrmDatabase(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public void Initialize()
    {
        using IDbConnection connection = OpenConnection();

        connection.Execute("""
            CREATE TABLE IF NOT EXISTS Contacts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Company TEXT NOT NULL DEFAULT '',
                Email TEXT NOT NULL DEFAULT '',
                Phone TEXT NOT NULL DEFAULT '',
                Status TEXT NOT NULL DEFAULT 'Nuevo',
                Notes TEXT NOT NULL DEFAULT '',
                CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            CREATE TABLE IF NOT EXISTS Opportunities (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ContactId INTEGER NOT NULL,
                Title TEXT NOT NULL,
                Stage TEXT NOT NULL DEFAULT 'Nueva',
                Value NUMERIC NOT NULL DEFAULT 0,
                ExpectedCloseDate TEXT NOT NULL,
                Notes TEXT NOT NULL DEFAULT '',
                CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (ContactId) REFERENCES Contacts(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS Activities (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ContactId INTEGER NOT NULL,
                Type TEXT NOT NULL DEFAULT 'Tarea',
                Subject TEXT NOT NULL,
                DueDate TEXT NOT NULL,
                IsDone INTEGER NOT NULL DEFAULT 0,
                Notes TEXT NOT NULL DEFAULT '',
                CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (ContactId) REFERENCES Contacts(Id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_Contacts_Name ON Contacts(Name);
            CREATE INDEX IF NOT EXISTS IX_Opportunities_ContactId ON Opportunities(ContactId);
            CREATE INDEX IF NOT EXISTS IX_Activities_ContactId ON Activities(ContactId);
            """);
    }

    public void SeedIfEmpty()
    {
        using IDbConnection connection = OpenConnection();
        int count = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Contacts;");
        if (count > 0)
        {
            return;
        }

        long anaId = InsertContact(new Contact
        {
            Name = "Ana Gómez",
            Company = "Distribuidora Norte",
            Email = "ana@distribuidora.test",
            Phone = "+54 381 555-0101",
            Status = "Interesada",
            Notes = "Pidió demo y precio final."
        });

        long carlosId = InsertContact(new Contact
        {
            Name = "Carlos Pérez",
            Company = "Estudio Pérez",
            Email = "carlos@estudio.test",
            Phone = "+54 381 555-0202",
            Status = "Cliente",
            Notes = "Cliente activo. Renovación anual."
        });

        InsertOpportunity(new Opportunity
        {
            ContactId = anaId,
            Title = "Implementación CRM básico",
            Stage = "Presupuesto",
            Value = 350000,
            ExpectedCloseDate = DateOnly.FromDateTime(DateTime.Today.AddDays(14)).ToString("yyyy-MM-dd"),
            Notes = "Quiere algo simple para tres usuarios."
        });

        InsertActivity(new Activity
        {
            ContactId = anaId,
            Type = "Llamada",
            Subject = "Confirmar alcance del presupuesto",
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)).ToString("yyyy-MM-dd"),
            IsDone = false,
            Notes = "Preguntar si necesitan importación desde Excel."
        });

        InsertActivity(new Activity
        {
            ContactId = carlosId,
            Type = "Email",
            Subject = "Enviar recordatorio de renovación",
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)).ToString("yyyy-MM-dd"),
            IsDone = false,
            Notes = "Adjuntar propuesta actualizada."
        });
    }

    public IEnumerable<Contact> SearchContacts(string search)
    {
        using IDbConnection connection = OpenConnection();

        if (string.IsNullOrWhiteSpace(search))
        {
            return connection.Query<Contact>("""
                SELECT *
                FROM Contacts
                ORDER BY Name;
                """).ToList();
        }

        string like = $"%{search.Trim()}%";
        return connection.Query<Contact>("""
            SELECT *
            FROM Contacts
            WHERE Name LIKE @like
               OR Company LIKE @like
               OR Email LIKE @like
               OR Phone LIKE @like
               OR Status LIKE @like
            ORDER BY Name;
            """, new { like }).ToList();
    }

    public long InsertContact(Contact contact)
    {
        using IDbConnection connection = OpenConnection();
        return connection.ExecuteScalar<long>("""
            INSERT INTO Contacts (Name, Company, Email, Phone, Status, Notes)
            VALUES (@Name, @Company, @Email, @Phone, @Status, @Notes);
            SELECT last_insert_rowid();
            """, contact);
    }

    public void UpdateContact(Contact contact)
    {
        using IDbConnection connection = OpenConnection();
        connection.Execute("""
            UPDATE Contacts
            SET Name = @Name,
                Company = @Company,
                Email = @Email,
                Phone = @Phone,
                Status = @Status,
                Notes = @Notes
            WHERE Id = @Id;
            """, contact);
    }

    public void DeleteContact(long id)
    {
        using IDbConnection connection = OpenConnection();
        connection.Execute("DELETE FROM Contacts WHERE Id = @id;", new { id });
    }

    public IEnumerable<Opportunity> GetOpportunities(long contactId)
    {
        using IDbConnection connection = OpenConnection();
        return connection.Query<Opportunity>("""
            SELECT *
            FROM Opportunities
            WHERE ContactId = @contactId
            ORDER BY ExpectedCloseDate, Id;
            """, new { contactId }).ToList();
    }

    public long InsertOpportunity(Opportunity opportunity)
    {
        using IDbConnection connection = OpenConnection();
        return connection.ExecuteScalar<long>("""
            INSERT INTO Opportunities (ContactId, Title, Stage, Value, ExpectedCloseDate, Notes)
            VALUES (@ContactId, @Title, @Stage, @Value, @ExpectedCloseDate, @Notes);
            SELECT last_insert_rowid();
            """, opportunity);
    }

    public void UpdateOpportunity(Opportunity opportunity)
    {
        using IDbConnection connection = OpenConnection();
        connection.Execute("""
            UPDATE Opportunities
            SET Title = @Title,
                Stage = @Stage,
                Value = @Value,
                ExpectedCloseDate = @ExpectedCloseDate,
                Notes = @Notes
            WHERE Id = @Id;
            """, opportunity);
    }

    public void DeleteOpportunity(long id)
    {
        using IDbConnection connection = OpenConnection();
        connection.Execute("DELETE FROM Opportunities WHERE Id = @id;", new { id });
    }

    public IEnumerable<Activity> GetActivities(long contactId)
    {
        using IDbConnection connection = OpenConnection();
        return connection.Query<Activity>("""
            SELECT *
            FROM Activities
            WHERE ContactId = @contactId
            ORDER BY IsDone, DueDate, Id;
            """, new { contactId }).ToList();
    }

    public long InsertActivity(Activity activity)
    {
        using IDbConnection connection = OpenConnection();
        return connection.ExecuteScalar<long>("""
            INSERT INTO Activities (ContactId, Type, Subject, DueDate, IsDone, Notes)
            VALUES (@ContactId, @Type, @Subject, @DueDate, @IsDone, @Notes);
            SELECT last_insert_rowid();
            """, activity);
    }

    public void CompleteActivity(long id)
    {
        using IDbConnection connection = OpenConnection();
        connection.Execute("UPDATE Activities SET IsDone = 1 WHERE Id = @id;", new { id });
    }

    public void DeleteActivity(long id)
    {
        using IDbConnection connection = OpenConnection();
        connection.Execute("DELETE FROM Activities WHERE Id = @id;", new { id });
    }

    private IDbConnection OpenConnection()
    {
        SqliteConnection connection = new(connectionString);
        connection.Open();
        connection.Execute("PRAGMA foreign_keys = ON;");
        return connection;
    }
}

public sealed class Contact
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Company { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Status { get; set; } = "Nuevo";
    public string Notes { get; set; } = "";
    public string CreatedAt { get; set; } = "";

    public static Contact Empty()
    {
        return new Contact { Status = "Nuevo" };
    }

    public Contact Clone()
    {
        return new Contact
        {
            Id = Id,
            Name = Name,
            Company = Company,
            Email = Email,
            Phone = Phone,
            Status = Status,
            Notes = Notes,
            CreatedAt = CreatedAt
        };
    }
}

public sealed class Opportunity
{
    public long Id { get; set; }
    public long ContactId { get; set; }
    public string Title { get; set; } = "";
    public string Stage { get; set; } = "Nueva";
    public decimal Value { get; set; }
    public string ExpectedCloseDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(7)).ToString("yyyy-MM-dd");
    public string Notes { get; set; } = "";
    public string CreatedAt { get; set; } = "";

    public static Opportunity Empty(long contactId)
    {
        return new Opportunity
        {
            ContactId = contactId,
            Stage = "Nueva",
            ExpectedCloseDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)).ToString("yyyy-MM-dd")
        };
    }

    public Opportunity Clone()
    {
        return new Opportunity
        {
            Id = Id,
            ContactId = ContactId,
            Title = Title,
            Stage = Stage,
            Value = Value,
            ExpectedCloseDate = ExpectedCloseDate,
            Notes = Notes,
            CreatedAt = CreatedAt
        };
    }
}

public sealed class Activity
{
    public long Id { get; set; }
    public long ContactId { get; set; }
    public string Type { get; set; } = "Tarea";
    public string Subject { get; set; } = "";
    public string DueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1)).ToString("yyyy-MM-dd");
    public bool IsDone { get; set; }
    public string Notes { get; set; } = "";
    public string CreatedAt { get; set; } = "";

    public static Activity Empty(long contactId)
    {
        return new Activity
        {
            ContactId = contactId,
            Type = "Tarea",
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)).ToString("yyyy-MM-dd")
        };
    }
}
