# Especificación: Cámara Inteligente con OpenAI Images

## Objetivo

Crear una app web autocontenida, pensada para celular, que permita abrir la cámara, capturar una foto, escribir un prompt de transformación y procesar la imagen usando la API de imágenes de OpenAI.

La app debe funcionar como un único archivo HTML, sin build, sin servidor backend obligatorio y sin dependencias externas.

## Plataforma

- Aplicación web mobile-first.
- Archivo único: `camara-inteligente.html`.
- Debe poder ejecutarse desde `http://localhost` o `http://127.0.0.1`.
- Debe estar optimizada para uso en celular.
- No requiere instalación.

## Flujo Principal

1. Al abrir la app, debe intentar activar la cámara automáticamente.
2. La pantalla principal debe mostrar el visor de cámara.
3. El botón **Capturar** debe estar superpuesto sobre el visor.
4. Al capturar, debe congelarse la foto tomada.
5. El usuario debe poder escribir o modificar un prompt.
6. Al tocar **Procesar**, la app debe enviar la foto y el prompt a OpenAI Images API.
7. Debe mostrar el resultado procesado junto a la foto original.
8. Debe permitir descargar la imagen generada.
9. Debe mantener un historial breve de resultados recientes.

## Configuración

La configuración no debe ocupar la pantalla principal.

Debe existir una ventana/modal de configuración con:

- API Key de OpenAI.
- Modelo.
- Calidad.
- Tamaño.
- Formato de salida.

Valores iniciales sugeridos:

- Modelo: `gpt-image-2`
- Calidad: `medium`
- Tamaño: `auto`
- Formato: `jpeg`

La configuración debe guardarse en `localStorage`.

Si la app se abre por primera vez y no hay API key guardada, debe abrir automáticamente la ventana de configuración y enfocar el campo de API key.

## Cámara

La cámara debe implementarse con:

- `navigator.mediaDevices.getUserMedia()`
- Preferir cámara trasera con `facingMode: { ideal: "environment" }`
- Usar `<video autoplay muted playsinline>`
- Capturar el frame usando `<canvas>`
- Convertir la captura a `File` o `Blob` JPEG antes de enviarla

Si el navegador no permite cámara directa, debe mostrar un fallback claro y permitir elegir una imagen desde archivo.

## Procesamiento con OpenAI

La app debe llamar a:

`POST https://api.openai.com/v1/images/edits`

Debe enviar un `FormData` con:

- `model`
- `image`
- `prompt`
- `quality`
- `size`
- `output_format`

Debe usar la API key desde la configuración:

`Authorization: Bearer <API_KEY>`

Debe interpretar la respuesta base64 y mostrar la imagen resultante.

## Pantalla Principal

Debe incluir:

- Encabezado con nombre de la app.
- Botón de configuración.
- Visor de cámara/foto original.
- Botón **Capturar** superpuesto al visor.
- Vista de resultado procesado.
- Campo de prompt.
- Prompts rápidos opcionales.
- Estado/progreso.
- Botón **Procesar**.
- Botón **Guardar**.
- Historial breve.

## Diseño

- Mobile-first.
- Interfaz clara para usar con una mano.
- Botones grandes y táctiles.
- El visor debe ser protagonista.
- La configuración debe estar separada para no distraer.
- Debe funcionar bien en pantallas angostas.
- Evitar landing page o contenido explicativo largo.

## Manejo de Estados

La app debe contemplar:

- Sin API key.
- Cámara cargando.
- Cámara activa.
- Cámara bloqueada o no disponible.
- Foto capturada.
- Procesamiento en curso.
- Error de API.
- Resultado disponible.
- Descarga disponible.

## Seguridad

- La API key se guarda solo en `localStorage`.
- Mostrar una nota indicando que para producción conviene usar backend propio para no exponer credenciales.
- No enviar datos hasta que el usuario toque **Procesar**.

## Criterios de Aceptación

- Al abrir la app, aparece el visor de cámara o un fallback claro.
- Si no hay API key, se abre automáticamente configuración.
- El botón de captura está superpuesto sobre el visor.
- Se puede capturar una foto real desde la cámara.
- Se puede escribir un prompt.
- Se puede procesar la foto con OpenAI Images API.
- Se muestra antes/después.
- Se puede descargar el resultado.
- La configuración persiste al recargar.
- La app funciona como un solo archivo HTML.

## Como usar?

Implementá esta especificación en el repo actual. Creá una app autocontenida en un solo HTML y verificá que cargue localmente.




