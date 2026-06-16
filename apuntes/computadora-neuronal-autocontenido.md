# Computadora neuronal

Un **perceptrón** es una combinación lineal de las entradas seguida de una función de activación. En el perceptrón clásico, esa activación es un **umbral** (también llamado escalón o pulso): si la suma ponderada llega a cierto valor, la salida es `1`; si no, `0`.

```text
  Perceptrón:  Escalon(x₁·w₁ + x₂·w₂ + b)

  Escalon(s) = 1  si s ≥ 0
               0  si s < 0
```

Eligiendo los valores de los pesos (`w₁`, `w₂` y `b`) y asumiendo que las entradas toman los valores `0` o `1`, podemos construir las funciones básicas. El sesgo `b` funciona como un umbral con signo cambiado: fija cuánta "evidencia" de las entradas hace falta para que la neurona se active.

**AND** — se activa solo si las dos entradas valen `1`. Pesos `w₁ = 1`, `w₂ = 1`, `b = −1.5` (la suma necesita llegar a 2 para superar el umbral):

| x₁ | x₂ | s = x₁ + x₂ − 1.5 | Escalon(s) |
|:--:|:--:|:-----------------:|:----------:|
| 0  | 0  |       −1.5        |     0      |
| 0  | 1  |       −0.5        |     0      |
| 1  | 0  |       −0.5        |     0      |
| 1  | 1  |        0.5        |     1      |

El único `1` está aislado en una esquina, así que una recta puede dejarlo solo de un lado: es **linealmente separable**.

