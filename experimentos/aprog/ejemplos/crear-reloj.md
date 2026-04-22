# Instrucciones para generar el reloj analógico

Crear un archivo HTML autocontenido llamado `reloj.html` con una app web que muestre un reloj analógico en tiempo real.

## Requisitos generales

- Debe ser un único archivo HTML.
- Debe funcionar abriéndolo directamente en el navegador.
- Debe incluir HTML, CSS y JavaScript en el mismo archivo.
- Debe mostrar un reloj analógico centrado en pantalla.
- Debe actualizarse en tiempo real.

## Estructura visual

- Fondo de página oscuro (`#111827`).
- Centrar el reloj horizontal y verticalmente usando `display: grid` y `place-items: center`.
- Reloj circular de `300px` por `300px`.
- Borde grueso claro (`12px solid #e5e7eb`).
- Fondo del reloj con un degradado radial claro.
- Sombra suave para dar relieve.

## Esfera del reloj

- Dibujar una marca circular central (`center`) de `14px`.
- Agregar 12 marcas principales de hora con la clase `.mark`.
- Cada marca principal debe estar rotada en incrementos de 30 grados.

## Dígitos

- Agregar los números `12`, `3`, `6` y `9`.
- Posicionarlos alrededor de la esfera:
  - `12` arriba
  - `3` a la derecha
  - `6` abajo
  - `9` a la izquierda
- Usar una clase base `.number` y clases de posición específicas:
  - `.num-12`
  - `.num-3`
  - `.num-6`
  - `.num-9`

## Marcas de minutos

- Agregar marcas pequeñas para los minutos.
- Deben rodear la esfera completa.
- Usar una clase `.minute-mark`.
- Cada marca debe ser más fina y corta que las marcas principales.
- Deben estar rotadas alrededor del centro en incrementos de 6 grados, omitiendo las posiciones de las horas principales.

## Agujas

- Agregar tres agujas:
  - hora
  - minuto
  - segundo
- Todas deben estar posicionadas desde el centro hacia arriba.
- Deben usar `transform-origin: bottom center`.
- Deben rotar dinámicamente con JavaScript.

### Estilos de las agujas

- Aguja horaria:
  - ancho `8px`
  - alto `70px`
  - color `#111827`

- Aguja minutera:
  - ancho `6px`
  - alto `100px`
  - color `#374151`

- Aguja segundera:
  - ancho `2px`
  - alto `115px`
  - color `#ef4444`

## JavaScript

- Obtener las agujas con `document.getElementById`.
- Crear una función `updateClock()`.
- Dentro de esa función:
  - obtener la hora actual con `new Date()`
  - calcular:
    - `hours = now.getHours() % 12`
    - `minutes = now.getMinutes()`
    - `seconds = now.getSeconds()`
  - calcular los grados de rotación:
    - `hourDeg = (hours * 30) + (minutes * 0.5)`
    - `minuteDeg = (minutes * 6) + (seconds * 0.1)`
    - `secondDeg = seconds * 6`
  - aplicar la rotación a cada aguja con `style.transform`.

- Llamar a `updateClock()` una vez al cargar.
- Ejecutar `setInterval(updateClock, 1000)`.

## Resultado esperado

El archivo `reloj.html` debe mostrar:

- un reloj analógico centrado,
- fondo oscuro de página,
- esfera clara con sombra,
- marcas principales de hora,
- marcas de minutos,
- números 12, 3, 6 y 9,
- agujas de hora, minuto y segundo funcionando en tiempo real.

