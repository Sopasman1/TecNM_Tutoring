# Listado de entregables

`GET /api/compliance/{docenteClave}/{periodoId}` devuelve los entregables registrados y vigentes de un docente. La clave reservada `me` identifica al docente de la sesión.

- `User` requiere el claim `docente_clave` y solo puede consultar su propia clave. `Admin` puede consultar una clave explícita. Sin sesión devuelve 401 y sin permiso devuelve 403.
- Acepta los mismos filtros `page`, `limit`, `month`, `formatKey` y `status` del resumen. Parámetros inválidos devuelven 400.
- Orden por mes, tipo de reporte e ID. Excluye registros eliminados. Sin coincidencias devuelve una página vacía.
- Devuelve `{ data, page, limit, total }` con `id`, `docenteClave`, `periodoId`, `formatKey`, `reportingMonth`, `status`, `delayDays` y `updatedAt` por fila. El cliente añade los metadatos visuales con su catálogo. Captura y detalle se consultan por separado.
