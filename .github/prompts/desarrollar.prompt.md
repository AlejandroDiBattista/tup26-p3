---
description: Preparar el workspace de un alumno para desarrollar un TP.
argument-hint: tp1 63217
agent: agent
---

Usa las reglas de [Copilot Instructions](../copilot-instructions.md).

Toma estos datos:
- TP: ${input:tp:tp1}
- Legajo: ${input:legajo:63217}

Ejecuta exactamente este comando desde la raiz del repositorio:

```bash
bash scripts/curso/desarrollar.sh "${input:tp}" "${input:legajo}"
```

Reglas:
- No reemplaces el script por comandos manuales si el script ya resolvio el flujo.
- Si el script falla, informa el error exacto y detenete.
- Si el script funciona, resume:
  - carpeta del alumno encontrada
  - rama usada
  - workspace o ventana de VS Code abierta
