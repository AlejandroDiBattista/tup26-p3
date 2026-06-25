# Pruebas manuales TP6

## Preparacion

Copiar `.env.ejemplo` como `.env` y completar el proveedor elegido.

Para OpenAI:

```env
OPENAI_API_URL=https://api.openai.com/v1/chat/completions
OPENAI_API_KEY=tu_clave
OPENAI_MODEL=gpt-5.5
```

Ejecutar:

```bash
dotnet run asistente.cs
```

Tambien se puede elegir proveedor:

```bash
dotnet run asistente.cs -- ollama
dotnet run asistente.cs -- gemini
```

## Casos para probar

1. Enviar un mensaje normal:

```text
Explicame que es recursividad en dos frases.
```

2. Verificar contexto:

```text
Ahora dame un ejemplo de eso en C#.
```

3. Probar listado de archivos:

```text
Lista los archivos de esta carpeta.
```

4. Probar escritura:

```text
Crea notas.txt con una lista de tres temas para estudiar.
```

5. Probar lectura:

```text
Lee notas.txt y resumilo.
```

6. Probar seguridad de rutas:

```text
Lee ../AGENTS.md
```

La respuesta debe indicar que la ruta esta fuera del directorio permitido o que
no se puede acceder.

7. Probar salida:

Presionar `Esc` y verificar que la terminal vuelva normalmente.
