# Alumnos · TUP 2026 · Programación III

Herramienta de consola para administrar la lista de alumnos, exportar la información y revisar recursos vinculados a cada legajo.

## Requisitos

- .NET 10
- Ejecutar los comandos desde la carpeta del proyecto

## Uso

```bash
dotnet run
```

Si ejecutás la app **sin argumentos**, se abre una interfaz interactiva construida con `Spectre.Console`.

También podés usar la línea de comandos tradicional, ahora gestionada con `Spectre.Console.CLI`:

```bash
dotnet run -- --help
dotnet run -- listar
dotnet run -- revisar-presentados TP1
```

## Comandos

- `listar`: muestra todos los alumnos
- `sin-github`: filtra alumnos sin cuenta de GitHub
- `sin-telefono`: filtra alumnos sin teléfono
- `sin-foto`: filtra alumnos sin foto
- `tp1-no-presentado`: lista alumnos que no presentaron el trabajo práctico 1
- `tp2-no-presentado`: lista alumnos que no presentaron el trabajo práctico 2
- `guardar [archivo.md]`: exporta la lista en Markdown
- `json [archivo.json]`: exporta la lista en JSON
- `vcf [archivo.vcf]`: exporta la lista en formato vCard
- `informer-estado`: publica en `README.md` el listado completo con columnas de legajo, nombre y prácticos
- `crear-carpetas`: crea la estructura de carpetas del curso
- `prs`: revisa pull requests de alumnos
- `normalizar-prs [--simular]`: ajusta títulos de pull requests
- `bajar-prs TP1 [--forzar]`: descarga archivos del práctico desde los PRs
- `cerrar-prs [TP1]`: cierra todos los PRs abiertos, o solo los del práctico indicado
- `revisar-presentados TP1`: marca como presentado un TP si supera 200 líneas en los archivos locales de la carpeta del trabajo
- `registrar-asistencias`: consolida los presentes cargados como asistencias
- `relevar-asistencias`: releva presentes del día desde WhatsApp
- `wapp-grupos`: muestra grupos y participantes de WhatsApp
- `wapp-recuperar-tp1-tp2 [--simular]`: envía un aviso de recuperación por WhatsApp a alumnos que no presentaron TP1 ni TP2

## Archivos de referencia

- `alumnos.md`: listado principal de alumnos
- `agenda.html`: vista HTML de la agenda/listado
