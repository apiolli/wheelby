# Tarea para el agente: Práctica 5 (Notificaciones, cola de correos y reportes)

Este es el encargo. Antes de empezar, lee completos:

1. `AGENTS.md`: reglas del repositorio, convenciones y restricciones.
2. `docs/criterios-notificaciones-reportes.md`: **el contrato**. Cada criterio `CA-*` se tiene que cumplir y tener su prueba.
3. Este documento: el diseño acordado, las rutas, los cambios por módulo y el orden de commits.

Si este documento y los criterios se contradicen, mandan los criterios. Si algo no está claro o choca con `AGENTS.md`, **detente y pregunta**; no supongas.

**Alcance:** solo el backend (`src/backend`), la documentación (`README.md`, `docs/`) y `.env.example`. El frontend **no se toca** en esta práctica: se trabajará después, sobre la API ya terminada.

---

## 1. Punto de partida (lo que ya existe)

| Pieza | Dónde | Estado |
|---|---|---|
| Cola mínima | `Core/Notifications` | `QueuedEmail(To, Subject, Body, Status Pending/Sent, CreatedAt, SentAt)`. `IEmailQueue.EnqueueAsync` (Contracts) → `EnqueueEmailCommand`. `SendPendingEmailsCommand` envía con `MailKitEmailSender`. **No hay intentos, ni último error, ni estado `Failed`: hoy un correo que falla queda `Pending` para siempre.** |
| Worker | `Host/Host.Worker` | `EmailDispatchWorker` (cada `Worker__IntervalSeconds`) y `ReservationExpiryWorker`. `ENTRYPOINT ["dotnet", "Host.Worker.dll"]`, así que los argumentos de `docker compose run worker <args>` llegan a `Program.cs`. |
| Directorio de usuarios | `AccessControl.Contracts/IUserDirectory` | Solo `GetContactAsync(userId)`, que excluye a los deshabilitados. |
| Permisos | `Core/Permissions` | `Create`/`Approve`/`Reject` con handlers en `Permissions.Application/Features/*`. `PermissionsUnitOfWork` **no** publica eventos de dominio. Una aprobación termina en `Applied` (Approve + MarkApplied). |
| Avisos de reserva (RF-NEG-07) | `Wheelby.Application` | `Reservation.TransitionTo` emite `ReservationStatusChanged`; `WheelbyUnitOfWork` lo publica después de guardar; `NotifyReservationStatusChangedHandler` (marcado `IWorkerHandler`) encola un correo directo a la contraparte, o a los dos si actúa el sistema. Corre en la API **y** en el Worker (expiración). |
| Correos de cuenta | `Core/AccessControl` | Activación, reenvío, recuperación y restablecimiento forzado usan `IEmailQueue`. **Se quedan así** (no son notificaciones). |

---

## 2. Diseño acordado

### 2.1 Módulo Notifications (Core)

**Dominio** (`Notifications.Domain`)

- `QueuedEmail`: se amplía.
  - Campos nuevos: `Attempts` (int, 0), `LastAttemptAt` (DateTime?), `LastError` (string?, máximo 500), `FailedAt` (DateTime?).
  - `EmailStatus` agrega `Failed`.
  - `RegisterFailure(string error, DateTime utcNow, int maxAttempts)`: suma un intento, guarda el error recortado a 500 y, si `Attempts >= maxAttempts`, pasa a `Failed` y fija `FailedAt`.
  - `MarkAsSent` también suma el intento (`Attempts` queda en 1 si salió a la primera).
  - Ni `Sent` ni `Failed` admiten más cambios: `ConflictException`.
- `Notification` (nueva, `AggregateRoot<Guid>` o `Entity<Guid>`): `RecipientId` (Guid), `Type` (string, máximo 100, por ejemplo `permission.request-created`), `Title` (máximo 200), `Message` (texto), `ReferenceId` (Guid?, el id de la solicitud o la reserva), `CreatedAt`, `ReadAt` (DateTime?), `EmailId` (Guid?).
  - `MarkAsRead(utcNow)` es idempotente: si ya está leída, no cambia `ReadAt`.
  - `IsRead => ReadAt != null`.
- `NotificationPreference` (nueva): `UserId` (PK), `EmailEnabled` (bool), `UpdatedAt`. Si un usuario no tiene fila, su correo está activado.

**Contratos** (`Notifications.Contracts`), lo único que ven los otros módulos

