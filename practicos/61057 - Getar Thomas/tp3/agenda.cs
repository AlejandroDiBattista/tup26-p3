using System;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;

namespace AgendaApp
{
    // Modelo Contacto
    public class Contact
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
    }

    // Persistencia SQLite
    public class ContactRepository : IDisposable
    {
        private SqliteConnection _conn;

        public ContactRepository(string dbFile = "agenda.db")
        {
            _conn = new SqliteConnection($"Data Source={dbFile}");
            _conn.Open();
            CreateTable();
        }

        private void CreateTable()
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS contacts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Phone TEXT,
                    Email TEXT
                )";
            cmd.ExecuteNonQuery();
        }

        public int AddContact(Contact contact)
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText = "INSERT INTO contacts (Name, Phone, Email) VALUES (@name, @phone, @email); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@name", contact.Name);
            cmd.Parameters.AddWithValue("@phone", contact.Phone ?? "");
            cmd.Parameters.AddWithValue("@email", contact.Email ?? "");

            long id = (long)cmd.ExecuteScalar();
            return (int)id;
        }

        public List<Contact> GetContacts(string search = null)
        {
            var contacts = new List<Contact>();
            var cmd = _conn.CreateCommand();

            if (!string.IsNullOrEmpty(search))
            {
                cmd.CommandText = "SELECT Id, Name, Phone, Email FROM contacts WHERE Name LIKE @search OR Phone LIKE @search OR Email LIKE @search ORDER BY Name";
                cmd.Parameters.AddWithValue("@search", $"%{search}%");
            }
            else
            {
                cmd.CommandText = "SELECT Id, Name, Phone, Email FROM contacts ORDER BY Name";
            }

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    contacts.Add(new Contact
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Phone = reader.GetString(2),
                        Email = reader.GetString(3)
                    });
                }
            }
            return contacts;
        }

        public Contact GetContact(int id)
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Phone, Email FROM contacts WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);

            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    return new Contact
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Phone = reader.GetString(2),
                        Email = reader.GetString(3)
                    };
                }
            }
            return null;
        }

        public void UpdateContact(Contact contact)
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText = "UPDATE contacts SET Name = @name, Phone = @phone, Email = @email WHERE Id = @id";
            cmd.Parameters.AddWithValue("@name", contact.Name);
            cmd.Parameters.AddWithValue("@phone", contact.Phone ?? "");
            cmd.Parameters.AddWithValue("@email", contact.Email ?? "");
            cmd.Parameters.AddWithValue("@id", contact.Id);

            cmd.ExecuteNonQuery();
        }

        public void DeleteContact(int id)
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText = "DELETE FROM contacts WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);

            cmd.ExecuteNonQuery();
        }

        public void Dispose()
        {
            _conn?.Close();
            _conn?.Dispose();
        }
    }

    // JSON Import / Export
    public static class JsonHandler
    {
        public static void ExportToJson(string filename, List<Contact> contacts)
        {
            string jsonString = JsonSerializer.Serialize(contacts, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filename, jsonString);
        }

        public static List<Contact> ImportFromJson(string filename)
        {
            var contacts = new List<Contact>();
            try
            {
                string jsonString = File.ReadAllText(filename);
                contacts = JsonSerializer.Deserialize<List<Contact>>(jsonString);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error al importar JSON: {e.Message}");
            }
            return contacts ?? new List<Contact>();
        }
    }

    // Interfaz de usuario en consola
    class Program
    {
        static void PrintMenu()
        {
            Console.WriteLine("\n--- Agenda ---");
            Console.WriteLine("1) Listar contactos");
            Console.WriteLine("2) Añadir contacto");
            Console.WriteLine("3) Modificar contacto");
            Console.WriteLine("4) Eliminar contacto");
            Console.WriteLine("5) Buscar contactos");
            Console.WriteLine("6) Exportar a JSON");
            Console.WriteLine("7) Importar desde JSON");
            Console.WriteLine("0) Salir");
            Console.Write("Seleccione opción: ");
        }

        static void PrintContacts(List<Contact> contacts)
        {
            if (contacts.Count == 0)
            {
                Console.WriteLine("No hay contactos.");
                return;
            }
            Console.WriteLine("\nID | Nombre               | Teléfono           | Email");
            Console.WriteLine(new string('-', 60));
            foreach (var c in contacts)
            {
                Console.WriteLine($"{c.Id,-3}| {c.Name,-20}| {c.Phone,-18}| {c.Email}");
            }
        }

        static string Prompt(string label, string defaultValue = "")
        {
            Console.Write($"{label}" + (defaultValue != "" ? $" [{defaultValue}]" : "") + ": ");
            string input = Console.ReadLine().Trim();
            return string.IsNullOrEmpty(input) ? defaultValue : input;
        }

        static void Main(string[] args)
        {
            using (var db = new ContactRepository())
            {
                bool running = true;
                while (running)
                {
                    PrintMenu();
                    string choice = Console.ReadLine().Trim();

                    switch (choice)
                    {
                        case "1":
                            var all = db.GetContacts();
                            PrintContacts(all);
                            break;

                        case "2":
                            Console.WriteLine("\nAñadir nuevo contacto:");
                            string name = "";
                            while (string.IsNullOrEmpty(name))
                            {
                                name = Prompt("Nombre");
                                if (string.IsNullOrEmpty(name))
                                    Console.WriteLine("El nombre es obligatorio.");
                            }
                            string phone = Prompt("Teléfono");
                            string email = Prompt("Email");

                            db.AddContact(new Contact { Name = name, Phone = phone, Email = email });
                            Console.WriteLine("Contacto añadido.");
                            break;

                        case "3":
                            Console.Write("ID del contacto a modificar: ");
                            if (int.TryParse(Console.ReadLine(), out int modId))
                            {
                                var contact = db.GetContact(modId);
                                if (contact == null)
                                {
                                    Console.WriteLine("Contacto no encontrado.");
                                    break;
                                }
                                Console.WriteLine("Modifique datos (deje vacío para mantener):");
                                string newName = Prompt("Nombre", contact.Name);
                                if(string.IsNullOrEmpty(newName)) {
                                    Console.WriteLine("El nombre no puede quedar vacío.");
                                    break;
                                }
                                string newPhone = Prompt("Teléfono", contact.Phone);
                                string newEmail = Prompt("Email", contact.Email);

                                contact.Name = newName;
                                contact.Phone = newPhone;
                                contact.Email = newEmail;
                                db.UpdateContact(contact);
                                Console.WriteLine("Contacto actualizado.");
                            }
                            else
                            {
                                Console.WriteLine("ID inválido.");
                            }
                            break;

                        case "4":
                            Console.Write("ID del contacto a eliminar: ");
                            if (int.TryParse(Console.ReadLine(), out int delId))
                            {
                                var contact = db.GetContact(delId);
                                if (contact == null)
                                {
                                    Console.WriteLine("Contacto no encontrado.");
                                    break;
                                }
                                db.DeleteContact(delId);
                                Console.WriteLine("Contacto eliminado.");
                            }
                            else
                            {
                                Console.WriteLine("ID inválido.");
                            }
                            break;

                        case "5":
                            Console.Write("Buscar: ");
                            string term = Console.ReadLine().Trim();
                            var results = db.GetContacts(term);
                            PrintContacts(results);
                            break;

                        case "6":
                            Console.Write("Nombre archivo para exportar (ej. contactos.json): ");
                            string exportFile = Console.ReadLine().Trim();
                            if (!string.IsNullOrEmpty(exportFile))
                            {
                                var contacts = db.GetContacts();
                                JsonHandler.ExportToJson(exportFile, contacts);
                                Console.WriteLine($"Exportado a {exportFile}");
                            }
                            break;

                        case "7":
                            Console.Write("Nombre archivo para importar (ej. contactos.json): ");
                            string importFile = Console.ReadLine().Trim();
                            if (!string.IsNullOrEmpty(importFile) && File.Exists(importFile))
                            {
                                var contactsToImport = JsonHandler.ImportFromJson(importFile);
                                int added = 0;
                                foreach (var c in contactsToImport)
                                {
                                    if (!string.IsNullOrEmpty(c.Name))
                                    {
                                        db.AddContact(c);
                                        added++;
                                    }
                                }
                                Console.WriteLine($"Importados {added} contactos.");
                            }
                            else
                            {
                                Console.WriteLine("Archivo no encontrado.");
                            }
                            break;

                        case "0":
                            running = false;
                            Console.WriteLine("Saliendo...");
                            break;

                        default:
                            Console.WriteLine("Opción no válida.");
                            break;
                    }
                }
            }
        }
    }
}