<img src="data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAzMzAgMzQwIiBmb250LWZhbWlseT0ic3lzdGVtLXVpLCBzYW5zLXNlcmlmIj4KPHJlY3Qgd2lkdGg9IjMzMCIgaGVpZ2h0PSIzNDAiIGZpbGw9IndoaXRlIi8+Cjx0ZXh0IHg9IjE2NS4wIiB5PSIzMiIgdGV4dC1hbmNob3I9Im1pZGRsZSIgZm9udC1zaXplPSIyMCIgZm9udC13ZWlnaHQ9IjYwMCIgZmlsbD0iIzFhMWExYSI+QU5EPC90ZXh0Pgo8bGluZSB4MT0iNTgiIHkxPSIyNzAiIHgyPSIzMDguMTU5OTk5OTk5OTk5OTciIHkyPSIyNzAiIHN0cm9rZT0iIzU1NSIgc3Ryb2tlLXdpZHRoPSIxLjUiLz4KPGxpbmUgeDE9IjU4IiB5MT0iMjcwIiB4Mj0iNTgiIHkyPSIxMi43NTk5OTk5OTk5OTk5OTEiIHN0cm9rZT0iIzU1NSIgc3Ryb2tlLXdpZHRoPSIxLjUiLz4KPHBhdGggZD0iTSAzMDguMTU5OTk5OTk5OTk5OTcgMjcwIGwgLTggLTQgbCAwIDggeiIgZmlsbD0iIzU1NSIvPgo8cGF0aCBkPSJNIDU4IDEyLjc1OTk5OTk5OTk5OTk5MSBsIC00IDggbCA4IDAgeiIgZmlsbD0iIzU1NSIvPgo8dGV4dCB4PSIzMTIuMTU5OTk5OTk5OTk5OTciIHk9IjI3NSIgZm9udC1zaXplPSIxNSIgZmlsbD0iIzU1NSI+eOKCgTwvdGV4dD4KPHRleHQgeD0iNjUiIHk9IjE2Ljc1OTk5OTk5OTk5OTk5IiBmb250LXNpemU9IjE1IiBmaWxsPSIjNTU1Ij544oKCPC90ZXh0Pgo8dGV4dCB4PSI1OCIgeT0iMjk0IiBmb250LXNpemU9IjEzIiBmaWxsPSIjODg4IiB0ZXh0LWFuY2hvcj0ibWlkZGxlIj4wPC90ZXh0Pgo8dGV4dCB4PSI0MiIgeT0iMjc1IiBmb250LXNpemU9IjEzIiBmaWxsPSIjODg4IiB0ZXh0LWFuY2hvcj0ibWlkZGxlIj4wPC90ZXh0Pgo8dGV4dCB4PSIyNzAiIHk9IjI5NCIgZm9udC1zaXplPSIxMyIgZmlsbD0iIzg4OCIgdGV4dC1hbmNob3I9Im1pZGRsZSI+MTwvdGV4dD4KPHRleHQgeD0iNDIiIHk9IjU3IiBmb250LXNpemU9IjEzIiBmaWxsPSIjODg4IiB0ZXh0LWFuY2hvcj0ibWlkZGxlIj4xPC90ZXh0Pgo8bGluZSB4MT0iMTE3LjM2MDAwMDAwMDAwMDAxIiB5MT0iMjUuODM5OTk5OTk5OTk5OTc1IiB4Mj0iMjk1LjQ0MDAwMDAwMDAwMDA1IiB5Mj0iMjA4Ljk1OTk5OTk5OTk5OTk4IiBzdHJva2U9IiNjMDM5MmIiIHN0cm9rZS13aWR0aD0iMi4yIiBzdHJva2UtZGFzaGFycmF5PSI2IDQiLz4KPGNpcmNsZSBjeD0iNTgiIGN5PSIyNzAiIHI9IjE0IiBmaWxsPSIjYjBiMGIwIiBzdHJva2U9IiNmZmYiIHN0cm9rZS13aWR0aD0iMi41Ii8+Cjx0ZXh0IHg9IjU4IiB5PSIyNzUiIHRleHQtYW5jaG9yPSJtaWRkbGUiIGZvbnQtc2l6ZT0iMTQiIGZvbnQtd2VpZ2h0PSI3MDAiIGZpbGw9IndoaXRlIj4wPC90ZXh0Pgo8Y2lyY2xlIGN4PSI1OCIgY3k9IjUyIiByPSIxNCIgZmlsbD0iI2IwYjBiMCIgc3Ryb2tlPSIjZmZmIiBzdHJva2Utd2lkdGg9IjIuNSIvPgo8dGV4dCB4PSI1OCIgeT0iNTciIHRleHQtYW5jaG9yPSJtaWRkbGUiIGZvbnQtc2l6ZT0iMTQiIGZvbnQtd2VpZ2h0PSI3MDAiIGZpbGw9IndoaXRlIj4wPC90ZXh0Pgo8Y2lyY2xlIGN4PSIyNzAiIGN5PSIyNzAiIHI9IjE0IiBmaWxsPSIjYjBiMGIwIiBzdHJva2U9IiNmZmYiIHN0cm9rZS13aWR0aD0iMi41Ii8+Cjx0ZXh0IHg9IjI3MCIgeT0iMjc1IiB0ZXh0LWFuY2hvcj0ibWlkZGxlIiBmb250LXNpemU9IjE0IiBmb250LXdlaWdodD0iNzAwIiBmaWxsPSJ3aGl0ZSI+MDwvdGV4dD4KPGNpcmNsZSBjeD0iMjcwIiBjeT0iNTIiIHI9IjE0IiBmaWxsPSIjMmU3ZDMyIiBzdHJva2U9IiNmZmYiIHN0cm9rZS13aWR0aD0iMi41Ii8+Cjx0ZXh0IHg9IjI3MCIgeT0iNTciIHRleHQtYW5jaG9yPSJtaWRkbGUiIGZvbnQtc2l6ZT0iMTQiIGZvbnQtd2VpZ2h0PSI3MDAiIGZpbGw9IndoaXRlIj4xPC90ZXh0Pgo8dGV4dCB4PSIxNjUuMCIgeT0iMzI0IiB0ZXh0LWFuY2hvcj0ibWlkZGxlIiBmb250LXNpemU9IjEyIiBmaWxsPSIjODg4IiBmb250LXdlaWdodD0iNDAwIj5yZWN0YSBzZXBhcmFkb3JhIChwZXJjZXB0csOzbik8L3RleHQ+Cjwvc3ZnPg==" width="25%" alt="Plano AND" />

