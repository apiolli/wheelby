# Criterios de aceptación: Notificaciones y Reportes (Práctica 5)

Programación III · ITLA · 2026-C-3 · Wheelby

Este documento se escribió **antes** de delegar la implementación. Es el contrato con el que se acepta o se rechaza el trabajo del agente: un cambio que no cumple un criterio de aquí no se integra. El agente no puede modificar este archivo.

Cada criterio tiene un identificador (`CA-NOT-xx.y` o `CA-REP-xx.y`), cita el requisito que cubre y se escribe como **Dado / Cuando / Entonces**. La línea **Cómo se comprueba** dice cómo verificarlo sin preguntarle al autor: por HTTP en Scalar (`http://localhost:8080/scalar/v1`), con una consulta SQL o con una prueba automática cuyo nombre empieza por el requisito.

---

## 0. Convenciones de este documento

**Usuarios de las comprobaciones**

| Nombre | Quién es |
|---|---|
| **Admin A** | El Administrador fijo del `.env` (`Admin__Email`). |
| **Admin B** | Un Estándar registrado y activado al que Admin A le dio el rol `Administrator` con `PATCH /admin/users/{id}/role`. |
| **Admin C** | Igual que Admin B, pero deshabilitado con `POST /admin/users/{id}/disable`. |
| **Ana** | Una Estándar activa. Hace de solicitante y de anfitriona. |
| **Beto** | Un Estándar activo. Hace de inquilino. |

**Definiciones**

- **Administrador activo:** rol `Administrator`, cuenta activada y no deshabilitada.
- **Notificación:** fila en `notifications."Notifications"`, visible en la bandeja interna del destinatario.
- **Correo encolado:** fila en `notifications."QueuedEmails"`.
- **Correos de cuenta:** activación, reenvío de activación, recuperación de contraseña y restablecimiento forzado. **No son notificaciones.** Entran directo a la cola, no aparecen en la bandeja y no respetan la preferencia de correo, para que desactivar el correo nunca impida activar una cuenta ni recuperar la contraseña. Sí cumplen todas las reglas de la cola (RF-NOT-08 a 13).
- **Rango de fechas:** `from` y `to` en formato `yyyy-MM-dd`, en UTC. `from` es inclusivo y `to` exclusivo: `[from, to)`.

**Consultas SQL de apoyo** (en `docker compose exec postgres psql -U wheelby -d wheelby`)

```sql
-- Notificaciones
SELECT "RecipientId", "Type", "Title", "ReadAt", "EmailId", "CreatedAt"
FROM notifications."Notifications" ORDER BY "CreatedAt" DESC;

-- Cola de correos
SELECT "Id", "To", "Subject", "Status", "Attempts", "LastAttemptAt", "SentAt", "FailedAt", "LastError"
FROM notifications."QueuedEmails" ORDER BY "CreatedAt" DESC;

-- Ids de los usuarios
SELECT "Id", "Email", "Role" FROM access_control."Users";
```

---

## 1. Notificaciones (pieza 4)

### RF-NOT-01: crear una solicitud de permiso notifica a los Administradores

**CA-NOT-01.1: llega a todos los Administradores activos**
- **Dado** que existen Admin A y Admin B activos, y Ana es Estándar activa,
- **cuando** Ana hace `POST /permissions/requests` con `{ "permissionCode": "core.demo-action" }` y recibe 201,
- **entonces** Admin A y Admin B tienen, cada uno, **exactamente una** notificación nueva sin leer de tipo `permission.request-created`, que menciona el código `core.demo-action` y el nombre de Ana.
- **Cómo se comprueba:** `GET /notifications?read=false` con el token de Admin A y con el de Admin B: cada uno ve la notificación. Prueba `RF_NOT_01_crear_solicitud_notifica_a_cada_administrador_activo`.