```csharp
public interface INotificationPublisher
{
    // Notifica a usuarios concretos (por Id). El motor resuelve el correo con IUserDirectory,
    // respeta la preferencia y guarda notificación + correo en una sola transacción.
    Task PublishAsync(NotificationMessage message, IReadOnlyCollection<Guid> recipientIds, CancellationToken ct = default);

    // Notifica a todos los Administradores activos, excepto los de excludeUserIds.
    Task PublishToAdministratorsAsync(NotificationMessage message, IReadOnlyCollection<Guid> excludeUserIds, CancellationToken ct = default);
}

public sealed record NotificationMessage(string Type, string Title, string Message, Guid? ReferenceId);

public static class NotificationTypes { /* constantes con los tipos de la tabla 2.3 */ }
```

`IEmailQueue` se conserva para los correos de cuenta.

**Aplicación** (`Notifications.Application/Features/...`), un caso de uso por carpeta, como en Permissions:

| Feature | Tipo | Qué hace |
|---|---|---|
| `PublishNotification` | Command | Por cada destinatario: obtiene su contacto (`IUserDirectory.GetContactAsync`; si es `null`, lo omite y lo registra en el log), crea la `Notification`, y si tiene el correo activado crea el `QueuedEmail` y fija `EmailId`. Un solo `SaveChanges`. |
| `ListMyNotifications` | Query | `GET /notifications`: paginada, filtro `read`, `unreadCount`, del `ICurrentUser`. |
| `MarkNotificationAsRead` | Command | Busca por `id` **y** `RecipientId = currentUser`. Si no la encuentra, `NotFoundException` con `code` `notification.not-found`. |
| `GetMyPreferences` / `UpdateMyPreferences` | Query / Command | Sobre `NotificationPreference` del usuario actual. |
| `ListQueuedEmails` / `GetQueuedEmail` | Query | Para el Administrador. **Sin `Body`.** |
| `SendPendingEmails` | Command | Se reescribe (ver 2.2). |

**Infraestructura** (`Notifications.Infrastructure`)

- `NotificationsDbContext`: agrega `Notifications` y `NotificationPreferences`, con índices `(RecipientId, ReadAt, CreatedAt)` y `(Status, CreatedAt)` en `QueuedEmails`.
- Una migración **nueva**, por ejemplo `NotificationsInboxAndRetries`, que agrega las columnas de `QueuedEmails` (`Attempts` con valor por defecto 0) y crea las dos tablas. No se tocan las migraciones existentes.
- `Endpoints/Endpoints.cs` con `MapNotificationsEndpoints()`, que se registra en `Host.WebAPI/Program.cs`.
- `NotificationOptions` (`Notifications:MaxEmailAttempts`, por defecto 5, mínimo 1). Variable de entorno `Notifications__MaxEmailAttempts`.
- `INotificationPublisher` implementado sobre `ISender` (como hoy `EmailQueue`).
- Las plantillas del correo son texto plano en español: asunto = `Title`, cuerpo = `Message` + una línea fija "Puedes ver tus notificaciones en Wheelby.".

### 2.2 Procesador de la cola (RF-NOT-08 a 12)

1. **Tomar el lote sin carrera:** en una transacción, seleccionar hasta N `Pending` ordenados por `CreatedAt` con `FOR UPDATE SKIP LOCKED` (SQL crudo o `FromSql` en el repositorio de Infrastructure). Así, el Worker continuo y un `--once` simultáneo nunca toman la misma fila (CA-NOT-12.2). Documéntalo en `docs/notificaciones/README.md`.
2. Por cada correo: intentar `IEmailSender.SendAsync`.
   - Si sale bien: `MarkAsSent`.
   - Si falla: `RegisterFailure(Describe(ex), utcNow, maxAttempts)`.
   - `Describe` usa el tipo y el mensaje de la excepción, recortados a 500 caracteres, **nunca** las opciones SMTP.
   - Un fallo no corta el lote (CA-NOT-10.4).
3. Guardar y confirmar la transacción. El resultado `(Sent, Failed)` cuenta los fallos de la pasada.
4. **Sin backoff:** cada pasada reintenta los `Pending`. Con el intervalo de 10 s, un correo imposible queda `Failed` en unos 50 s. Así lo puede ver el revisor.
5. **Limitación conocida** (va al README): si el proceso cae justo después de que el SMTP aceptó el correo y antes de guardar `Sent`, ese correo puede salir dos veces. Se acepta y se documenta.
6. **`--once`:** en `Host.Worker/Program.cs`, si `args` contiene `--once`, se construye el host **sin** los `BackgroundService`, se ejecuta una pasada de `SendPendingEmailsAsync`, se escribe en el log `Pending emails processed. Sent: N. Failed: M.` (también con 0 y 0) y se sale con código 0. Comando: `docker compose run --rm worker --once`. **No se modifica el Dockerfile.**

