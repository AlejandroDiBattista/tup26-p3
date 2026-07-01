---

- Legajo  : 63219
- Nombre  : Sergio Fabricio Lazarte
- Comisión: C9

---

# Cambios solicitados para TP5

Aplicar los siguientes cambios sobre la agenda de contactos. En cada sección, completar **Cambios realizados** con una breve explicación de lo modificado y los archivos principales modificados.


## 1. Ajustar la información del panel maestro

En el panel maestro, cada contacto debe mostrar solamente:

> **Nombre completo**  
> Teléfono | Email

El **nombre completo** se forma con `Nombre` y `Apellido`.

No deben mostrarse otros datos del contacto en el panel maestro.

### Cambios realizados

> *** Poner aca el codigo final con los cambios realizados *** 

---

## 2. Cambiar los nombres de las acciones

Actualizar los textos visibles de las acciones para que usen estos nombres:

| Acción actual                       | Texto requerido        |
|-------------------------------------|------------------------|
| Agregar contacto                    | `Alta de contacto`     |
| Editar contacto                     | `Modificar contacto`   |
| Borrar contacto                     | `Baja de contacto`     |
| Aceptar cambios en la edición       | `Guardar`              |
| Descartar cambios en la edición     | `Cancelar`             |

Los nuevos textos deben aparecer en los botones, enlaces o títulos donde corresponda.

### Cambios realizados

> *** Poner aca el codigo final con los cambios realizados *** 

---

## 3. Agregar el campo Legajo

Agregar el campo `Legajo` a la aplicación.

La base de datos `contactos.db` ya incluye, en la tabla `Contactos`, una columna `Legajo` de tipo numérico. No hace falta crear la columna.

El campo `Legajo` debe incorporarse en todos los lugares necesarios para que el sistema lo utilice correctamente:

- Modelo de datos.
- Búsqueda o filtrado de contactos.
- Vista de solo lectura o detalle del contacto.
- Formulario de alta y modificación.
- Guardado y actualización en la base de datos.

### Cambios realizados

> *** Poner aca el codigo final con los cambios realizados *** 