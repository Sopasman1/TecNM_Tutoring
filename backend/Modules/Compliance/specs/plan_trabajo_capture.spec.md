# Captura del Plan de Trabajo

- `GET /api/compliance/tasks/{id}` devuelve el Plan de Trabajo activo para Admin o para el User propietario según `docente_clave`. Incluye los campos del listado, el título, los indicadores de formato, `prefilledData.docente_nombre` y `formPayload`.
- `PUT /api/compliance/tasks/{id}/form` permite al User propietario guardar `{ formPayload, submit }` como JSON de hasta 3 MiB. El formulario tiene una sola pantalla. No admite pestañas.
- El borrador admite campos incompletos y conserva `Pending`. Para enviar se requieren periodo, grupo, carrera, objetivo general, 16 sesiones con título, objetivo y actividades, confirmación de actividades normativas y confirmación de firma. Validación fallida devuelve 422.
- El primer envío pasa a `OnTime` con cero días de atraso mientras no exista una ventana configurada para la captura estructurada. Un envío idéntico se puede repetir. Cambios después de enviar o escrituras concurrentes incompatibles devuelven 409.
- Se excluyen entregables eliminados. Un ID ajeno o inexistente devuelve 404. User sin clave recibe 403. Admin tiene acceso de lectura.
- `form_payload` almacena el formulario en JSONB y por defecto contiene un objeto vacío. PostgreSQL conserva la fecha de actualización mediante el trigger existente. La API registra correo del JWT e IP de la petición al modificar.