**CA-NOT-01.2: nadie más la recibe**
- **Dado** el mismo escenario, con Beto (Estándar) y Admin C (Administrador deshabilitado),
- **cuando** Ana crea la solicitud,
- **entonces** ni Ana, ni Beto, ni Admin C tienen una notificación de tipo `permission.request-created` para esa solicitud. La cantidad de notificaciones de ese tipo es igual a la de Administradores activos.
- **Cómo se comprueba:** `GET /notifications` con el token de Ana y con el de Beto: no aparece. SQL: `SELECT count(*) FROM notifications."Notifications" WHERE "Type" = 'permission.request-created' AND "CreatedAt" > <momento de la prueba>;` da 2. Prueba `RF_NOT_01_crear_solicitud_no_notifica_a_estandar_ni_a_administrador_deshabilitado`.

**CA-NOT-01.3: un Administrador que crea su propia solicitud no se notifica a sí mismo**
- **Dado** que Admin B crea una solicitud de permiso,
- **cuando** se registra la solicitud,
- **entonces** la reciben los demás Administradores activos (Admin A) y Admin B no. Él no puede resolver su propia solicitud (RF-GP-07).
- **Cómo se comprueba:** prueba `RF_NOT_01_administrador_solicitante_no_recibe_su_propia_notificacion`.

### RF-NOT-02: resolver una solicitud notifica al solicitante

**CA-NOT-02.1: aprobación**
- **Dado** que Ana tiene una solicitud `Pending`,
- **cuando** Admin A hace `POST /admin/permissions/requests/{id}/approve` y recibe 204,
- **entonces** Ana tiene exactamente una notificación nueva sin leer de tipo `permission.request-approved`, que dice que fue **aprobada** y nombra el permiso. Ningún otro usuario recibe esa notificación.
- **Cómo se comprueba:** `GET /notifications?read=false` con el token de Ana. Prueba `RF_NOT_02_aprobar_notifica_solo_al_solicitante`.

**CA-NOT-02.2: rechazo con motivo**
- **Dado** que Ana tiene otra solicitud `Pending`,
- **cuando** Admin A la rechaza con `{ "reason": "No corresponde al perfil del usuario" }` y recibe 204,
- **entonces** Ana tiene exactamente una notificación de tipo `permission.request-rejected`, que dice que fue **rechazada** e incluye el texto exacto del motivo. El correo encolado para Ana también incluye el motivo.
- **Cómo se comprueba:** `GET /notifications` con el token de Ana (el `message` contiene el motivo). SQL: el `Body` del último correo a Ana contiene el motivo. Prueba `RF_NOT_02_rechazar_notifica_al_solicitante_con_el_motivo`.

**CA-NOT-02.3: una resolución rechazada no notifica**
- **Dado** que la solicitud ya está `Applied` o `Rejected`,
- **cuando** Admin A intenta aprobarla o rechazarla de nuevo y recibe 409,
- **entonces** no se crea ninguna notificación nueva ni se encola ningún correo.
- **Cómo se comprueba:** prueba `RF_NOT_02_transicion_rechazada_no_crea_notificacion`.

### RF-NOT-03: un evento del negocio dispara una notificación

**CA-NOT-03.1: nueva solicitud de reserva al anfitrión** (el evento documentado en el README, sección Notificaciones)
- **Dado** que Ana publicó un vehículo activo y Beto es otro Estándar,
- **cuando** Beto hace `POST /reservations` sobre ese vehículo y recibe 201,
- **entonces** Ana (la anfitriona) tiene exactamente una notificación nueva sin leer de tipo `reservation.requested`, con la marca, el modelo, las fechas y el nombre de Beto, y se encola un correo para Ana. Beto no recibe notificación por su propia solicitud.
- **Cómo se comprueba:** siguiendo los pasos del README, sección Notificaciones. Prueba `RF_NOT_03_solicitar_reserva_notifica_al_anfitrion`.

