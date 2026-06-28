namespace Tup26.AlumnosApp;

static class ArchivoTexto {
    static readonly HashSet<string> extensionesTexto = new(StringComparer.OrdinalIgnoreCase) {
        ".cs",
        ".cshtml",
        ".csproj",
        ".css",
        ".csv",
        ".editorconfig",
        ".env",
        ".fs",
        ".fsproj",
        ".gitignore",
        ".html",
        ".htm",
        ".http",
        ".js",
        ".json",
        ".md",
        ".razor",
        ".sln",
        ".slnx",
        ".sql",
        ".svg",
        ".ts",
        ".txt",
        ".vb",
        ".vbproj",
        ".xml",
        ".yaml",
        ".yml"
    };

    static readonly HashSet<string> nombresTexto = new(StringComparer.OrdinalIgnoreCase) {
        ".dockerignore",
        ".gitattributes",
        ".gitignore",
        "dockerfile",
        "license",
        "makefile",
        "readme"
    };

    public static bool EsRutaTexto(string ruta) {
        string nombre = Path.GetFileName(ruta);
        if (nombresTexto.Contains(nombre)) {
            return true;
        }

        string extension = Path.GetExtension(ruta);
        return !string.IsNullOrWhiteSpace(extension) && extensionesTexto.Contains(extension);
    }

    public static bool PareceContenidoTexto(byte[] contenido) {
        if (contenido.Length == 0) {
            return true;
        }

        if (contenido.Length >= 2 && ((contenido[0] == 0xff && contenido[1] == 0xfe) || (contenido[0] == 0xfe && contenido[1] == 0xff))) {
            return true;
        }

        if (contenido.Length >= 3 && contenido[0] == 0xef && contenido[1] == 0xbb && contenido[2] == 0xbf) {
            return true;
        }

        int revisar = Math.Min(contenido.Length, 8192);
        for (int i = 0; i < revisar; i++) {
            byte b = contenido[i];
            if (b == 0) {
                return false;
            }
        }

        return true;
    }

    public static bool PareceArchivoTexto(string ruta) {
        byte[] buffer = new byte[8192];
        int leidos;

        try {
            using FileStream stream = File.OpenRead(ruta);
            leidos = stream.Read(buffer, 0, buffer.Length);
        } catch (IOException) {
            return false;
        } catch (UnauthorizedAccessException) {
            return false;
        }

        if (leidos == buffer.Length) {
            return PareceContenidoTexto(buffer);
        }

        byte[] contenido = new byte[leidos];
        Array.Copy(buffer, contenido, leidos);
        return PareceContenidoTexto(contenido);
    }
}
