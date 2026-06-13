# Alumnos · TUP 2026 · Programación III

Herramienta de consola para administrar la lista de alumnos, exportar información, revisar presentaciones y automatizar tareas operativas del cursado.

## Requisitos

- .NET 10 SDK
- Ejecutar los comandos desde la carpeta `alumnos/`
- Para los comandos de pull requests: `gh` autenticado contra GitHub
- Para los comandos de WhatsApp: `wacli` configurado

## Uso básico

```bash
dotnet run
```

Si ejecutás la app **sin argumentos**, se abre una interfaz interactiva construida con `Spectre.Console`.

También podés usar la línea de comandos tradicional con `Spectre.Console.CLI`:

```bash
dotnet run -- --help
dotnet run -- listar-alumnos
dotnet run -- publicar-practico TP3 --forzar
dotnet run -- revisar-presentaciones 3
dotnet run -- revisar-presentaciones
```

- En los comandos que reciben un práctico, se acepta `1`, `tp1` o `TP1`.
- Cuando una ruta de salida es opcional, si no se informa se usa la ruta por defecto del proyecto.

## Comandos

### Operaciones principales

- `listar-alumnos`: muestra todos los alumnos.
- `contar-asistencias`: reconstruye las asistencias hasta hoy y marca los presentes del día a partir de WhatsApp.

### Pull requests y prácticos

Los títulos de los PRs se normalizan automáticamente antes de revisarlos, descargarlos o cerrarlos.

- `revisar-prs`: revisa pull requests de los alumnos.
- `bajar-prs`: descarga y sobrescribe todos los prácticos detectados en los PRs, y luego revisa automáticamente los TP presentados.
- `cerrar-prs`: cierra todos los PRs abiertos.
- `publicar-practico <tp> [--forzar]`: copia el enunciado del práctico indicado a la carpeta de cada alumno.
- `publicar-apuntes`: ejecuta `publicar.py` con `apuntes/` como directorio de trabajo.

Las carpetas de alumnos se crean o normalizan automáticamente antes de los comandos que las recorren o modifican.

### Auditoría

- `listar-practicos-faltantes <tp>`: lista alumnos a quienes les falta el trabajo práctico indicado.

### Exportación

- `exportar-estado`: publica un resumen de estado en `ESTADO.md` en la raíz del repositorio.
- `exportar-markdown [ruta]`: exporta la lista en Markdown. Ruta por defecto: `alumnos.md`.
- `exportar-json [ruta]`: exporta la lista en JSON. Ruta por defecto: `alumnos.json`.
- `exportar-vcard [ruta]`: exporta contactos en formato vCard. Ruta por defecto: `alumnos.vcf`.

### Utilidades

- `listar-grupos-whatsapp`: lista grupos y participantes de WhatsApp.
- `revisar-presentaciones [tp]`: marca presentaciones a partir del código local de cada carpeta. Si no se indica un TP, revisa todos los prácticos que tengan enunciado y criterio configurado.
	- `TP1`: presentado si tiene al menos 100 líneas totales.
	- `TP2`: presentado si agrega al menos 20 líneas respecto del enunciado.
	- `TP3`: presentado si agrega al menos 50 líneas respecto del enunciado.
	- `TP4`: presentado si agrega al menos 150 líneas respecto del enunciado.
- `limpiar-archivos-temporales`: elimina `bin`, `obj`, `.vs` y cachés de compilación dentro de `practicos/`, `enunciados/` y `clases/`.

## Archivos de referencia

- `alumnos.md`: listado principal de alumnos.
- `alumnos.json`: exportación JSON.
- `alumnos.vcf`: contactos en formato vCard.
- `agenda.html`: vista HTML de agenda/listado.
- `recuperacion.md`: comunicación y seguimiento de recuperatorios.
- `resultado-examenes.md`: resumen de exámenes.
- `ESTADO.md`: estado resumido generado para el repositorio.
- `practicos/`: carpetas locales por alumno.
- `enunciados/`: enunciados base de los trabajos prácticos.
- `apuntes/`: fuentes y script de publicación de los apuntes.