**CA-NOT-03.2: las demás transiciones de la reserva también pasan por el motor**
- **Dado** una reserva entre Ana (anfitriona) y Beto (inquilino),
- **cuando** ocurre cada transición de la tabla de abajo,
- **entonces** el destinatario indicado recibe una notificación del tipo indicado (y su correo, salvo que tenga el correo desactivado), y quien actuó no la recibe. Ya no se encola ningún correo de reserva por fuera del motor de notificaciones.

| Transición | Quién actúa | Destinatario | Tipo |
|---|---|---|---|
| `Pending → Confirmed` | Anfitrión | Inquilino | `reservation.confirmed` |
| `Pending → Cancelled` (rechazo con `/reject`) | Anfitrión | Inquilino, con el motivo | `reservation.rejected` |
| `Pending → Cancelled` o `Confirmed → Cancelled` | Inquilino | Anfitrión | `reservation.cancelled` |
| `Pending → Cancelled` o `Confirmed → Cancelled` | Anfitrión | Inquilino, con el motivo | `reservation.cancelled` |
| `Pending → Cancelled` (expiración) | Sistema (Worker) | Anfitrión **e** inquilino | `reservation.expired` |
| `Confirmed → InProgress` | Anfitrión | Inquilino | `reservation.started` |
| `InProgress → Completed` | Anfitrión | Inquilino | `reservation.completed` |

- **Cómo se comprueba:** una prueba por fila, `RF_NOT_03_<transicion>_notifica_a_<destinatario>`. Por HTTP, siguiendo el frente 6 del README: tras confirmar, Beto ve `reservation.confirmed` en `GET /notifications`.

### RF-NOT-04: consultar las notificaciones propias, distinguiendo leídas de no leídas

**CA-NOT-04.1: cada usuario ve solo las suyas**
- **Dado** que Ana y Admin A tienen notificaciones,
- **cuando** Ana hace `GET /notifications`,
- **entonces** recibe 200 y todos los elementos tienen `recipientId` igual al `id` de Ana (el de `GET /auth/me`). Ninguna notificación de Admin A ni de Beto aparece, ni siquiera cambiando la paginación o los filtros.
- **Cómo se comprueba:** comparar con `GET /notifications` de Admin A: los dos conjuntos de `id` no tienen ningún elemento en común. Prueba `RF_NOT_04_un_usuario_no_ve_notificaciones_de_otro`.

**CA-NOT-04.2: leídas y no leídas se distinguen**
- **Dado** que Ana tiene 3 notificaciones y marcó 1 como leída,
- **cuando** consulta `GET /notifications`, `GET /notifications?read=false` y `GET /notifications?read=true`,
- **entonces** obtiene 3, 2 y 1 elementos. Cada elemento trae `isRead` (`true`/`false`) y `readAt` (`null` si no está leída), y la respuesta trae `unreadCount = 2`. Ordenadas de la más nueva a la más vieja.
- **Cómo se comprueba:** por HTTP. Prueba `RF_NOT_04_filtra_leidas_y_no_leidas_y_cuenta_no_leidas`.

**CA-NOT-04.3: sin sesión no hay notificaciones** (RD-06)
- **Dado** una petición sin cabecera `Authorization`, o con un token de una sesión cerrada con `POST /auth/logout`,
- **cuando** se llama a cualquier ruta de `/notifications`,
- **entonces** la respuesta es 401.
- **Cómo se comprueba:** por HTTP. Prueba de rutas `RF_NOT_04_rutas_de_notificaciones_exigen_sesion`.

### RF-NOT-05: marcar una notificación como leída

**CA-NOT-05.1: marcar la propia**
- **Dado** una notificación sin leer de Ana,
- **cuando** Ana hace `POST /notifications/{id}/read`,
- **entonces** recibe 204. La notificación queda con `isRead = true` y `readAt` con la fecha, y `unreadCount` baja en 1. Repetir la llamada responde 204 y no cambia `readAt`.
- **Cómo se comprueba:** por HTTP. Pruebas `RF_NOT_05_marcar_propia_como_leida` y `RF_NOT_05_marcar_dos_veces_conserva_la_primera_fecha`.

