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
dotnet run -- listar
dotnet run -- publicar TP3 --forzar
dotnet run -- revisar-presentados 3
```

- En los comandos que reciben un práctico, se acepta `1`, `tp1` o `TP1`.
- Cuando una ruta de salida es opcional, si no se informa se usa la ruta por defecto del proyecto.

## Comandos

### Auditoría y listados

- `listar`: muestra todos los alumnos.
- `sin-github`: lista alumnos sin cuenta de GitHub.
- `sin-telefono`: lista alumnos sin teléfono.
- `sin-foto`: lista alumnos sin foto y sincroniza ese estado desde las carpetas locales.
- `tp-no-presentado <tp>`: lista alumnos que no presentaron el trabajo práctico indicado, ignorando quienes no presentaron ningún práctico.
- `tp1-no-presentado`: alias de `tp-no-presentado TP1`.
- `tp2-no-presentado`: alias de `tp-no-presentado TP2`.
- `sin-practicos`: lista alumnos que no presentaron ningún práctico.
- `limpiar-proyectos-practicos`: elimina `bin`, `obj`, `.vs` y cachés de compilación dentro de `practicos/`.

### Exportación

- `guardar [ruta]`: exporta la lista en Markdown. Ruta por defecto: `alumnos.md`.
- `json [ruta]`: exporta la lista en JSON. Ruta por defecto: `alumnos.json`.
- `vcf [ruta]`: exporta contactos en formato vCard. Ruta por defecto: `alumnos.vcf`.
- `informar-estado`: publica un resumen de estado en `ESTADO.md` en la raíz del repositorio.

### Carpetas y enunciados

- `crear-carpetas`: crea o normaliza las carpetas de prácticos de cada alumno.
- `publicar <tp> [--forzar]`: copia el enunciado del práctico indicado a la carpeta de cada alumno.
- `publicar-rehacer <tp>`: borra y republica el práctico solo en alumnos cuyo estado para ese TP sea Revisar.

### Pull requests y presentaciones

- `prs`: revisa pull requests de los alumnos.
- `normalizar-prs [--simular]`: ajusta títulos de pull requests.
- `bajar-prs [tp] [--forzar]`: descarga archivos desde los PRs; sin `tp` baja todos los prácticos detectados por carpetas `tpN`.
- `cerrar-prs [tp]`: cierra todos los PRs abiertos, o solo los del práctico indicado.
- `revisar-presentados <tp>`: marca presentaciones a partir del código local de cada carpeta.
	- `TP1`: presentado si tiene al menos 100 líneas totales.
	- `TP2`: presentado si agrega al menos 20 líneas respecto del enunciado.
	- `TP3`: presentado si agrega al menos 50 líneas respecto del enunciado.

### Asistencias y WhatsApp

- `contar-asistencias`: releva presentes del día a partir de WhatsApp.
- `registrar-asistencias`: consolida los presentes del día como asistencias acumuladas.
- `wapp-grupos`: lista grupos y participantes de WhatsApp.
- `wapp-recuperar-tp1-tp2 [tp] [--simular]`: envía un aviso de recuperación por WhatsApp.
- `wapp-foto-parcial [--simular]`: pide una selfie a alumnos sin foto registrada para el parcial.
- `registrar-respuestas`: lee respuestas de WhatsApp y registra el código informado por cada alumno.

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
