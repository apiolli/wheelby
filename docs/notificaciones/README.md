# Notificaciones y cola de correos (Práctica 5)

Diseño del motor de notificaciones, de la cola de correos y cómo verificar cada criterio `CA-NOT-*` de [docs/criterios-notificaciones-reportes.md](../criterios-notificaciones-reportes.md). Los pasos para un revisor están en el README principal, secciones [Notificaciones](../../README.md#12-notificaciones) y [Reportes](../../README.md#13-reportes).

---

## 1. Piezas

| Pieza | Dónde | Qué hace |
|---|---|---|
| `INotificationPublisher` | `Notifications.Contracts` | Lo único que ven Permissions y Wheelby. `PublishAsync(message, recipientIds)` y `PublishToAdministratorsAsync(message, excludeUserIds)` |
| `NotificationTypes` | `Notifications.Contracts` | Los tipos estables (tabla de la sección 2) |
| `PublishNotification` | `Notifications.Application/Features/PublishNotification` | Por cada destinatario: busca su contacto (`IUserDirectory`), crea la `Notification` y, si tiene el correo activado, el `QueuedEmail` con el mismo título como asunto. Un solo `SaveChanges` |
| `NotificationPublisher` | `Notifications.Infrastructure/Inbox` | Implementa el contrato sobre `ISender`. Para los Administradores usa `IAdministratorDirectory` |
| `IAdministratorDirectory` | `AccessControl.Contracts` | Solo lectura: Administradores **activos** = rol `Administrator`, `ActivatedAt` con valor y `DisabledAt` nulo |
| `SendPendingEmails` | `Notifications.Application/Features/SendPendingEmail` | Una pasada del procesador de la cola (sección 4) |
| `Host.Worker` | `Host/Host.Worker/Program.cs` | Corre la pasada cada `Worker__IntervalSeconds`, o una sola vez con `--once` |

Tablas del esquema `notifications`:

| Tabla | Columnas principales |
|---|---|
| `QueuedEmails` | `Id`, `To`, `Subject`, `Body`, `Status` (`Pending`, `Sent`, `Failed`), `Attempts`, `CreatedAt`, `LastAttemptAt`, `SentAt`, `FailedAt`, `LastError` (máx. 500) |
| `Notifications` | `Id`, `RecipientId`, `Type`, `Title`, `Message`, `ReferenceId`, `CreatedAt`, `ReadAt`, `EmailId` |
| `NotificationPreferences` | `UserId` (PK), `EmailEnabled`, `UpdatedAt`. Sin fila = correo activado |

Migraciones nuevas (las anteriores no se tocaron): `QueuedEmailRetries`, `NotificationsInbox` y `NotificationPreferences`.

## 2. Tipos y destinatarios

| Evento | Dónde se engancha | Destinatarios | Tipo |
|---|---|---|---|
| Crear solicitud de permiso | `CreatePermissionRequestCommandHandler`, después de guardar | Administradores activos menos el solicitante | `permission.request-created` |
| Aprobar solicitud | `ApprovePermissionRequestCommandHandler`, después de guardar | Solicitante | `permission.request-approved` |
| Rechazar solicitud | `RejectPermissionRequestCommandHandler`, después de guardar | Solicitante, con el motivo | `permission.request-rejected` |
| **Solicitar una reserva** (evento del negocio de RF-NOT-03) | `NotifyReservationStatusChangedHandler` (evento `ReservationStatusChanged` con `From = null`) | Anfitrión | `reservation.requested` |
| `Pending → Confirmed` | igual | Inquilino | `reservation.confirmed` |
| `Pending → Cancelled` con `/reject` | igual (el evento trae `IsRejection = true`) | Inquilino, con el motivo | `reservation.rejected` |
| `Pending/Confirmed → Cancelled` por el inquilino | igual | Anfitrión | `reservation.cancelled` |
| `Pending/Confirmed → Cancelled` por el anfitrión con `/cancel` | igual | Inquilino, con el motivo | `reservation.cancelled` |
| `Pending → Cancelled` por expiración (Worker, actor `System`) | igual | Anfitrión **e** inquilino | `reservation.expired` |
| `Confirmed → InProgress` | igual | Inquilino | `reservation.started` |
| `InProgress → Completed` | igual | Inquilino | `reservation.completed` |

Quien actúa nunca recibe su propio aviso. El correo de cada notificación lleva el título como asunto y como cuerpo el saludo, el mensaje y la línea fija «Puedes ver tus notificaciones en Wheelby.».

## 3. Decisiones

- **Los correos de cuenta no son notificaciones.** Activación, reenvío, recuperación y restablecimiento forzado siguen por `IEmailQueue`: entran directo a la cola, no aparecen en la bandeja y no consultan la preferencia, para que desactivar el correo nunca impida activar una cuenta ni recuperar la contraseña. Sí cumplen las reglas de la cola (reintentos, `Failed`, consulta del Administrador).
- **Notificar es un efecto secundario posterior al guardado, no un outbox.** Primero se guarda la operación (la solicitud, la reserva); después se publica el aviso. Si el motor falla, el error se registra en el log y la operación **no** se revierte ni cambia su respuesta. Contrapartida aceptada: si el proceso cae entre el guardado de la operación y el del aviso, ese aviso se pierde.
- **Solo se notifica lo que tuvo éxito.** Un 400, 403, 404 o 409 sale del handler antes de guardar, y por lo tanto antes de notificar.
- **Notificación y correo en una transacción.** `PublishNotification` agrega las notificaciones y los correos al mismo `DbContext` y hace un solo `SaveChanges`.
- **Permissions sin el motor.** El notificador de Permissions resuelve el motor como opcional: si un host registra Permissions sin Notifications, se registra un aviso en el log y no se notifica. En la API y en el Worker siempre está registrado.
- **Sin backoff.** Cada pasada reintenta los `Pending`. Con el intervalo de 10 s, un correo imposible queda `Failed` en unos 50 s.

## 4. El procesador de la cola y cómo se evita la duplicación

`SendPendingEmailsCommandHandler` procesa hasta 50 correos por pasada, **uno por transacción**:

1. Abre una transacción.
2. Toma el `Pending` más viejo que no haya intentado ya en esta pasada:

   ```sql
   SELECT * FROM notifications."QueuedEmails"
   WHERE "Status" = 'Pending' AND NOT ("Id" = ANY(@procesados))
   ORDER BY "CreatedAt", "Id"
   LIMIT 1
   FOR UPDATE SKIP LOCKED
   ```

   `FOR UPDATE` bloquea la fila hasta el commit y `SKIP LOCKED` hace que otro procesador que corra a la vez (el Worker continuo y un `docker compose run --rm worker --once`) **salte** esa fila en lugar de esperarla o tomarla. Así cada correo lo toma un solo procesador (CA-NOT-12.2). Está en `Notifications.Infrastructure/Persistence/QueuedEmailRepository.cs`.
3. Intenta enviarlo con `IEmailSender`:
   - si sale bien, `MarkAsSent` (estado `Sent`, `SentAt`, `Attempts + 1`);
   - si falla, `RegisterFailure(error, utcNow, maxAttempts)`: `Attempts + 1`, `LastAttemptAt` y `LastError`; al llegar a `Notifications__MaxEmailAttempts` (5) queda `Failed` con `FailedAt`.
4. Guarda y confirma la transacción. Un fallo no corta la pasada (CA-NOT-10.4).

`Sent` y `Failed` son terminales: el dominio rechaza cualquier cambio (`ConflictException`) y la consulta solo toma `Pending`, así que una segunda pasada no reenvía nada (CA-NOT-12.1) y un `Failed` no se reintenta (CA-NOT-10.3).

El texto de `LastError` es el tipo y el mensaje de la excepción, recortados a 500 caracteres. `MailKitEmailSender` reemplaza el usuario y la contraseña SMTP por `***` antes de lanzar el error, y nunca se usan las opciones SMTP para armarlo.

**Limitación conocida:** si el proceso cae justo después de que el servidor SMTP aceptó el correo y antes de confirmar `Sent`, la transacción se revierte y ese correo puede salir dos veces. Como cada correo va en su propia transacción, la ventana es de un solo correo.

## 5. Rutas

| Ruta | Política | Respuesta |
|---|---|---|
| `GET /notifications?read=&page=1&pageSize=20` | Sesión (los dos roles) | 200 `{ items, page, pageSize, totalItems, totalPages, hasPreviousPage, hasNextPage, unreadCount }` |
| `POST /notifications/{id}/read` | Sesión | 204; 404 `notification.not-found` |
| `GET /notifications/preferences` | Sesión | 200 `{ "emailEnabled": true }` |
| `PUT /notifications/preferences` | Sesión | 204 |
| `GET /admin/notifications/emails?status=&page=1&pageSize=20` | `RequireAdministrator` | 200 paginada, **sin `body`** |
| `GET /admin/notifications/emails/{id}` | `RequireAdministrator` | 200; 404 `notification.email-not-found` |

Ninguna ruta de `/notifications` recibe un `userId`: el dueño es siempre el usuario de la sesión.

## 6. Verificación de cada criterio

Abreviaturas de las pruebas: **N** = `Notifications.Test`, **P** = `Permissions.Test`, **W** = `Wheelby.Test`.

| Criterio | Cómo se comprueba | Pruebas |
|---|---|---|
| CA-NOT-01.1 | README §12.2 pasos 1 y 2: Admin A y Admin B ven `permission.request-created` con el código y el nombre de Ana | `RF_NOT_01_crear_solicitud_notifica_a_cada_administrador_activo` (P, motor real sobre InMemory) |
| CA-NOT-01.2 | Ana y Beto no la ven; SQL de conteo por tipo da el número de Administradores activos | `RF_NOT_01_crear_solicitud_no_notifica_a_estandar_ni_a_administrador_deshabilitado` (P) |
| CA-NOT-01.3 | Admin B crea una solicitud: la recibe Admin A y no Admin B | `RF_NOT_01_administrador_solicitante_no_recibe_su_propia_notificacion` (P) |
| CA-NOT-02.1 | README §12.2 paso 3 | `RF_NOT_02_aprobar_notifica_solo_al_solicitante` (P) |
| CA-NOT-02.2 | README §12.2 paso 4; SQL: el `Body` del último correo a Ana contiene el motivo | `RF_NOT_02_rechazar_notifica_al_solicitante_con_el_motivo` (P) |
| CA-NOT-02.3 | Repetir la aprobación da 409 y no aparece nada nuevo | `RF_NOT_02_transicion_rechazada_no_crea_notificacion` (P) |
| CA-NOT-03.1 | README §12.3 | `RF_NOT_03_solicitar_reserva_notifica_al_anfitrion` (W) |
| CA-NOT-03.2 | Una prueba por fila de la tabla | `RF_NOT_03_confirmar_notifica_al_inquilino`, `RF_NOT_03_rechazar_notifica_al_inquilino_con_el_motivo`, `RF_NOT_03_cancelar_inquilino_notifica_al_anfitrion`, `RF_NOT_03_cancelar_anfitrion_notifica_al_inquilino_con_el_motivo`, `RF_NOT_03_expirar_notifica_al_anfitrion_y_al_inquilino`, `RF_NOT_03_iniciar_notifica_al_inquilino`, `RF_NOT_03_completar_notifica_al_inquilino`, `RF_NOT_03_los_avisos_de_reserva_ya_no_usan_la_cola_directa` (W) |
| CA-NOT-04.1 | Los `id` de Ana y de Admin A no se cruzan | `RF_NOT_04_un_usuario_no_ve_notificaciones_de_otro` (N) |
| CA-NOT-04.2 | 3, 2 y 1 elementos con `read` vacío, `false` y `true`; `unreadCount = 2` | `RF_NOT_04_filtra_leidas_y_no_leidas_y_cuenta_no_leidas` (N) |
| CA-NOT-04.3 | Sin `Authorization`, o con un token cerrado con `/auth/logout` → 401 | `RF_NOT_04_rutas_de_notificaciones_exigen_sesion` (N) |
| CA-NOT-05.1 | README §12.4 | `RF_NOT_05_marcar_propia_como_leida`, `RF_NOT_05_marcar_dos_veces_conserva_la_primera_fecha` (N) |
| CA-NOT-05.2 | Ana marca la de Admin A → 404 `notification.not-found` y sigue sin leer | `RF_NOT_05_marcar_la_de_otro_se_rechaza_y_no_cambia`, `RF_NOT_05_un_id_que_no_existe_da_la_misma_respuesta` (N) |
| CA-NOT-06.1 | SQL de la sección 7 (join por `EmailId`) | `RF_NOT_06_notificacion_crea_entrada_en_bandeja_y_correo_encolado` (N) |
| CA-NOT-07.1 | README §12.5 | `RF_NOT_07_preferencia_por_defecto_es_correo_activado`, `RF_NOT_07_desactivar_el_correo_se_guarda_y_se_lee` (N) |
| CA-NOT-07.2 | README §12.5: el conteo de `QueuedEmails` de Ana no cambia | `RF_NOT_07_correo_desactivado_crea_notificacion_sin_encolar_correo` (N), `RF_NOT_02_con_el_correo_desactivado_queda_solo_la_notificacion` (P) |
| CA-NOT-07.3 | `POST /auth/forgot-password` con el correo desactivado: el correo con el código sí se encola | `RF_NOT_07_correos_de_cuenta_ignoran_la_preferencia` (N) |
| CA-NOT-08.1 | Con el SMTP de ejemplo, registrar o solicitar responde 201 y el correo queda `Pending` con `Attempts = 0` | `RF_NOT_08_operacion_termina_aunque_el_envio_falle` (N) |
| CA-NOT-09.1 | `docker compose logs -f worker` | — |
| CA-NOT-09.2 | `docker compose run --rm worker --once`; `$LASTEXITCODE` = 0 | — |
| CA-NOT-10.1 | README §12.7 y `.env.example` | `RF_NOT_10_el_maximo_es_configurable` (N) |
| CA-NOT-10.2 | SQL tras un `--once` | `RF_NOT_10_fallo_incrementa_intentos_y_guarda_ultimo_error` (N) |
| CA-NOT-10.3 | SQL tras 6 `--once` | `RF_NOT_10_al_llegar_al_maximo_queda_fallido`, `RF_NOT_10_correo_fallido_no_se_reintenta` (N) |
| CA-NOT-10.4 | — | `RF_NOT_10_un_fallo_no_impide_enviar_los_demas` (N) |
| CA-NOT-11.1 | README §12.6 | `RF_NOT_11_administrador_consulta_la_cola_con_estado_intentos_fechas_y_error` (N) |
| CA-NOT-11.2 | El JSON no trae `body` | `RF_NOT_11_la_consulta_no_expone_el_cuerpo` (N) |
| CA-NOT-11.3 | Token Estándar → 403; sin sesión → 401 | `RF_NOT_11_rutas_de_la_cola_exigen_administrador` (N) |
| CA-NOT-12.1 | Dos `--once` seguidos: el segundo dice `Sent: 0` | `RF_NOT_12_dos_pasadas_no_duplican_el_envio` (N) |
| CA-NOT-12.2 | Revisión de `QueuedEmailRepository.ClaimNextPendingAsync` y de la sección 4 de este documento | `RF_NOT_12_solo_toma_correos_pendientes`, `RF_NOT_12_cada_correo_se_confirma_en_su_propia_transaccion` (N). El bloqueo en sí se verifica contra PostgreSQL: InMemory no lo soporta |
| CA-NOT-13.1 | `git ls-files \| grep -E '(^\|/)\.env$'` vacío; `.env.example` solo con valores de ejemplo; `SmtpOptions` sin valores reales | — |

## 7. Consultas de apoyo

```sql
-- Notificaciones con su correo (CA-NOT-06.1)
SELECT n."Type", n."Title", q."Subject", q."To", q."Status"
FROM notifications."Notifications" n
JOIN notifications."QueuedEmails" q ON q."Id" = n."EmailId"
ORDER BY n."CreatedAt" DESC;

-- Estado de la cola
SELECT "To", "Subject", "Status", "Attempts", "LastAttemptAt", "SentAt", "FailedAt", "LastError"
FROM notifications."QueuedEmails" ORDER BY "CreatedAt" DESC;

-- Preferencias
SELECT * FROM notifications."NotificationPreferences";
```