**OR** — se activa si alguna entrada vale `1`. Pesos `w₁ = 1`, `w₂ = 1`, `b = −0.5` (basta con que la suma llegue a 1):

| x₁ | x₂ | s = x₁ + x₂ − 0.5 | Escalon(s) |
|:--:|:--:|:-----------------:|:----------:|
| 0  | 0  |       −0.5        |     0      |
| 0  | 1  |        0.5        |     1      |
| 1  | 0  |        0.5        |     1      |
| 1  | 1  |        1.5        |     1      |

El único `0` está aislado en una esquina, así que una recta separa ese `0` del resto: es **linealmente separable**.

<img src="data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAzMzAgMzQwIiBmb250LWZhbWlseT0ic3lzdGVtLXVpLCBzYW5zLXNlcmlmIj4KPHJlY3Qgd2lkdGg9IjMzMCIgaGVpZ2h0PSIzNDAiIGZpbGw9IndoaXRlIi8+Cjx0ZXh0IHg9IjE2NS4wIiB5PSIzMiIgdGV4dC1hbmNob3I9Im1pZGRsZSIgZm9udC1zaXplPSIyMCIgZm9udC13ZWlnaHQ9IjYwMCIgZmlsbD0iIzFhMWExYSI+T1I8L3RleHQ+CjxsaW5lIHgxPSI1OCIgeTE9IjI3MCIgeDI9IjMwOC4xNTk5OTk5OTk5OTk5NyIgeTI9IjI3MCIgc3Ryb2tlPSIjNTU1IiBzdHJva2Utd2lkdGg9IjEuNSIvPgo8bGluZSB4MT0iNTgiIHkxPSIyNzAiIHgyPSI1OCIgeTI9IjEyLjc1OTk5OTk5OTk5OTk5MSIgc3Ryb2tlPSIjNTU1IiBzdHJva2Utd2lkdGg9IjEuNSIvPgo8cGF0aCBkPSJNIDMwOC4xNTk5OTk5OTk5OTk5NyAyNzAgbCAtOCAtNCBsIDAgOCB6IiBmaWxsPSIjNTU1Ii8+CjxwYXRoIGQ9Ik0gNTggMTIuNzU5OTk5OTk5OTk5OTkxIGwgLTQgOCBsIDggMCB6IiBmaWxsPSIjNTU1Ii8+Cjx0ZXh0IHg9IjMxMi4xNTk5OTk5OTk5OTk5NyIgeT0iMjc1IiBmb250LXNpemU9IjE1IiBmaWxsPSIjNTU1Ij544oKBPC90ZXh0Pgo8dGV4dCB4PSI2NSIgeT0iMTYuNzU5OTk5OTk5OTk5OTkiIGZvbnQtc2l6ZT0iMTUiIGZpbGw9IiM1NTUiPnjigoI8L3RleHQ+Cjx0ZXh0IHg9IjU4IiB5PSIyOTQiIGZvbnQtc2l6ZT0iMTMiIGZpbGw9IiM4ODgiIHRleHQtYW5jaG9yPSJtaWRkbGUiPjA8L3RleHQ+Cjx0ZXh0IHg9IjQyIiB5PSIyNzUiIGZvbnQtc2l6ZT0iMTMiIGZpbGw9IiM4ODgiIHRleHQtYW5jaG9yPSJtaWRkbGUiPjA8L3RleHQ+Cjx0ZXh0IHg9IjI3MCIgeT0iMjk0IiBmb250LXNpemU9IjEzIiBmaWxsPSIjODg4IiB0ZXh0LWFuY2hvcj0ibWlkZGxlIj4xPC90ZXh0Pgo8dGV4dCB4PSI0MiIgeT0iNTciIGZvbnQtc2l6ZT0iMTMiIGZpbGw9IiM4ODgiIHRleHQtYW5jaG9yPSJtaWRkbGUiPjE8L3RleHQ+CjxsaW5lIHgxPSI0MS4wNCIgeTE9IjEyMS43NTk5OTk5OTk5OTk5OSIgeDI9IjIwMi4xNiIgeTI9IjI4Ny40NCIgc3Ryb2tlPSIjYzAzOTJiIiBzdHJva2Utd2lkdGg9IjIuMiIgc3Ryb2tlLWRhc2hhcnJheT0iNiA0Ii8+CjxjaXJjbGUgY3g9IjU4IiBjeT0iMjcwIiByPSIxNCIgZmlsbD0iI2IwYjBiMCIgc3Ryb2tlPSIjZmZmIiBzdHJva2Utd2lkdGg9IjIuNSIvPgo8dGV4dCB4PSI1OCIgeT0iMjc1IiB0ZXh0LWFuY2hvcj0ibWlkZGxlIiBmb250LXNpemU9IjE0IiBmb250LXdlaWdodD0iNzAwIiBmaWxsPSJ3aGl0ZSI+MDwvdGV4dD4KPGNpcmNsZSBjeD0iNTgiIGN5PSI1MiIgcj0iMTQiIGZpbGw9IiMyZTdkMzIiIHN0cm9rZT0iI2ZmZiIgc3Ryb2tlLXdpZHRoPSIyLjUiLz4KPHRleHQgeD0iNTgiIHk9IjU3IiB0ZXh0LWFuY2hvcj0ibWlkZGxlIiBmb250LXNpemU9IjE0IiBmb250LXdlaWdodD0iNzAwIiBmaWxsPSJ3aGl0ZSI+MTwvdGV4dD4KPGNpcmNsZSBjeD0iMjcwIiBjeT0iMjcwIiByPSIxNCIgZmlsbD0iIzJlN2QzMiIgc3Ryb2tlPSIjZmZmIiBzdHJva2Utd2lkdGg9IjIuNSIvPgo8dGV4dCB4PSIyNzAiIHk9IjI3NSIgdGV4dC1hbmNob3I9Im1pZGRsZSIgZm9udC1zaXplPSIxNCIgZm9udC13ZWlnaHQ9IjcwMCIgZmlsbD0id2hpdGUiPjE8L3RleHQ+CjxjaXJjbGUgY3g9IjI3MCIgY3k9IjUyIiByPSIxNCIgZmlsbD0iIzJlN2QzMiIgc3Ryb2tlPSIjZmZmIiBzdHJva2Utd2lkdGg9IjIuNSIvPgo8dGV4dCB4PSIyNzAiIHk9IjU3IiB0ZXh0LWFuY2hvcj0ibWlkZGxlIiBmb250LXNpemU9IjE0IiBmb250LXdlaWdodD0iNzAwIiBmaWxsPSJ3aGl0ZSI+MTwvdGV4dD4KPHRleHQgeD0iMTY1LjAiIHk9IjMyNCIgdGV4dC1hbmNob3I9Im1pZGRsZSIgZm9udC1zaXplPSIxMiIgZmlsbD0iIzg4OCIgZm9udC13ZWlnaHQ9IjQwMCI+cmVjdGEgc2VwYXJhZG9yYSAocGVyY2VwdHLDs24pPC90ZXh0Pgo8L3N2Zz4=" width="25%" alt="Plano OR" />

