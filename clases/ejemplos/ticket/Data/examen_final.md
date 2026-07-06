# Como rendir el Examen final práctico

## Sistema de agenda de contactos con historial de comunicaciones

Materia: Programación III
Carrera: Tecnicatura Universitaria en Programación
Facultad: UTN Facultad Regional Tucumán

⸻

1. Objetivo del examen

El alumno deberá presentar y defender una aplicación web funcional que permita administrar una agenda de contactos y registrar el historial de comunicaciones mantenidas con cada contacto.

El sistema deberá permitir cargar, consultar, modificar y eliminar contactos. Además, para cada contacto, deberá permitir registrar comunicaciones asociadas, tales como llamadas, correos electrónicos, mensajes o reuniones.

Durante el examen final, el alumno deberá ejecutar la aplicación, mostrar su funcionamiento y explicar las decisiones técnicas tomadas en el desarrollo.

⸻

2. Descripción general del sistema

La aplicación consiste en una agenda de contactos con historial de comunicaciones.

Cada contacto puede tener muchas comunicaciones asociadas. Cada comunicación pertenece a un único contacto.

La interfaz debe estar organizada con un patrón maestro–detalle:

* A la izquierda se muestra la lista de contactos.
* A la derecha se muestra el detalle del contacto seleccionado.
* Desde el detalle se puede consultar, agregar, editar o eliminar comunicaciones.

La aplicación debe funcionar en una única pantalla principal, sin necesidad de navegar entre páginas diferentes para las operaciones principales.

⸻

3. Modelo de datos requerido

El sistema debe tener, como mínimo, dos entidades principales:

⸻

3.1. Contacto

Cada contacto debe tener los siguientes datos:

* Identificador único, generado por la base de datos.
* Nombre, obligatorio.
* Apellido, obligatorio.
* Empresa, opcional.
* Teléfono, opcional.
* Correo electrónico, opcional.

⸻

3.2. Comunicación

Cada comunicación debe tener los siguientes datos:

* Identificador único, generado por la base de datos.
* Fecha y hora, obligatoria.
* Tipo de comunicación, obligatorio.
* Descripción, obligatoria.
* Referencia al contacto al que pertenece.

Los tipos de comunicación permitidos son:

* Llamada.
* Correo.
* Mensaje.
* Reunión.

⸻

4. Relación entre entidades

El sistema debe implementar una relación de uno a muchos entre contactos y comunicaciones:

* Un contacto puede tener muchas comunicaciones.
* Una comunicación pertenece a un solo contacto.

Esta relación debe estar definida correctamente en el modelo de datos y reflejada en la base de datos.

⸻

5. Funcionamiento de la pantalla principal

La aplicación debe tener una pantalla principal dividida en dos paneles.

⸻

5.1. Panel izquierdo: lista de contactos

El panel izquierdo debe mostrar la lista de contactos cargados en el sistema.

Debe cumplir con los siguientes requisitos:

1. Mostrar todos los contactos ordenados por apellido y nombre.
2. Mostrar en cada fila:
    * Nombre completo.
    * Empresa, si existe.
    * Cantidad de comunicaciones asociadas.
3. Permitir buscar contactos por nombre, apellido o empresa.
4. Filtrar la lista mientras el usuario escribe en el cuadro de búsqueda.
5. Permitir seleccionar un contacto.
6. Resaltar visualmente el contacto seleccionado.
7. Tener una opción para crear un nuevo contacto.

Al abrir la aplicación, si existen contactos cargados, debe quedar seleccionado automáticamente el primer contacto de la lista.

Si no existen contactos cargados, el sistema debe mostrar un mensaje indicando que todavía no hay contactos y ofrecer la posibilidad de crear el primero.

⸻

5.2. Panel derecho: detalle del contacto seleccionado

El panel derecho debe mostrar la información del contacto seleccionado y su historial de comunicaciones.

En modo detalle, debe mostrar:

1. Los datos principales del contacto:
    * Nombre.
    * Apellido.
    * Empresa.
    * Teléfono.
    * Correo electrónico.
2. Botón para editar el contacto.
3. Botón para eliminar el contacto.
4. Una sección para registrar rápidamente una nueva comunicación.
5. El historial de comunicaciones del contacto.

⸻

