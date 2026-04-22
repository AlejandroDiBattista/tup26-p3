# Verificación Creacion de Ambiente de Desarrollo, Terminal, Git

---

## Terminal y Bash para desarrollo

### Bash y shell

1) ¿Que es Bash?
- [ ] Una interfaz grafica para administrar archivos.
- [ ] Un sistema operativo.
- [x] Un shell que interpreta comandos y ejecuta programas.

---

### Terminal y shell

2) ¿Cual es la diferencia principal entre terminal y shell?
- [x] La terminal es la aplicacion donde escribis y el shell es quien interpreta los comandos.
- [ ] No hay diferencia, son exactamente lo mismo.
- [ ] La terminal interpreta comandos y el shell solo muestra texto.

---

### Lectura de comandos

3) En la forma general `comando [opciones] [argumentos]`, ¿que son `-l` y `-a` en `ls -la`?
- [ ] Variables de entorno.
- [x] Opciones.
- [ ] Argumentos.

---

### Rutas y espacios

4) ¿Cual de estas formas sirve para cambiar a una carpeta llamada `Mis Documentos` en Bash?
```bash
cd Mis Documentos
cd "Mis Documentos"
cd Mis\ Documentos
```
- [ ] Solo la primera.
- [ ] Las tres.
- [x] La segunda y la tercera.

---

### Variables en Bash

5) ¿Que imprime `echo '$HOME'`?
- [ ] El valor real de la variable `HOME`.
- [x] El texto literal `$HOME`.
- [ ] Un error porque Bash no acepta comillas simples.

---

## Git y GitHub: control de versiones y colaboración

### Git y GitHub

6) ¿Cual afirmacion es correcta?
- [ ] Git solo sirve si se usa junto con GitHub.
- [x] Git guarda historial y GitHub agrega colaboracion y alojamiento remoto.
- [ ] GitHub reemplaza completamente a Git.

---

### Commit

7) ¿Que es un commit en Git?
- [x] Una foto del estado del proyecto en un momento dado.
- [ ] Una rama remota de GitHub.
- [ ] Una carpeta temporal donde se guardan cambios sin historial.

---

### Staging area

8) ¿Para que sirve el staging area?
- [ ] Para fusionar ramas automaticamente.
- [ ] Para borrar archivos del repositorio.
- [x] Para elegir que cambios van a entrar en el proximo commit.

---

### Ramas

9) ¿Para que se usa una rama en Git?
- [ ] Para cambiar el nombre del repositorio.
- [ ] Para guardar contraseñas del proyecto.
- [x] Para desarrollar una idea o correccion sin tocar directamente la rama principal.

---

### Flujo basico con Git

10) En un flujo simple con Git, ¿que paso viene despues de revisar el estado y antes de crear un commit?
- [ ] Borrar el repositorio remoto.
- [x] Agregar los cambios al staging area.
- [ ] Hacer merge a `main`.