**NAND** — la negación de AND: se apaga solo si las dos entradas valen `1`. Pesos `w₁ = −1`, `w₂ = −1`, `b = 1.5` (los pesos negativos invierten la lógica del AND):

| x₁ | x₂ | s = −x₁ − x₂ + 1.5 | Escalon(s) |
|:--:|:--:|:------------------:|:----------:|
| 0  | 0  |        1.5         |     1      |
| 0  | 1  |        0.5         |     1      |
| 1  | 0  |        0.5         |     1      |
| 1  | 1  |       −0.5         |     0      |

El único `0` está en la esquina `(1,1)`, así que una recta lo separa de los demás: es **linealmente separable**.

<img src="data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAzMzAgMzQwIiBmb250LWZhbWlseT0ic3lzdGVtLXVpLCBzYW5zLXNlcmlmIj4KPHJlY3Qgd2lkdGg9IjMzMCIgaGVpZ2h0PSIzNDAiIGZpbGw9IndoaXRlIi8+Cjx0ZXh0IHg9IjE2NS4wIiB5PSIzMiIgdGV4dC1hbmNob3I9Im1pZGRsZSIgZm9udC1zaXplPSIyMCIgZm9udC13ZWlnaHQ9IjYwMCIgZmlsbD0iIzFhMWExYSI+TkFORDwvdGV4dD4KPGxpbmUgeDE9IjU4IiB5MT0iMjcwIiB4Mj0iMzA4LjE1OTk5OTk5OTk5OTk3IiB5Mj0iMjcwIiBzdHJva2U9IiM1NTUiIHN0cm9rZS13aWR0aD0iMS41Ii8+CjxsaW5lIHgxPSI1OCIgeTE9IjI3MCIgeDI9IjU4IiB5Mj0iMTIuNzU5OTk5OTk5OTk5OTkxIiBzdHJva2U9IiM1NTUiIHN0cm9rZS13aWR0aD0iMS41Ii8+CjxwYXRoIGQ9Ik0gMzA4LjE1OTk5OTk5OTk5OTk3IDI3MCBsIC04IC00IGwgMCA4IHoiIGZpbGw9IiM1NTUiLz4KPHBhdGggZD0iTSA1OCAxMi43NTk5OTk5OTk5OTk5OTEgbCAtNCA4IGwgOCAwIHoiIGZpbGw9IiM1NTUiLz4KPHRleHQgeD0iMzEyLjE1OTk5OTk5OTk5OTk3IiB5PSIyNzUiIGZvbnQtc2l6ZT0iMTUiIGZpbGw9IiM1NTUiPnjigoE8L3RleHQ+Cjx0ZXh0IHg9IjY1IiB5PSIxNi43NTk5OTk5OTk5OTk5OSIgZm9udC1zaXplPSIxNSIgZmlsbD0iIzU1NSI+eOKCgjwvdGV4dD4KPHRleHQgeD0iNTgiIHk9IjI5NCIgZm9udC1zaXplPSIxMyIgZmlsbD0iIzg4OCIgdGV4dC1hbmNob3I9Im1pZGRsZSI+MDwvdGV4dD4KPHRleHQgeD0iNDIiIHk9IjI3NSIgZm9udC1zaXplPSIxMyIgZmlsbD0iIzg4OCIgdGV4dC1hbmNob3I9Im1pZGRsZSI+MDwvdGV4dD4KPHRleHQgeD0iMjcwIiB5PSIyOTQiIGZvbnQtc2l6ZT0iMTMiIGZpbGw9IiM4ODgiIHRleHQtYW5jaG9yPSJtaWRkbGUiPjE8L3RleHQ+Cjx0ZXh0IHg9IjQyIiB5PSI1NyIgZm9udC1zaXplPSIxMyIgZmlsbD0iIzg4OCIgdGV4dC1hbmNob3I9Im1pZGRsZSI+MTwvdGV4dD4KPGxpbmUgeDE9IjExNy4zNjAwMDAwMDAwMDAwMSIgeTE9IjI1LjgzOTk5OTk5OTk5OTk3NSIgeDI9IjI5NS40NDAwMDAwMDAwMDAwNSIgeTI9IjIwOC45NTk5OTk5OTk5OTk5OCIgc3Ryb2tlPSIjYzAzOTJiIiBzdHJva2Utd2lkdGg9IjIuMiIgc3Ryb2tlLWRhc2hhcnJheT0iNiA0Ii8+CjxjaXJjbGUgY3g9IjU4IiBjeT0iMjcwIiByPSIxNCIgZmlsbD0iIzJlN2QzMiIgc3Ryb2tlPSIjZmZmIiBzdHJva2Utd2lkdGg9IjIuNSIvPgo8dGV4dCB4PSI1OCIgeT0iMjc1IiB0ZXh0LWFuY2hvcj0ibWlkZGxlIiBmb250LXNpemU9IjE0IiBmb250LXdlaWdodD0iNzAwIiBmaWxsPSJ3aGl0ZSI+MTwvdGV4dD4KPGNpcmNsZSBjeD0iNTgiIGN5PSI1MiIgcj0iMTQiIGZpbGw9IiMyZTdkMzIiIHN0cm9rZT0iI2ZmZiIgc3Ryb2tlLXdpZHRoPSIyLjUiLz4KPHRleHQgeD0iNTgiIHk9IjU3IiB0ZXh0LWFuY2hvcj0ibWlkZGxlIiBmb250LXNpemU9IjE0IiBmb250LXdlaWdodD0iNzAwIiBmaWxsPSJ3aGl0ZSI+MTwvdGV4dD4KPGNpcmNsZSBjeD0iMjcwIiBjeT0iMjcwIiByPSIxNCIgZmlsbD0iIzJlN2QzMiIgc3Ryb2tlPSIjZmZmIiBzdHJva2Utd2lkdGg9IjIuNSIvPgo8dGV4dCB4PSIyNzAiIHk9IjI3NSIgdGV4dC1hbmNob3I9Im1pZGRsZSIgZm9udC1zaXplPSIxNCIgZm9udC13ZWlnaHQ9IjcwMCIgZmlsbD0id2hpdGUiPjE8L3RleHQ+CjxjaXJjbGUgY3g9IjI3MCIgY3k9IjUyIiByPSIxNCIgZmlsbD0iI2IwYjBiMCIgc3Ryb2tlPSIjZmZmIiBzdHJva2Utd2lkdGg9IjIuNSIvPgo8dGV4dCB4PSIyNzAiIHk9IjU3IiB0ZXh0LWFuY2hvcj0ibWlkZGxlIiBmb250LXNpemU9IjE0IiBmb250LXdlaWdodD0iNzAwIiBmaWxsPSJ3aGl0ZSI+MDwvdGV4dD4KPHRleHQgeD0iMTY1LjAiIHk9IjMyNCIgdGV4dC1hbmNob3I9Im1pZGRsZSIgZm9udC1zaXplPSIxMiIgZmlsbD0iIzg4OCIgZm9udC13ZWlnaHQ9IjQwMCI+cmVjdGEgc2VwYXJhZG9yYSAocGVyY2VwdHLDs24pPC90ZXh0Pgo8L3N2Zz4=" width="25%" alt="Plano NAND" />