6. Alta y edición de contactos

El sistema debe permitir crear y modificar contactos.

El formulario de contacto debe permitir cargar o modificar:

* Nombre.
* Apellido.
* Empresa.
* Teléfono.
* Correo electrónico.

El sistema debe validar que:

* El nombre sea obligatorio.
* El apellido sea obligatorio.
* El correo electrónico tenga un formato válido, si fue ingresado.

Al guardar un contacto:

1. Se deben validar los datos.
2. Si hay errores, deben mostrarse junto a los campos correspondientes.
3. Si los datos son válidos, se debe guardar la información.
4. Luego de guardar, el sistema debe volver al modo detalle.
5. Si se creó un nuevo contacto, este debe quedar seleccionado.

El sistema también debe permitir cancelar la operación de alta o edición sin guardar cambios.

Mientras se está editando un contacto, la lista de contactos debe quedar deshabilitada para evitar cambios de selección que puedan hacer perder los datos cargados.

⸻

7. Eliminación de contactos

El sistema debe permitir eliminar un contacto.

Antes de eliminarlo, debe pedir confirmación al usuario.

La confirmación debe informar que al eliminar el contacto también se eliminarán sus comunicaciones asociadas.

Al eliminar un contacto:

1. Se debe borrar el contacto.
2. Se deben borrar sus comunicaciones asociadas.
3. Se debe actualizar la lista de contactos.
4. Debe quedar seleccionado otro contacto, si existe.
5. Si no quedan contactos, el panel derecho debe mostrar el estado vacío correspondiente.

⸻

8. Registro rápido de comunicaciones

El panel derecho debe incluir una sección para registrar rápidamente una nueva comunicación del contacto seleccionado.

La barra de registro debe tener los siguientes campos:

* Tipo de comunicación.
* Descripción.
* Fecha y hora.

El campo fecha y hora debe proponer inicialmente el momento actual, pero debe permitir ser modificado por el usuario.

Al registrar una comunicación:

1. Se deben validar los datos.
2. La descripción debe ser obligatoria.
3. El tipo debe ser obligatorio.
4. La fecha y hora debe ser obligatoria.
5. La fecha y hora no puede ser futura.
6. Si los datos son válidos, la comunicación debe guardarse asociada al contacto seleccionado.
7. La nueva comunicación debe aparecer al principio del historial.
8. El formulario de registro rápido debe quedar limpio para permitir cargar otra comunicación.

⸻

9. Historial de comunicaciones

El sistema debe mostrar el historial de comunicaciones del contacto seleccionado.

El historial debe cumplir con los siguientes requisitos:

1. Mostrar las comunicaciones ordenadas desde la más reciente hasta la más antigua.
2. Mostrar en cada comunicación:
    * Tipo.
    * Fecha y hora.
    * Descripción.
3. Permitir editar una comunicación.
4. Permitir eliminar una comunicación.
5. Mostrar un mensaje claro si el contacto todavía no tiene comunicaciones registradas.

⸻

10. Edición de comunicaciones

El sistema debe permitir editar una comunicación existente.

La edición debe realizarse en la misma fila del historial, reemplazando temporalmente la visualización de la comunicación por un formulario.

El formulario de edición debe permitir modificar:

* Tipo.
* Descripción.
* Fecha y hora.

Al guardar:

1. Se deben validar los datos.
2. Si hay errores, deben mostrarse.
3. Si los datos son válidos, se debe actualizar la comunicación.
4. La fila debe volver al modo de lectura.

Al cancelar:

1. Se deben descartar los cambios.
2. La fila debe volver al modo de lectura.

Solo una comunicación puede estar en edición a la vez.

Mientras una comunicación está siendo editada, el registro rápido de comunicaciones debe quedar deshabilitado.

⸻

11. Eliminación de comunicaciones

El sistema debe permitir eliminar una comunicación individual.

Antes de eliminarla, debe pedir confirmación al usuario.

Al eliminar una comunicación:

1. Debe borrarse de la base de datos.
2. Debe desaparecer del historial.
3. Debe actualizarse la cantidad de comunicaciones del contacto en la lista del panel izquierdo.

⸻

12. Validaciones obligatorias

El sistema debe implementar, como mínimo, las siguientes validaciones:

Contacto