### 2.3 Eventos que notifican

| Origen | Dónde se engancha | Destinatarios | Tipo | Contenido mínimo |
|---|---|---|---|---|
| Crear solicitud de permiso | `CreatePermissionRequestCommandHandler`, **después** de `SaveChangesAsync` | `PublishToAdministratorsAsync`, excluyendo al solicitante | `permission.request-created` | Nombre del solicitante, `permissionCode` |
| Aprobar | `ApprovePermissionRequestCommandHandler`, después de guardar | Solicitante | `permission.request-approved` | "aprobada", `permissionCode` |
| Rechazar | `RejectPermissionRequestCommandHandler`, después de guardar | Solicitante | `permission.request-rejected` | "rechazada", `permissionCode` y el **motivo** |
| Reserva solicitada (**evento de RF-NOT-03**) | Donde hoy se avisa de la creación de la reserva. Si `Create` no emite evento, se emite uno al crear (no toca la máquina de estados) | Anfitrión | `reservation.requested` | Marca, modelo, fechas, nombre del inquilino |
| Transiciones de reserva | `NotifyReservationStatusChangedHandler`: se reemplaza `IEmailQueue` por `INotificationPublisher` | Según la tabla de CA-NOT-03.2 | `reservation.*` | Vehículo, fechas, nuevo estado y motivo si hay |

Reglas:

- Notificar es un **efecto secundario**: si falla, se registra en el log y la operación **no** se revierte ni cambia su respuesta. Es el mismo criterio que hoy tiene RF-NEG-07; no es outbox y se documenta.
- Solo se notifica cuando la operación tuvo éxito. Un 400, 403 o 409 no notifica (CA-NOT-02.3).
- Para la lista de Administradores activos, agrega a `IUserDirectory` (AccessControl.Contracts) `Task<IReadOnlyList<UserContact>> ListActiveAdministratorsAsync(CancellationToken ct = default)`: rol `Administrator`, cuenta activada (revisa en `User.cs` cómo se representa) y `DisabledAt == null`. Es **lectura**; no cambia nada de Control de acceso.
- **Dependencias permitidas:** Permissions y Wheelby → `Notifications.Contracts`; Notifications.Infrastructure → `AccessControl.Contracts`. El Core **nunca** referencia a Wheelby (RD-03).
- El Worker ya registra `AddNotifications` y `AddAccessControlUserDirectory`. Comprueba que `INotificationPublisher` funcione ahí sin `ICurrentUser` (la expiración la ejecuta el sistema).

### 2.4 Rutas nuevas

| Ruta | Grupo y política | Cuerpo | Respuesta |
|---|---|---|---|
| `GET /notifications?read=&page=1&pageSize=20` | `/notifications`, `RequireAuthorization()` (los dos roles) | — | 200 `{ items, page, pageSize, totalItems, unreadCount }`. Cada item: `id`, `recipientId`, `type`, `title`, `message`, `referenceId`, `isRead`, `readAt`, `createdAt`, `emailSent` (bool, si tiene `EmailId`) |
| `POST /notifications/{id}/read` | igual | — | 204; 404 `notification.not-found` |
| `GET /notifications/preferences` | igual | — | 200 `{ "emailEnabled": true }` |
| `PUT /notifications/preferences` | igual | `{ "emailEnabled": false }` | 204 |
| `GET /admin/notifications/emails?status=&page=1&pageSize=20` | `/admin/notifications`, `RequireAdministrator` | — | 200 paginada, **sin `body`** |
| `GET /admin/notifications/emails/{id}` | igual | — | 200; 404 |
| `GET /reports/permission-requests?from=&to=` | en Permissions, `/reports`, `RequireAuthorization()` | — | ver 2.5 |
| `GET /reports/vehicle-revenue?from=&to=` | en Wheelby, `/reports`, `RequireAuthorization()` | — | ver 2.5 |

La política se declara en el grupo (RD-06). La regla "solo lo propio" va en el handler, con `ICurrentUser`.

### 2.5 Reportes