Lo importante: **la pieza es siempre la misma** —ponderar, sumar, aplicar el umbral—; lo único que cambia entre una compuerta y otra son los pesos y el sesgo. Esos números *son* la regla.

Y como ya construimos NAND, valen las mismas conclusiones que con las compuertas: un perceptrón con los pesos adecuados reproduce NAND, y repitiendo NAND se arma cualquier función booleana. La neurona no es menos poderosa que la compuerta; es otra forma de implementar lo mismo.

## El límite del perceptrón: el XOR

En los tres casos anteriores siempre pudimos trazar una recta que dejara los `1` de un lado. Veamos ahora una función donde eso no es posible.

Un perceptrón solo **no** puede calcular un XOR (el "o exclusivo": `1` cuando las entradas son distintas, `0` cuando son iguales).

La razón es geométrica. Un perceptrón traza una sola recta y responde `1` de un lado y `0` del otro; solo puede separar problemas **linealmente separables**. Para AND, OR y NAND alcanza, porque en cada uno hay una sola esquina distinta del resto. Pero el XOR pide encender `(0,1)` y `(1,0)` —esquinas opuestas— y apagar `(0,0)` y `(1,1)` —las otras dos opuestas—. No existe una sola recta que separe esos dos pares: están cruzados.

<img src="data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAzMzAgMzQwIiBmb250LWZhbWlseT0ic3lzdGVtLXVpLCBzYW5zLXNlcmlmIj4KPHJlY3Qgd2lkdGg9IjMzMCIgaGVpZ2h0PSIzNDAiIGZpbGw9IndoaXRlIi8+Cjx0ZXh0IHg9IjE2NS4wIiB5PSIzMiIgdGV4dC1hbmNob3I9Im1pZGRsZSIgZm9udC1zaXplPSIyMCIgZm9udC13ZWlnaHQ9IjYwMCIgZmlsbD0iIzFhMWExYSI+WE9SPC90ZXh0Pgo8bGluZSB4MT0iNTgiIHkxPSIyNzAiIHgyPSIzMDguMTU5OTk5OTk5OTk5OTciIHkyPSIyNzAiIHN0cm9rZT0iIzU1NSIgc3Ryb2tlLXdpZHRoPSIxLjUiLz4KPGxpbmUgeDE9IjU4IiB5MT0iMjcwIiB4Mj0iNTgiIHkyPSIxMi43NTk5OTk5OTk5OTk5OTEiIHN0cm9rZT0iIzU1NSIgc3Ryb2tlLXdpZHRoPSIxLjUiLz4KPHBhdGggZD0iTSAzMDguMTU5OTk5OTk5OTk5OTcgMjcwIGwgLTggLTQgbCAwIDggeiIgZmlsbD0iIzU1NSIvPgo8cGF0aCBkPSJNIDU4IDEyLjc1OTk5OTk5OTk5OTk5MSBsIC00IDggbCA4IDAgeiIgZmlsbD0iIzU1NSIvPgo8dGV4dCB4PSIzMTIuMTU5OTk5OTk5OTk5OTciIHk9IjI3NSIgZm9udC1zaXplPSIxNSIgZmlsbD0iIzU1NSI+eOKCgTwvdGV4dD4KPHRleHQgeD0iNjUiIHk9IjE2Ljc1OTk5OTk5OTk5OTk5IiBmb250LXNpemU9IjE1IiBmaWxsPSIjNTU1Ij544oKCPC90ZXh0Pgo8dGV4dCB4PSI1OCIgeT0iMjk0IiBmb250LXNpemU9IjEzIiBmaWxsPSIjODg4IiB0ZXh0LWFuY2hvcj0ibWlkZGxlIj4wPC90ZXh0Pgo8dGV4dCB4PSI0MiIgeT0iMjc1IiBmb250LXNpemU9IjEzIiBmaWxsPSIjODg4IiB0ZXh0LWFuY2hvcj0ibWlkZGxlIj4wPC90ZXh0Pgo8dGV4dCB4PSIyNzAiIHk9IjI5NCIgZm9udC1zaXplPSIxMyIgZmlsbD0iIzg4OCIgdGV4dC1hbmNob3I9Im1pZGRsZSI+MTwvdGV4dD4KPHRleHQgeD0iNDIiIHk9IjU3IiBmb250LXNpemU9IjEzIiBmaWxsPSIjODg4IiB0ZXh0LWFuY2hvcj0ibWlkZGxlIj4xPC90ZXh0Pgo8Y2lyY2xlIGN4PSI1OCIgY3k9IjI3MCIgcj0iMTQiIGZpbGw9IiNiMGIwYjAiIHN0cm9rZT0iI2ZmZiIgc3Ryb2tlLXdpZHRoPSIyLjUiLz4KPHRleHQgeD0iNTgiIHk9IjI3NSIgdGV4dC1hbmNob3I9Im1pZGRsZSIgZm9udC1zaXplPSIxNCIgZm9udC13ZWlnaHQ9IjcwMCIgZmlsbD0id2hpdGUiPjA8L3RleHQ+CjxjaXJjbGUgY3g9IjU4IiBjeT0iNTIiIHI9IjE0IiBmaWxsPSIjMmU3ZDMyIiBzdHJva2U9IiNmZmYiIHN0cm9rZS13aWR0aD0iMi41Ii8+Cjx0ZXh0IHg9IjU4IiB5PSI1NyIgdGV4dC1hbmNob3I9Im1pZGRsZSIgZm9udC1zaXplPSIxNCIgZm9udC13ZWlnaHQ9IjcwMCIgZmlsbD0id2hpdGUiPjE8L3RleHQ+CjxjaXJjbGUgY3g9IjI3MCIgY3k9IjI3MCIgcj0iMTQiIGZpbGw9IiMyZTdkMzIiIHN0cm9rZT0iI2ZmZiIgc3Ryb2tlLXdpZHRoPSIyLjUiLz4KPHRleHQgeD0iMjcwIiB5PSIyNzUiIHRleHQtYW5jaG9yPSJtaWRkbGUiIGZvbnQtc2l6ZT0iMTQiIGZvbnQtd2VpZ2h0PSI3MDAiIGZpbGw9IndoaXRlIj4xPC90ZXh0Pgo8Y2lyY2xlIGN4PSIyNzAiIGN5PSI1MiIgcj0iMTQiIGZpbGw9IiNiMGIwYjAiIHN0cm9rZT0iI2ZmZiIgc3Ryb2tlLXdpZHRoPSIyLjUiLz4KPHRleHQgeD0iMjcwIiB5PSI1NyIgdGV4dC1hbmNob3I9Im1pZGRsZSIgZm9udC1zaXplPSIxNCIgZm9udC13ZWlnaHQ9IjcwMCIgZmlsbD0id2hpdGUiPjA8L3RleHQ+Cjx0ZXh0IHg9IjE2NS4wIiB5PSIzMjQiIHRleHQtYW5jaG9yPSJtaWRkbGUiIGZvbnQtc2l6ZT0iMTIiIGZpbGw9IiNjMDM5MmIiIGZvbnQtd2VpZ2h0PSI2MDAiPk5pbmd1bmEgcmVjdGEgc2VwYXJhIGxvcyAxIGRlIGxvcyAwPC90ZXh0Pgo8L3N2Zz4=" width="25%" alt="Plano XOR" />

