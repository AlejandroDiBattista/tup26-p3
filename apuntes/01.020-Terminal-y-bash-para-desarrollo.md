# Terminal y Bash para desarrollo

## ¿Qué es la terminal?

La **terminal** es una aplicación de texto. Su trabajo es mostrar un prompt, dejarte escribir comandos y mostrar la salida.

La terminal no interpreta los comandos por sí sola. El programa que los interpreta es el **shell**.

## ¿Qué es Bash?

**Bash** es un shell. Lee lo que escribís, lo procesa y ejecuta el comando correspondiente.

Cuando escribís algo como:

```bash
ls -la
```

Bash hace varias tareas antes de ejecutar `ls`:

- separa el texto en partes,
- interpreta comillas y barras invertidas,
- expande variables como `$HOME`,
- expande atajos como `~`,
- resuelve comodines como `*.txt`,
- aplica redirecciones y pipes,
- y recién después ejecuta el programa.

Por eso, aprender Bash no es solo memorizar comandos: también es entender cómo el shell transforma lo que escribís.

## Terminal, shell y sistema operativo

- **Terminal**: la aplicación o ventana donde escribís.
- **Shell**: el intérprete de comandos.
- **Bash**: un shell concreto.

En Linux, Bash es muy común. En macOS moderno, el shell por defecto suele ser **zsh**, no Bash, pero casi todos los ejemplos básicos de este apunte funcionan igual en ambos. En Windows, el shell por defecto es **CMD** o **PowerShell**, pero también podés usar Bash a través de herramientas como Git Bash o WSL.

## ¿Para qué sirve?

Bash se usa para:

- navegar por carpetas y archivos,
- crear, copiar, mover y borrar contenido,
- ejecutar programas,
- automatizar tareas repetitivas,
- combinar herramientas pequeñas para resolver trabajos grandes,
- trabajar más rápido que con una interfaz gráfica en muchas tareas técnicas.

## Usar Bash en Windows

En Windows hay dos opciones comunes:

- **Git Bash**: simple y suficiente para empezar.
- **WSL**: más parecido a un Linux real.

Para una primera práctica, **Git Bash** suele alcanzar.

Con **Git Bash** tenés acceso a comandos típicos de Bash como:

- `ls`
- `cp`
- `grep`
- `cat`
- `pwd`
- `mkdir`
- `rm`

En **CMD** o **PowerShell** no siempre vas a tener exactamente esos mismos comandos con comportamiento Bash. En PowerShell, por ejemplo, algunos nombres existen como alias, pero no son necesariamente los comandos GNU originales.

### Instalar Git Bash

1. Instalá **Git for Windows**.
2. Abrí **Git Bash** desde el menú Inicio.
3. Verificá la instalación con:


```bash
git --version
bash --version
which git
which bash
```

Si probás desde **CMD** o **PowerShell**, en lugar de `which` usá:

```bash
where git
where bash
```

### Cómo usarlo en VS Code

1. Abrí VS Code.
2. Presioná `Ctrl + Shift + P` y escribí "Terminal: Select Default Profile".
3. Elegí "Git Bash" como terminal por defecto.
4. Abrí una terminal nueva dentro de VS Code.

De esa forma, seguís trabajando dentro de VS Code, pero el shell que corre en la terminal integrada es Git Bash.

### Rutas en Git Bash sobre Windows

En **Git Bash**, lo normal es usar `/` para separar directorios.

Ejemplos:

```bash
/c/Users/Ana/Documentos
./carpeta/archivo.txt
../otra-carpeta/archivo.txt
```

