# CRM Agente — MVP

CRM mínimo pero funcional para **Programación III** (UTN Tucumán).
Blazor Server + EF Core + SQLite + un agente de IA con *tool use*.

## Qué demuestra

- **Modelo de dominio** con tres entidades y relaciones 1-N:
  `Contacto` → muchas `Oportunidad` y muchas `Actividad`.
- **EF Core + SQLite**: `DbContext`, `Include`, consultas con LINQ.
- **Arquitectura limpia**: una sola capa de servicios (`CrmService`)
  que consumen *tanto la UI como el agente*. Sin lógica duplicada.
- **Ficha 360**: una vista que compone las tres entidades.
- **Pipeline**: tablero por etapas con totales.
- **Agente IA**: consulta en lenguaje natural traducida a llamadas de
  herramienta sobre el dominio real (el LLM razona, el código ejecuta).

## Cómo correrlo

Requiere el SDK de .NET 10.

```bash
# 1. (Opcional) configurar el agente de IA
export ANTHROPIC_API_KEY="sk-ant-..."

# 2. Restaurar y correr
dotnet restore
dotnet run
```

Abrir el navegador en la URL que imprime la consola (ej. https://localhost:5001).

La base `crm.db` se crea sola al arrancar, con datos de ejemplo.

> Si no configurás la API key, el CRM funciona igual — solo la página
> del Agente queda deshabilitada. Esa es la idea: la IA es una capa
> encima, no el corazón de la app.

## El punto pedagógico del agente

El LLM **no** ve la base de datos. Recibe descripciones de herramientas
(`buscar_oportunidades`), decide cuál llamar y con qué parámetros, y
nuestro código ejecuta el método real del `CrmService`. El resultado
vuelve al modelo, que lo redacta en lenguaje natural.

La herramienta del agente **es** un método del repositorio. Si la capa
de datos no funciona, el agente no sirve. No se puede delegar el
aprendizaje del modelo de datos.