Comunes:
- `from` y `to` como `DateOnly` (`yyyy-MM-dd`), obligatorios, `from < to`, validados con FluentValidation (400 por campo).
- Se convierten a `DateTime` UTC `[from 00:00, to 00:00)`.
- `scope` es `"all"` si el usuario es Administrador y `"own"` si no.
- La agregación va en un **read store** de Infrastructure con LINQ `GroupBy` + `Count`/`Sum`, que EF traduce a SQL. Nada de `ToListAsync()` seguido de agrupar en memoria.
- Trampa de `AGENTS.md` §8: usa arreglos o listas en `Contains`, y verifica contra PostgreSQL real, porque EF InMemory acepta consultas que Npgsql no traduce.

**Core: `GET /reports/permission-requests`** (Permissions)
- Filtro: `CreatedAt` en el rango. Si `scope = own`, también `RequesterId = currentUser`.
- `GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month, r.Status })` → `count`. `period = $"{Year:0000}-{Month:00}"`, armado después de agregar.
- Respuesta: `{ from, to, scope, total, rows: [{ period, status, count }] }`, ordenada por `period` y luego por `status`.

**Negocio: `GET /reports/vehicle-revenue`** (Wheelby)
- Filtro: `Status ∈ [Confirmed, InProgress, Completed]` (arreglo) y `StartDate` en el rango. Si `scope = own`, solo vehículos con `OwnerId = currentUser` (join con `Vehicles` dentro del esquema `wheelby`).
- Por `VehicleId`: `count`, `sum(Days)`, `sum(TotalAmount)`, más `Brand`, `Model` y `Plate` del vehículo. `averageAmount = round(total / count, 2)`.
- Respuesta: `{ from, to, scope, currency: "DOP", totals: { reservations, days, totalAmount }, rows: [...] }`, ordenada por `totalAmount` descendente.
- El Administrador puede consultar este reporte aunque no entre a `/vehicles` ni a `/reservations`.

---

## 3. Pruebas

- **Proyecto nuevo `Notifications.Test`** en `src/backend/src/Core/Notifications/Notifications.Test/`, con la misma estructura que `Permissions.Test`: `Domain/`, `Application/`, `Infrastructure/`, `TestSupport/` con dobles a mano, `InternalsVisibleTo` desde los proyectos de Notifications. Se agrega a `Wheelby.slnx`. No necesita línea en el Dockerfile, porque la etapa `test` copia todo `src/backend` y corre `dotnet test Wheelby.slnx`.
- Las pruebas de reportes van en `Permissions.Test` y `Wheelby.Test`. Las del hook de permisos, en `Permissions.Test`; las de reservas, en `Wheelby.Test`.
- Hay que crear todas las pruebas que nombran los criterios, con ese nombre exacto.
- Convenciones de `AGENTS.md` §5: tres fases, reloj fijo, valores esperados a mano, `async Task`, base InMemory con nombre único.
- **Pruebas existentes:** no se modifican ni se borran. La única excepción son las que comprueban que `NotifyReservationStatusChangedHandler` llamaba a `IEmailQueue`: se adaptan para comprobar `INotificationPublisher`, y la lista va en la descripción del PR.
- `FOR UPDATE SKIP LOCKED` no se prueba con InMemory: su prueba unitaria cubre la regla "solo `Pending`" y "dos pasadas no duplican", y la concurrencia se verifica a mano contra PostgreSQL.
- Las pruebas de rutas siguen la trampa de §8: leer `((IEndpointRouteBuilder)app).DataSources` y exigir el número exacto de endpoints y sus políticas.

---

## 4. Documentación que hay que escribir

**`README.md`** (lo que sigue el revisor, al pie de la letra)
- Encabezado "Práctica 5" y `git checkout practica-5` en la sección 2.1.
- La sección 3 avisa que con SMTP de ejemplo los correos terminan `Failed` tras 5 intentos, y que el cuerpo sigue legible en la cola.
- **Sección nueva "Notificaciones"** con:
  1. Las rutas (tabla como la de los frentes) y los tokens que hacen falta.
  2. **Cómo provocar cada evento:** crear una solicitud de permiso (Admin), aprobarla y rechazarla con motivo (solicitante), y el **evento del negocio** (solicitar una reserva → el anfitrión ve `reservation.requested`), paso a paso, con los cuerpos JSON.
  3. Consultar y marcar como leída; intentar marcar la de otro → 404.
  4. Desactivar el correo y comprobar con SQL que no se encola.
  5. **Cómo ejecutar el procesador:** `docker compose run --rm worker --once`, y que el servicio `worker` lo hace solo cada 10 s.
  6. **Cómo provocar un fallo de envío:** con los valores de ejemplo de `.env.example` el SMTP no existe. Con credenciales reales, poner `Smtp__Port=9999` en `.env` y `docker compose up -d worker`. Ejecutar `--once` 5 veces y ver `Attempts`, `Failed` y `LastError` con `GET /admin/notifications/emails?status=Failed` (Admin) y con SQL. Con el token Estándar → 403. Restaurar el puerto.
  7. **Máximo de intentos: 5**, configurable con `Notifications__MaxEmailAttempts`.
  8. Ejecutar `--once` dos veces seguidas y comprobar que no se duplica.
  9. Las limitaciones declaradas: no es outbox, y la posible duplicación si el proceso cae entre el envío y el guardado.
