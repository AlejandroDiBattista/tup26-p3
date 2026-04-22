---
description: Presentar la rama actual del alumno creando el pull request y limpiando la rama local.
agent: agent
---

Usa las reglas de [Copilot Instructions](../copilot-instructions.md).

Ejecuta exactamente este comando desde la raiz del repositorio:

```bash
bash scripts/curso/presentar.sh
```

Reglas:
- No improvises comandos manuales si el script ya resolvio el flujo.
- Si el script falla, informa el error exacto y detenete.
- Si el script funciona, resume:
  - rama presentada
  - titulo del pull request
  - URL del pull request
  - confirmacion de vuelta a `main`
  - confirmacion de borrado de la rama local
- No borres la rama remota.