**CA-NOT-05.2: marcar la de otro se rechaza**
- **Dado** una notificación sin leer de Admin A,
- **cuando** Ana hace `POST /notifications/{id de Admin A}/read`,
- **entonces** recibe **404** con `code` `notification.not-found` (no se revela que exista), y la notificación de Admin A **sigue sin leer**. Un `id` que no existe da la misma respuesta.
- **Cómo se comprueba:** por HTTP; luego `GET /notifications?read=false` con el token de Admin A todavía la muestra. Prueba `RF_NOT_05_marcar_la_de_otro_se_rechaza_y_no_cambia`.

### RF-NOT-06: toda notificación se entrega por dos canales

**CA-NOT-06.1: bandeja interna y correo**
- **Dado** que el destinatario tiene el correo activado (el valor por defecto),
- **cuando** se dispara cualquier notificación de las secciones RF-NOT-01 a 03,
- **entonces** aparece en su bandeja (`GET /notifications`) **y** existe en `notifications."QueuedEmails"` un correo a su dirección con el mismo título como asunto. La notificación guarda el `EmailId` de ese correo, y la notificación y el correo se guardan en la misma transacción.
- **Cómo se comprueba:** SQL: `SELECT n."Title", q."Subject", q."To" FROM notifications."Notifications" n JOIN notifications."QueuedEmails" q ON q."Id" = n."EmailId" ORDER BY n."CreatedAt" DESC;`. Con SMTP real, el correo llega. Prueba `RF_NOT_06_notificacion_crea_entrada_en_bandeja_y_correo_encolado`.

### RF-NOT-07: el destinatario puede desactivar el canal de correo

**CA-NOT-07.1: consultar y cambiar la preferencia**
- **Dado** que Ana tiene sesión,
- **cuando** hace `GET /notifications/preferences`,
- **entonces** recibe 200 `{ "emailEnabled": true }`, el valor por defecto. Con `PUT /notifications/preferences` y `{ "emailEnabled": false }` recibe 204, y la siguiente consulta devuelve `false`. Cada usuario cambia solo su propia preferencia: la ruta no recibe ningún `userId`.
- **Cómo se comprueba:** por HTTP. Prueba `RF_NOT_07_preferencia_por_defecto_es_correo_activado`.

**CA-NOT-07.2: con el correo desactivado queda la interna y no se encola correo**
- **Dado** que Ana desactivó el correo,
- **cuando** Beto solicita una reserva sobre el vehículo de Ana (o Admin A resuelve una solicitud de Ana),
- **entonces** Ana tiene la notificación en su bandeja con `EmailId = null`, y **no** se agrega ninguna fila nueva a `notifications."QueuedEmails"` con su dirección.
- **Cómo se comprueba:** SQL `SELECT count(*) FROM notifications."QueuedEmails" WHERE "To" = '<correo de Ana>';` da el mismo número antes y después; la notificación aparece en `GET /notifications`. Prueba `RF_NOT_07_correo_desactivado_crea_notificacion_sin_encolar_correo`.

**CA-NOT-07.3: la preferencia no afecta a los correos de cuenta**
- **Dado** que Ana desactivó el correo,
- **cuando** hace `POST /auth/forgot-password` con su correo,
- **entonces** el correo con el código **sí** se encola.
- **Cómo se comprueba:** SQL sobre `QueuedEmails`. Prueba `RF_NOT_07_correos_de_cuenta_ignoran_la_preferencia`.

---

## 2. Cola de correos

### RF-NOT-08: los correos se encolan; la operación no espera al servidor de correo

**CA-NOT-08.1**
- **Dado** que el servidor SMTP no responde (los valores de ejemplo de `.env.example`, o `Smtp__Port=9999` y `docker compose up -d worker`),
- **cuando** se registra un usuario, se crea o se resuelve una solicitud de permiso, o se solicita una reserva,
- **entonces** la operación responde con su código de éxito (201 o 204) en el tiempo normal. El correo queda `Pending` con `Attempts = 0` hasta que lo toma el procesador. Ni la API ni sus handlers llaman al servidor SMTP.
- **Cómo se comprueba:** por HTTP y SQL. Prueba `RF_NOT_08_operacion_termina_aunque_el_envio_falle` (con un `IEmailSender` que lanza excepción: la operación no la ve).