- **Sección nueva "Reportes"** con: la pregunta del reporte del negocio ("¿cuánto generó cada vehículo y cuántos días estuvo alquilado en un período?"), las dos rutas con dos rangos de ejemplo que den resultados distintos sobre los datos del frente 6 (decir qué fechas usar), y la prueba con un token Estándar.
- La sección 8 (variables) agrega `Notifications__MaxEmailAttempts`.
- La sección 9 (SQL) agrega las tablas nuevas.
- La sección "Pruebas" actualiza el conteo total y la tabla por proyecto con `Notifications.Test`. Hoy dice 234 y son 278 antes de esta práctica.
- La sección 11 (estructura) agrega `Notifications.Test` y `docs/notificaciones/`.

**`docs/notificaciones/README.md`** (nuevo): diseño del motor, tabla de tipos, decisión sobre los correos de cuenta, cómo se evita la duplicación, y cada criterio `CA-NOT-*` con su verificación, al estilo de `docs/gestion-permisos/README.md`.

**`docs/wheelby/README.md`**: actualizar la nota de RF-NEG-07 (ahora pasa por el motor), sacar "Reportes (RF-NEG-08)" de pendientes y agregar el reporte.

**`.env.example`**: agregar `Notifications__MaxEmailAttempts=5` con su comentario, y corregir el comentario desactualizado de `ExchangeRate__ApiKey`, que dice «responde 503» (`AGENTS.md` §7.3).

---

## 5. Orden de trabajo y commits

Rama: `feature/practica-5`, creada **desde `main` después de fusionar el PR de documentación** (el que contiene este archivo y los criterios). Un cambio lógico por commit, cada uno compila con las pruebas en verde, con el formato de `AGENTS.md` §2.1 (≤ 50 caracteres, sin tildes, sin trailers). Orden sugerido:

1. `RF-NOT-10: agrega intentos y estado fallido`
2. `RF-NOT-12: toma pendientes con bloqueo de filas`
3. `RF-NOT-09: agrega ejecucion unica del procesador`
4. `RF-NOT-11: agrega consulta de la cola para admin`
5. `RF-NOT-04: agrega bandeja de notificaciones`
6. `RF-NOT-05: permite marcar notificacion como leida`
7. `RF-NOT-07: agrega preferencia de canal de correo`
8. `RF-NOT-06: agrega motor de notificaciones`
9. `RF-NOT-01: notifica a admins al crear solicitud`
10. `RF-NOT-02: notifica resolucion al solicitante`
11. `RF-NOT-03: pasa avisos de reserva al motor`
12. `RF-REP-01: agrega reporte de solicitudes por mes`
13. `RF-REP-02: agrega reporte de ingresos por vehiculo`
14. `RF-NOT-01: documenta notificaciones en el readme`
15. `RF-REP-01: documenta reportes en el readme`

Se integra a `main` con un PR fusionado con **"Create a merge commit"** (nunca squash ni rebase). La etiqueta `practica-5` la crea y la sube el estudiante; el agente no crea etiquetas ni hace push.

---

## 6. Antes de entregar

- [ ] `docker build --target test .` en verde; `dotnet build` con 0 warnings.
- [ ] `docker compose up --build` desde cero (`down -v`) con un `.env` copiado de `.env.example`, y recorrer **cada** paso de las secciones Notificaciones y Reportes del README contra PostgreSQL real.
- [ ] Los seis frentes del README siguen funcionando.
- [ ] Cada `CA-*` de los criterios marcado en la descripción del PR, con la prueba o el paso que lo demuestra.
- [ ] `git diff main --stat` no toca nada de la lista prohibida de `AGENTS.md` §2.3.
- [ ] Ningún secreto en el diff ni en los logs de ejemplo del README.