Este hallazgo, señalado por Minsky y Papert en 1969, frenó la investigación en redes neuronales durante años (el llamado "invierno de la IA").

## La solución: combinar neuronas en capas

El XOR sí se resuelve, pero hace falta **más de un nivel de neuronas**. La idea es descomponerlo en funciones que el perceptrón sí sabe hacer:

```text
  XOR(x₁, x₂) = OR(x₁, x₂)  AND  NAND(x₁, x₂)
```

En palabras: encendé si hay al menos un `1` (OR), pero no si están los dos (NAND). La intersección de ambas condiciones es exactamente "exactamente uno".

Esto se arma con dos capas. Una **capa oculta** calcula dos resultados intermedios, y una **capa de salida** los combina:

```text
  Capa oculta:   h₁ = OR(x₁, x₂)      → pesos  1,  1, sesgo −0.5
                 h₂ = NAND(x₁, x₂)    → pesos −1, −1, sesgo  1.5
  Capa salida:   y  = AND(h₁, h₂)     → pesos  1,  1, sesgo −1.5
```

Verifiquémoslo:

| x₁ | x₂ | h₁ = OR | h₂ = NAND | y = AND(h₁, h₂) | XOR esperado |
|:--:|:--:|:-------:|:---------:|:---------------:|:------------:|
| 0  | 0  |    0    |     1     |        0        |      0       |
| 0  | 1  |    1    |     1     |        1        |      1       |
| 1  | 0  |    1    |     1     |        1        |      1       |
| 1  | 1  |    1    |     0     |        0        |      0       |

Funciona. La capa de salida no mira las entradas originales: mira lo que **la capa oculta ya procesó**. Ese es el salto de capacidad. Una neurona traza una recta; una segunda capa que combina varias rectas puede recortar regiones que ninguna recta sola delimita —como las dos esquinas en diagonal del XOR.

Y acá reaparece el principio de siempre: con una sola pieza simple, repetida y organizada en capas, se construye cualquier función. Lo que el perceptrón solo no alcanzaba, la red en capas lo resuelve. Esta es, en miniatura, la razón por la que las redes neuronales necesitan ser **profundas**.