### RF-NOT-09: un proceso independiente envía los pendientes

**CA-NOT-09.1: el Worker continuo**
- **Dado** un correo `Pending`,
- **cuando** el servicio `worker` hace su siguiente vuelta (cada `Worker__IntervalSeconds`),
- **entonces** el correo se envía, o se registra el intento fallido. Ninguna petición HTTP vuelve a ejecutarse para que esto ocurra.
- **Cómo se comprueba:** `docker compose logs -f worker` muestra `Pending emails processed. Sent: N. Failed: M.`.

**CA-NOT-09.2: ejecución manual de una pasada**
- **Dado** que el proyecto está levantado,
- **cuando** se ejecuta `docker compose run --rm worker --once`,
- **entonces** el procesador hace **una sola pasada** sobre los pendientes, escribe en el log `Pending emails processed. Sent: N. Failed: M.` y termina con código de salida 0. No ejecuta la expiración de reservas ni se queda corriendo.
- **Cómo se comprueba:** ejecutar el comando; termina solo. `echo $?` (bash) o `$LASTEXITCODE` (PowerShell) da 0.

### RF-NOT-10: reintentos limitados y estado fallido

**CA-NOT-10.1: el máximo está declarado y es configurable**
- **Dado** el README, sección Notificaciones,
- **entonces** declara que el máximo es **5 intentos**, configurable con `Notifications__MaxEmailAttempts` (mínimo 1), y `.env.example` incluye esa variable con el valor 5.

**CA-NOT-10.2: cada fallo suma un intento y guarda el error**
- **Dado** un correo `Pending` y un SMTP que no responde,
- **cuando** el procesador intenta enviarlo y falla,
- **entonces** el correo sigue `Pending`, `Attempts` aumenta en 1, `LastAttemptAt` tiene la fecha del intento y `LastError` tiene un mensaje no vacío de 500 caracteres como máximo, **sin credenciales** (ni `Smtp__Password` ni `Smtp__User`).
- **Cómo se comprueba:** SQL tras una pasada (`docker compose run --rm worker --once`). Prueba `RF_NOT_10_fallo_incrementa_intentos_y_guarda_ultimo_error`.

**CA-NOT-10.3: al llegar al máximo queda fallido y no se reintenta más**
- **Dado** un correo que falla en todos los intentos,
- **cuando** se ejecutan 5 pasadas (o se espera a 5 vueltas del Worker),
- **entonces** el correo queda `Failed` con `Attempts = 5`, `FailedAt` con fecha y su último error. En una sexta pasada **no** se intenta enviar de nuevo: `Attempts` sigue en 5. El correo no se borra.
- **Cómo se comprueba:** SQL tras 6 ejecuciones de `docker compose run --rm worker --once`. Pruebas `RF_NOT_10_al_llegar_al_maximo_queda_fallido` y `RF_NOT_10_correo_fallido_no_se_reintenta`.

**CA-NOT-10.4: un correo que falla no detiene a los demás**
- **Dado** dos pendientes, uno a una dirección que falla y otro que se envía bien,
- **cuando** corre una pasada,
- **entonces** el segundo queda `Sent` aunque el primero falle.
- **Cómo se comprueba:** prueba `RF_NOT_10_un_fallo_no_impide_enviar_los_demas`.

### RF-NOT-11: cada correo registra su estado y el Administrador lo consulta