* Nombre obligatorio.
* Apellido obligatorio.
* Correo electrónico con formato válido, si se ingresa.

Comunicación

* Tipo obligatorio.
* Descripción obligatoria.
* Fecha y hora obligatoria.
* La fecha y hora no puede ser futura.

Los mensajes de error deben mostrarse junto al campo correspondiente o en una zona claramente visible del formulario.

⸻

13. Requisitos técnicos

La aplicación debe cumplir con los siguientes requisitos técnicos:

1. Debe estar desarrollada con Blazor.
2. Debe usar Entity Framework Core para el acceso a datos.
3. Debe usar SQLite como base de datos.
4. La relación de uno a muchos entre contactos y comunicaciones debe estar correctamente configurada.
5. La base de datos debe crearse automáticamente la primera vez que se ejecuta la aplicación.
6. La aplicación debe poder ejecutarse desde cero usando:

dotnet run

7. No deben requerirse pasos manuales previos para crear la base de datos.
8. El código debe estar organizado de forma clara.
9. Deben respetarse las convenciones vistas durante la cursada.

⸻

14. Qué debe defender el alumno durante el examen

Durante la defensa, el alumno deberá poder explicar y demostrar:

1. Cómo está organizado el proyecto.
2. Cuáles son las entidades principales del sistema.
3. Cómo se define la relación entre contacto y comunicación.
4. Cómo se configura Entity Framework Core.
5. Cómo se crea y utiliza la base de datos SQLite.
6. Cómo se cargan, modifican y eliminan contactos.
7. Cómo se cargan, modifican y eliminan comunicaciones.
8. Cómo se actualiza el panel derecho al seleccionar un contacto.
9. Cómo funciona la búsqueda de contactos.
10. Cómo se implementan las validaciones.
11. Cómo se manejan las confirmaciones antes de eliminar.
12. Cómo se evita perder datos cuando hay una edición en curso.
13. Qué componentes de Blazor se utilizaron y cómo se comunican entre sí.
14. Qué partes del código corresponden a interfaz, lógica de negocio y acceso a datos.

El alumno debe poder modificar o explicar fragmentos de código durante la defensa si el docente lo solicita.

⸻

15. Criterios de evaluación

Se evaluará que el sistema cumpla correctamente con los siguientes aspectos:

1. El modelo de datos está bien definido.
2. La relación de uno a muchos funciona correctamente.
3. La base de datos se crea y se utiliza correctamente.
4. La lista de contactos se muestra, filtra y selecciona correctamente.
5. El panel derecho muestra correctamente el detalle del contacto seleccionado.
6. El alta, edición y eliminación de contactos funciona correctamente.
7. El registro rápido de comunicaciones funciona correctamente.
8. El historial de comunicaciones se muestra ordenado correctamente.
9. La edición inline de comunicaciones funciona correctamente.
10. La eliminación de contactos y comunicaciones solicita confirmación.
11. Las validaciones obligatorias están implementadas.
12. La interfaz es clara y usable.
13. La aplicación se ejecuta con dotnet run sin pasos manuales.
14. El alumno puede explicar el funcionamiento técnico del sistema.
15. El código está ordenado y sigue las convenciones de la cátedra.

⸻

16. Funcionalidades que no se solicitan

No se pide implementar:

* Usuarios.
* Login.
* Permisos.
* Roles.
* Autenticación.
* Autorización.
* Envío real de correos electrónicos.
* Integración real con WhatsApp.
* Integración con calendarios externos.
* Diseño visual complejo.
* Funcionalidades no descriptas en este enunciado.

El objetivo del examen es evaluar el desarrollo de una aplicación CRUD con relación entre entidades, interfaz maestro–detalle, validaciones, persistencia en base de datos y defensa técnica del código desarrollado.

⸻

17. Condición importante para la defensa

El sistema presentado debe ser comprendido por el alumno.

Durante el examen final, no alcanza con que la aplicación funcione. El alumno debe poder explicar cómo fue construida, justificar las decisiones tomadas y responder preguntas sobre el código.

Si el alumno no puede explicar partes sustanciales del sistema presentado, eso afectará la evaluación, aunque la aplicación se ejecute correctamente.

18. Pantalla de ejemplo
![Examen Final](examen-final.png)