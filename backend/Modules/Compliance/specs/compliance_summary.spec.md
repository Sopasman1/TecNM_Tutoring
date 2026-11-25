# Resumen de cumplimiento

`GET /api/compliance/summary` requiere sesión con rol `Admin`.
Sin sesión, devuelve 401. Otro rol recibe 403.

- `periodoId` obligatorio, entero positivo. `page` y `limit` por defecto 1 y 20. Límite máximo 100 por página.
- `month` opcional entre 1 y 12. `status` acepta Pending, OnTime y Late.
- `formatKey` acepta PlanTrabajo, DiagnosticoInicial, Anexo14, Anexo15, Anexo19AMensual, SeguimientoMensual y SeguimientoFinal. Los últimos dos corresponden a Asistencia y Anexo19Final en BD.
- `search` busca por nombre completo o clave, sin distinguir mayúsculas. Parámetros inválidos devuelven 400.
- Devuelve `{ data, page, limit, total }`. Cada fila contiene `docenteClave`, `docenteName`, `totalDeliverables`, `pending`, `onTime`, `late` y `totalDelayDays`.
- Lista docentes activos de BD Principal ordenados por clave. Cuenta entregables que coincidan con los filtros y excluye registros eliminados. Un docente sin coincidencias conserva su fila con ceros.