**CA-NOT-11.1: el Administrador consulta la cola**
- **Dado** correos en los tres estados,
- **cuando** Admin A hace `GET /admin/notifications/emails?page=1&pageSize=20`, opcionalmente con `&status=Pending|Sent|Failed`,
- **entonces** recibe 200 con una lista paginada (`items`, `page`, `pageSize`, `totalItems`). Cada elemento trae `id`, `to`, `subject`, `status`, `attempts`, `createdAt`, `lastAttemptAt`, `sentAt`, `failedAt` y `lastError`, ordenados del más nuevo al más viejo. El filtro `status` devuelve solo ese estado. `GET /admin/notifications/emails/{id}` devuelve un correo (404 si no existe).
- **Cómo se comprueba:** por HTTP. Prueba `RF_NOT_11_administrador_consulta_la_cola_con_estado_intentos_fechas_y_error`.

**CA-NOT-11.2: el cuerpo no se expone**
- **Dado** la respuesta de la consulta de la cola,
- **entonces** **no** incluye el `body` del correo, porque los correos de cuenta llevan enlaces y códigos de un solo uso de otros usuarios.
- **Cómo se comprueba:** inspeccionar el JSON. Prueba `RF_NOT_11_la_consulta_no_expone_el_cuerpo`.

**CA-NOT-11.3: un Estándar recibe rechazo**
- **Dado** el token de Ana (Estándar),
- **cuando** llama a `GET /admin/notifications/emails` o a `GET /admin/notifications/emails/{id}`,
- **entonces** recibe **403**; sin sesión, **401**.
- **Cómo se comprueba:** por HTTP. Prueba de rutas `RF_NOT_11_rutas_de_la_cola_exigen_administrador`.

### RF-NOT-12: un correo enviado no se vuelve a enviar

**CA-NOT-12.1: dos pasadas seguidas**
- **Dado** un correo `Pending` y un SMTP que funciona,
- **cuando** se ejecuta `docker compose run --rm worker --once` dos veces seguidas,
- **entonces** la primera envía el correo (`Sent`, `SentAt` con fecha, `Attempts = 1`) y la segunda no lo envía de nuevo: `Sent: 0` en el log, y `SentAt` y `Attempts` no cambian.
- **Cómo se comprueba:** SQL y log. Prueba `RF_NOT_12_dos_pasadas_no_duplican_el_envio`.

**CA-NOT-12.2: dos procesadores a la vez no toman el mismo correo**
- **Dado** que el Worker continuo está corriendo,
- **cuando** se ejecuta a la vez `docker compose run --rm worker --once`,
- **entonces** cada correo pendiente lo toma un solo procesador: la selección de pendientes bloquea las filas tomadas (`FOR UPDATE SKIP LOCKED` o un mecanismo equivalente, documentado en `docs/notificaciones/README.md`).
- **Cómo se comprueba:** revisión del repositorio de la cola y de su documentación. Con SMTP real, cada correo llega una sola vez.

### RF-NOT-13: credenciales SMTP solo por entorno (RD-10)

**CA-NOT-13.1**
- **Dado** el repositorio en la etiqueta `practica-5`,
- **entonces** `git ls-files | grep -E '(^|/)\.env$'` no devuelve nada. `.env.example` solo trae valores de ejemplo (`smtp.example.com`, `change_me`). `git log -p --all` no contiene la contraseña SMTP real ni la clave real de ExchangeRate-API. `SmtpOptions` se llena solo desde configuración (variables `Smtp__*`), sin valores por defecto reales en el código.
- **Cómo se comprueba:** con esos comandos, y `git log -p --all -S "<valor real>"` vacío para cada secreto que el autor conoce.

---

## 3. Reportes con agregación (pieza 5)

Rutas comunes: las dos exigen sesión (401 sin ella) y aceptan los dos roles. Los parámetros `from` y `to` son obligatorios.

**CA-REP-00.1: validación del rango** (RD-07)
- **Dado** cualquiera de los dos reportes,
- **cuando** falta `from` o `to`, alguno no tiene el formato `yyyy-MM-dd`, o `from >= to`,
- **entonces** la respuesta es 400 `problem+json` con el error por campo.
- **Cómo se comprueba:** por HTTP. Pruebas `RF_REP_05_rango_invalido_responde_400` en los dos módulos.