Aunque Windows suele usar `\`, en Bash ese carácter normalmente se interpreta como escape. Por eso, para escribir rutas en Git Bash conviene usar `/`.

## Cómo leer un comando

La forma general es esta:

```text
comando [opciones] [argumentos]
```

Ejemplos:

```bash
ls -la
cp origen.txt copia.txt
grep -i "bash" notas.txt
dotnet run programa.cs
```

- `ls`, `cp`, `grep`, `dotnet` son comandos.
- `-l`, `-a`, `-i` son opciones.
- `origen.txt`, `copia.txt`, `notas.txt`, `programa.cs` son argumentos.

## Directorio actual y rutas

### Directorio actual

Es la carpeta en la que estás parado en este momento. Muchos comandos trabajan sobre esa ubicación.

Para verla:

```bash
pwd
```

### Ruta

Una ruta indica dónde está un archivo o una carpeta.

Puede ser:

- **absoluta**: empieza desde la raíz del sistema.
- **relativa**: se interpreta desde el directorio actual.

Ejemplos:

```bash
/Users/usuario/Documentos
proyecto/archivo.txt
```

Atajos importantes:

- `.`: directorio actual.
- `..`: directorio padre.
- `~`: directorio personal del usuario actual.

Ejemplos:

```bash
cd .
cd ..
cd ~
ls .
ls ..
```

### Comillas y espacios en rutas

Si una ruta tiene espacios, tenés que protegerla.

Esto falla porque Bash separa por espacios:

```bash
cd Mis Documentos
```

Esto funciona:

```bash
cd "Mis Documentos"
cd Mis\ Documentos
```

Regla útil:

- comillas dobles `"..."`: mantienen el texto junto, pero permiten expandir variables.
- comillas simples `'...'`: mantienen el texto literal, sin expandir variables.

Ejemplos:

```bash
echo "$HOME"
echo '$HOME'
```

La primera línea muestra el valor de la variable. La segunda imprime literalmente `$HOME`.

## Primeros comandos importantes

### `pwd`

Muestra la ruta del directorio actual.

```bash
pwd
```

### `ls`

Lista el contenido de una carpeta.

```bash
ls
ls -l
ls -a
ls -la
```

Opciones comunes:

- `-l`: formato largo, con permisos, tamaño y fecha.
- `-a`: incluye archivos ocultos.

En Unix, un archivo oculto suele empezar con `.`.

También podés usar patrones:

```bash
ls *.txt
ls a*
ls ??-*.md
```

Estos patrones no los interpreta `ls`: los interpreta Bash antes de ejecutar el comando. Se llaman **globs** o **comodines**.

- `*.txt`: nombres que terminan en `.txt`
- `a*`: nombres que empiezan con `a`
- `??-*.md`: dos caracteres, luego `-`, luego cualquier texto y al final `.md`

### `cd`

Cambia de directorio.

```bash
cd Documentos
cd ..
cd ~
cd
cd -
```

- `cd ..`: sube un nivel.
- `cd ~`: va al directorio personal.
- `cd` sin argumentos: también va al directorio personal.
- `cd -`: vuelve al directorio anterior.

### `mkdir`

Crea carpetas.

```bash
mkdir proyecto
mkdir docs src
mkdir -p proyecto/src/modelos
```

- `-p`: crea también carpetas intermedias si faltan.

### `touch`

Si el archivo no existe, lo crea vacío. Si ya existe, actualiza su fecha de modificación.

```bash
touch notas.txt
touch informe.md
```

### `cp`

Copia archivos o carpetas.

```bash
cp origen.txt copia.txt
cp archivo.txt Documentos/
cp -r carpeta1 carpeta2
```

- Si el destino es una carpeta existente, copia adentro.
- Si el destino no existe, crea una copia con ese nombre.
- `-r` copia carpetas de forma recursiva.

### `mv`

Mueve o renombra.

```bash
mv viejo.txt nuevo.txt
mv archivo.txt Documentos/
mv carpeta_vieja carpeta_nueva
```

`mv` no duplica: cambia la ubicación o el nombre.

### `rm`

Elimina archivos o carpetas.

```bash
rm archivo.txt
rm -r carpeta_vieja
rm -ri carpeta_vieja
```

- `-r`: borra de forma recursiva.
- `-i`: pide confirmación.

Importante:

- `rm` no manda a la papelera.
- si escribís mal la ruta, podés borrar otra cosa.
- conviene revisar con `pwd` y `ls` antes de usarlo.

### `cat`

Muestra el contenido completo de un archivo en la salida estándar.

```bash
cat archivo.txt
```

Para archivos largos, suele ser más cómodo usar `less`:

```bash
less archivo.txt
```

Con `less` podés desplazarte. Salís con `q`.

### `head` y `tail`

Sirven para ver el principio o el final de un archivo.

```bash
head archivo.txt
tail archivo.txt
head -n 5 archivo.txt
tail -n 20 archivo.txt
```

### `echo`

Imprime texto.

```bash
echo Hola mundo
echo "$HOME"
```

`echo` es útil para casos simples. Si necesitás más control sobre el formato, `printf` suele ser más predecible:

```bash
printf "Hola %s\n" "mundo"
```

### `clear`

Limpia la pantalla de la terminal.

```bash
clear
```

## Ejecutar programas

Para ejecutar un programa instalado, escribís su nombre:

```bash
git --version
dotnet --info
```

Para ejecutar un archivo del directorio actual, normalmente necesitás anteponer `./`:

```bash
./programa.sh
```

Eso pasa porque, por seguridad, el directorio actual no suele estar en el `PATH`.

Si el archivo no tiene permiso de ejecución, podés hacerlo ejecutable con:

```bash
chmod +x programa.sh
```

O ejecutarlo invocando Bash de forma explícita:

```bash
bash programa.sh
```

## Entrada, salida y error

Todo comando trabaja, como mínimo, con estos tres flujos:

- **entrada estándar** (`stdin`): lo que el programa recibe.
- **salida estándar** (`stdout`): resultado normal.
- **salida de error** (`stderr`): mensajes de error.

En Bash se suelen representar con estos números:

- `0`: `stdin`
- `1`: `stdout`
- `2`: `stderr`

## Redirección

La redirección permite cambiar de dónde lee un comando o a dónde escribe.

### `>`

Redirige la salida estándar a un archivo y lo sobreescribe.

```bash
ls > listado.txt
```

### `>>`

Agrega al final del archivo.

```bash
date >> historial.txt
```

### `<`

Hace que el comando lea desde un archivo.

```bash
sort < nombres.txt
```

### `2>`

Redirige los errores a un archivo.

```bash
ls carpeta-que-no-existe 2> errores.txt
```

### `2>&1`

Hace que `stderr` vaya al mismo destino que `stdout`.

```bash
comando > salida.txt 2>&1
```

Ese orden importa. En esta forma, tanto la salida normal como los errores terminan en `salida.txt`.

### Ejemplos útiles

Guardar un listado:

```bash
ls -la > contenido.txt
```

Guardar errores sin mezclar con la salida normal:

```bash
grep "hola" archivo-inexistente.txt > salida.txt 2> errores.txt
```

Descartar mensajes de error:

```bash
ls carpeta-que-no-existe 2> /dev/null
```

## Pipes

El símbolo `|` conecta la salida estándar de un comando con la entrada estándar del siguiente.

```bash
comando1 | comando2
```

Eso no conecta los errores: `stderr` sigue aparte, salvo que lo redirijas.

Ejemplos:

```bash
ls | wc -l
cat nombres.txt | sort
history | grep cd
```

## Filtros muy usados

### `grep`

Busca líneas que coincidan con un patrón.

```bash
grep "bash" archivo.txt
grep -i "bash" archivo.txt
ls | grep '\.md$'
```

- `-i`: ignora mayúsculas y minúsculas.

Importante:

- los globs como `*.txt` son cosa del shell;
- `grep` usa **expresiones regulares**, no globs.

Por eso:

```bash
ls *.txt
```

usa un patrón de Bash, mientras que:

```bash
ls | grep '\.txt$'
```

usa una expresión regular de `grep`.

### `sort`

Ordena líneas.

```bash
sort nombres.txt
sort < nombres.txt
```

### `wc -l`

Cuenta líneas.

```bash
wc -l alumnos.txt
ls | wc -l
```

## Redirección y pipes juntos

Se pueden combinar:

```bash
ls -la | grep '\.md$' > markdowns.txt
grep -i "bash" apuntes.txt | sort
```

En la primera línea:

1. `ls -la` genera la salida,
2. `grep` filtra solo líneas que terminan en `.md`,
3. `>` guarda el resultado final en `markdowns.txt`.

## Variables de entorno

Una variable de entorno guarda información que Bash y otros programas pueden usar.

Ejemplos frecuentes:

- `HOME`: carpeta personal.
- `PATH`: lista de carpetas donde se buscan comandos.
- `USER`: nombre del usuario.

Ver una variable:

```bash
echo "$HOME"
echo "$PATH"
```

Definir una variable para la sesión actual:

```bash
NOMBRE="Ana"
echo "$NOMBRE"
```

Exportarla para que la vean otros procesos:

```bash
export NOMBRE="Ana"
```

Definir una variable solo para un comando:

```bash
NOMBRE="Ana" printenv NOMBRE
```

## Cómo pedir ayuda

Dos herramientas muy útiles son:

```bash
man ls
type cd
```

- `man ls`: abre el manual del comando `ls`.
- `type cd`: indica si algo es un comando externo, un builtin, un alias o una función.

También es común usar:

```bash
which bash
```

`which` muestra qué ejecutable se está usando, aunque `type` suele ser más exacto dentro del shell.

En muchos programas también funciona:

```bash
comando --help
```

Pero no es universal. En macOS varios comandos del sistema no aceptan `--help` y usan `man`.

## Historial y edición de línea

### Historial

Podés recuperar comandos anteriores con:

- `↑` y `↓`
- `history`
- `Ctrl + R` para buscar en el historial

### Atajos útiles

- `Tab`: autocompleta nombres si puede.
- `Ctrl + C`: interrumpe el comando actual.
- `Ctrl + D`: cierra la entrada actual; si la línea está vacía, suele cerrar la sesión.
- `Ctrl + A`: va al inicio de la línea.
- `Ctrl + E`: va al final de la línea.
- `Ctrl + L`: limpia la pantalla.

## Código de salida

Cuando un comando termina, devuelve un **código de salida**.

Por convención:

- `0` significa éxito.
- un valor distinto de `0` indica algún error o condición especial.

Podés ver el código del último comando con:

```bash
echo $?
```

Ejemplo:

```bash
ls carpeta-que-no-existe
echo $?
```

## Buenas prácticas

- Revisá `pwd` antes de borrar o mover cosas.
- Usá comillas si hay espacios en rutas o nombres.
- Probá primero con `ls` o `echo` cuando no estés seguro de una expansión.
- Preferí `rm -ri` si querés más seguridad.
- Hacé copias antes de cambios grandes.
- No uses `sudo` si no entendés exactamente qué va a hacer el comando.

## Casos habituales

### Ver dónde estás y qué hay

```bash
pwd
ls -la
```

### Entrar a un proyecto

```bash
cd Documentos
cd mi-proyecto
ls
```

### Crear una estructura mínima

```bash
mkdir -p proyecto/src proyecto/docs
cd proyecto
touch README.md
```

### Hacer un respaldo rápido

```bash
cp datos.csv datos-respaldo.csv
```

### Buscar archivos Markdown y guardar el resultado

```bash
ls -la | grep '\.md$' > markdowns.txt
```

### Contar cuántas líneas contienen una palabra

```bash
grep -i "bash" apuntes.txt | wc -l
```

## Resumen rápido

- `pwd`: muestra el directorio actual.
- `ls`: lista contenido.
- `cd`: cambia de directorio.
- `mkdir`: crea carpetas.
- `touch`: crea archivos vacíos o actualiza su fecha.
- `cp`: copia.
- `mv`: mueve o renombra.
- `rm`: elimina.
- `cat`: muestra un archivo completo.
- `less`: permite leerlo por partes.
- `head` y `tail`: muestran el principio o el final.
- `echo`: imprime texto.
- `>`: redirige `stdout` sobrescribiendo.
- `>>`: redirige `stdout` agregando al final.
- `<`: toma `stdin` desde un archivo.
- `2>`: redirige `stderr`.
- `|`: conecta `stdout` con `stdin`.
- `grep`: filtra líneas.
- `sort`: ordena.
- `wc -l`: cuenta líneas.
- `history`: muestra historial.
- `man`: abre el manual.
- `echo $?`: muestra el código de salida anterior.

## Cierre

La terminal no es una pantalla negra que hay que memorizar: es una forma precisa de hablarle al sistema.

Si entendés estas ideas:

- qué hace el shell,
- cómo se interpretan rutas y comillas,
- cómo funcionan entrada, salida, errores y pipes,
- y qué hacen los comandos más comunes,

ya tenés una base sólida para trabajar con Bash con confianza y sin depender solo de prueba y error.
