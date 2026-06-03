# Instrucciones para generar `2048-nano.html`

Crear una **única página web autocontenida** llamada `2048-nano.html` que implemente una variante del juego **2048**

## Requisitos generales

- Debe ser un solo archivo HTML.
- Debe incluir todo adentro del mismo archivo:
  - HTML
  - CSS
  - JavaScript
- No usar frameworks ni dependencias externas.
- No usar imágenes externas ni archivos de audio externos.
- Debe funcionar abriendo el archivo directamente en el navegador.
- El título visible del juego debe ser **2048**.
- El archivo debe llamarse exactamente **`2048-nano.html`**.

## Juego

Implementar un tablero de **4x4**.

Reglas:
- Al comenzar, aparecen 2 fichas aleatorias.
- Cada nueva ficha debe ser 2 o 4.
- En cada movimiento, todas las fichas deben desplazarse en la dirección elegida.
- Cuando dos fichas del mismo valor colisionan, deben unirse en una sola con valor doble.
- Cada ficha solo puede participar en una fusión por movimiento.
- Después de un movimiento válido, debe aparecer una nueva ficha aleatoria.
- Si no hay movimientos posibles, debe mostrarse fin de juego.
- Si aparece una ficha con valor **2048**, debe mostrarse mensaje de victoria, pero permitir seguir jugando.

## Controles

Soportar:
- teclado con flechas
- teclado con WASD
- swipe táctil en móvil

## UI

La interfaz debe incluir:
- título grande: `2048`
- puntaje actual
- mejor puntaje
- botón `Nuevo juego`
- overlay de victoria y derrota
- texto de ayuda breve

Guardar el mejor puntaje usando `localStorage`.

## Estética

La estética debe ser:
- divertida
- colorida
- simpática
- moderna
- tipo candy / glass / casual game

Sugerencias visuales:
- fondo con degradados suaves y colores cálidos
- tarjetas con esquinas redondeadas
- efecto glassmorphism suave
- colores distintos según el valor de la ficha
- buen contraste para fichas altas
- interfaz agradable también en mobile

## Animaciones

Agregar animaciones fluidas.

Debe incluir como mínimo:
- animación de aparición de ficha nueva
- animación de merge
- animación de desplazamiento real de una celda a otra
- pequeña animación de “bump” cuando el movimiento no produce cambios

La animación de desplazamiento real significa:
- las fichas no deben simplemente redibujarse en destino
- deben verse moviéndose visualmente desde su posición original a la nueva
- durante la animación debe bloquearse la entrada para evitar estados inconsistentes

## Sonido

Agregar sonidos agradables generados programáticamente con **Web Audio API**.

No usar archivos externos.

Incluir al menos:
- sonido suave de movimiento
- sonido más expresivo para merge
- sonido de victoria
- sonido de derrota

Los sonidos deben ser cortos y agradables, no estridentes.

## Implementación técnica recomendada

### Estructura visual

- Crear la grilla de fondo de 4x4 con celdas vacías.
- Renderizar las fichas reales en una capa absoluta por encima de la grilla.
- Las fichas deben posicionarse mediante coordenadas calculadas en píxeles para permitir animación de desplazamiento real.
- Medir tamaño y separación de celdas con `getBoundingClientRect()`.
- Recalcular posiciones al redimensionar ventana.

### Modelo de datos

Mantener un estado de fichas con identidad propia, por ejemplo:
- `id`
- `value`
- `r`
- `c`
- referencia al elemento DOM

Además mantener:
- `score`
- `best`
- `won`
- flag `animating`

### Lógica de movimiento

Implementar planificación del movimiento antes de modificar definitivamente el DOM:
- detectar todas las fichas de una fila o columna según la dirección
- procesarlas en el orden correcto
- calcular destino final de cada ficha
- calcular merges válidos
- impedir doble merge en un mismo turno
- acumular score
- detectar si hubo movimiento real

Luego:
1. mover visualmente las fichas con transición CSS
2. esperar fin de animación
3. resolver merges definitivos
4. eliminar la ficha absorbida
5. actualizar valores visuales
6. crear nueva ficha aleatoria
7. actualizar score y best
8. verificar victoria o derrota

## Bugs a evitar explícitamente

Evitar estos errores:
- fichas renderizadas en posiciones incorrectas cuando hay menos de 4 filas ocupadas
- contenedor de fichas sin filas explícitas o con layout que dependa de auto-placement
- fichas vacías ocupando lugar visual
- merges desalineados después de mover arriba/abajo
- rotaciones lógicas mal resueltas
- aparición de fichas nuevas antes de terminar la animación del movimiento
- aceptar input adicional mientras una animación está en curso

## Detalles de comportamiento

- Si un movimiento no cambia nada, no debe aparecer ficha nueva.
- En ese caso, solo debe sonar/verse el pequeño bump visual.
- El overlay de victoria no debe impedir seguir jugando; solo informar.
- El botón `Nuevo juego` debe reiniciar todo correctamente.
- La primera carga debe mostrar 2 fichas con animación de aparición.

## Calidad esperada

El código debe ser:
- claro
- mantenible
- bien organizado
- sin dependencias externas
- listo para usar directamente

## Entregable

Generar exactamente un archivo:
- `2048-nano.html`

No generar archivos adicionales.