### RF-REP-01: reporte del Core, solicitudes de permiso por estado y por mes

**CA-REP-01.1: devuelve grupos, no registros**
- **Dado** que existen solicitudes de permiso con distintos estados creadas en distintos meses,
- **cuando** Admin A hace `GET /reports/permission-requests?from=2026-01-01&to=2027-01-01`,
- **entonces** recibe 200 con `{ from, to, scope, total, rows }`. Cada fila tiene `period` (mes UTC de `CreatedAt`, formato `yyyy-MM`), `status` (`Pending`, `Approved`, `Applied` o `Rejected`) y `count`. Solo aparecen combinaciones con `count > 0`, ordenadas por `period` y luego por `status`. `total` es la suma de los `count`. Ninguna fila trae `id`, `requesterId` ni `permissionCode` de una solicitud.
- **Cómo se comprueba:** por HTTP, comparando con `SELECT to_char("CreatedAt",'YYYY-MM'), "Status", count(*) FROM permissions."PermissionRequests" WHERE "CreatedAt" >= '2026-01-01' AND "CreatedAt" < '2027-01-01' GROUP BY 1,2 ORDER BY 1,2;`. Prueba `RF_REP_01_agrupa_solicitudes_por_mes_y_estado`.

### RF-REP-02: reporte del negocio, ingresos por vehículo

**Pregunta que responde:** *¿cuánto generó cada vehículo y cuántos días estuvo alquilado en un período?* Le sirve al anfitrión para saber qué vehículo le rinde más, y al Administrador para ver el movimiento de toda la plataforma.

**CA-REP-02.1: ingresos agrupados por vehículo**
- **Dado** reservas de distintos vehículos en distintos estados,
- **cuando** se hace `GET /reports/vehicle-revenue?from=...&to=...`,
- **entonces** recibe 200 con `{ from, to, scope, currency: "DOP", totals, rows }`. Cada fila es **un vehículo** con `vehicleId`, `brand`, `model`, `plate`, `reservations` (cantidad), `days` (suma de `Days`), `totalAmount` (suma de `TotalAmount`) y `averageAmount` (`totalAmount / reservations`, redondeado a 2 decimales). Solo cuentan las reservas en estado `Confirmed`, `InProgress` o `Completed` cuya `StartDate` está en `[from, to)`. `Pending` y `Cancelled` no generan ingreso. Las filas se ordenan por `totalAmount` de mayor a menor. `totals` trae la suma de `reservations`, `days` y `totalAmount` de todas las filas. Los montos son los congelados en la reserva, no el precio actual del vehículo.
- **Cómo se comprueba:** por HTTP, comparando con `SELECT "VehicleId", count(*), sum("Days"), sum("TotalAmount") FROM wheelby."Reservations" WHERE "Status" IN ('Confirmed','InProgress','Completed') AND "StartDate" >= '<from>' AND "StartDate" < '<to>' GROUP BY 1;`. Pruebas `RF_REP_02_suma_ingresos_y_dias_por_vehiculo` y `RF_REP_02_excluye_pendientes_y_canceladas`.

### RF-REP-03: agregación real

**CA-REP-03.1: menos filas que registros**
- **Dado** al menos 3 reservas válidas de un mismo vehículo y 3 solicitudes de permiso del mismo mes y estado,
- **cuando** se consultan los reportes con un rango que las incluye,
- **entonces** el reporte de ingresos devuelve **una** fila para ese vehículo con `reservations = 3`, y el de permisos **una** fila para ese mes y estado con `count = 3`.
- **Cómo se comprueba:** pruebas `RF_REP_03_varios_registros_producen_una_sola_fila` en los dos módulos.

**CA-REP-03.2: la agregación la hace el motor de datos**
- **Dado** el código de los dos reportes,
- **entonces** la agrupación (`GroupBy` con `Count` y `Sum`) se ejecuta en la consulta de EF Core traducida a SQL. El read store no carga la lista completa de registros a memoria para resumirla, y el frontend no resume nada.
- **Cómo se comprueba:** revisión del read store de cada reporte, y del SQL en el log de EF con el proyecto levantado.

### RF-REP-04: los reportes respetan el rol

**CA-REP-04.1: el Estándar solo ve lo suyo en el reporte del Core**
- **Dado** que Ana y Beto tienen solicitudes de permiso,
- **cuando** Ana consulta `GET /reports/permission-requests`,
- **entonces** `scope = "own"`, y los conteos incluyen solo las solicitudes con `RequesterId = Ana`. Con el token de Admin A, `scope = "all"` e incluye las de todos. No existe ningún parámetro para pedir los datos de otro usuario.
- **Cómo se comprueba:** comparar los `total` de Ana, de Beto y de Admin A: el de Admin A es mayor o igual que la suma de los otros dos. Prueba `RF_REP_04_estandar_solo_cuenta_sus_solicitudes`.

**CA-REP-04.2: el Estándar solo ve sus vehículos en el reporte del negocio**
- **Dado** que Ana y otra anfitriona tienen vehículos con reservas válidas,
- **cuando** Ana consulta `GET /reports/vehicle-revenue`,
- **entonces** `scope = "own"`, y solo aparecen vehículos cuyo dueño es Ana. Beto, que solo es inquilino, recibe 200 con `rows` vacío y totales en 0. Admin A recibe `scope = "all"` con todos los vehículos.
- **Cómo se comprueba:** por HTTP con los tres tokens. Pruebas `RF_REP_04_anfitrion_solo_ve_sus_vehiculos` y `RF_REP_04_administrador_ve_todos_los_vehiculos`.

### RF-REP-05: filtro por rango de fechas

**CA-REP-05.1: dos rangos distintos, dos resultados distintos**
- **Dado** los mismos datos, con registros en dos meses distintos (por ejemplo, reservas con `StartDate` en 2030-01 y en 2030-02, y solicitudes creadas en dos meses distintos),
- **cuando** se consulta cada reporte con `from=2030-01-01&to=2030-02-01` y luego con `from=2030-02-01&to=2030-03-01` (o los meses que correspondan a las solicitudes),
- **entonces** los resultados son distintos: cada uno incluye solo los registros de su rango, y un registro con fecha exactamente igual a `to` no se incluye.
- **Cómo se comprueba:** por HTTP, siguiendo el README, sección Reportes. Pruebas `RF_REP_05_dos_rangos_distintos_dan_resultados_distintos` y `RF_REP_05_to_es_exclusivo` en los dos módulos.

---

## 4. No se rompe nada de lo anterior

**CA-REG-01: suite completa en verde**
- **Cuando** se ejecuta `docker build --target test .`,
- **entonces** pasan todas las pruebas: las 278 previas (Documents 44, Permissions 71, Wheelby 163) más las nuevas, con 0 fallidas. Ninguna prueba previa se modificó ni se borró, salvo las que comprobaban que la reserva encolaba el correo directo, que se adaptan al motor nuevo y se indican en la descripción del PR.

**CA-REG-02: los seis frentes**
- **Cuando** se copia `.env.example` a `.env` y se ejecuta `docker compose up --build`,
- **entonces** los seis frentes del README (registro con activación por la cola, sesión, rechazo a Estándar, recuperación, gestión de permisos, negocio con la transición prohibida) responden igual que en `practica-4`, y un documento subido sobrevive a `docker compose down` (sin `-v`).

**CA-REG-03: orden en el historial**
- **Cuando** se revisa `git log --reverse --oneline practica-4..practica-5`,
- **entonces** el commit que agrega este archivo y la versión de `AGENTS.md` para la Práctica 5 aparece **antes** que cualquier commit que toque `src/backend/src/Core/Notifications`, `Host.Worker` o los reportes.
