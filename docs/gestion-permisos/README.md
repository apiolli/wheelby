# Gestión de permisos

Asignación 2 (RF-GP-01 a RF-GP-07, RD-03, RD-04, RD-06, RD-07, RD-09, RD-11, RD-12).

Solicitud de permisos, aprobación o rechazo por parte de un Administrador, concesión aplicada y comprobación de permisos en el servidor.

**Estado por pull request**

| PR                                  | Requisitos                 | Estado       |
| ----------------------------------- | -------------------------- | ------------ |
| Solicitud y consulta de permisos    | RF-GP-01, RF-GP-02         | Implementado |
| Aprobación y rechazo                | RF-GP-03, RF-GP-04         | Implementado |
| Concesión aplicada y comprobación   | RF-GP-05                   | Implementado |
| Transiciones prohibidas             | RF-GP-06                   | Implementado |
| Nadie resuelve la propia solicitud | RF-GP-07                   | Implementado |
| Persistencia en PostgreSQL          | RD-09                      | Implementado |

## Requisitos previos

- .NET SDK 10 y Docker Desktop.
- Todos los comandos se ejecutan desde la carpeta `src/backend` (la que contiene `Wheelby.slnx`).
- La base de datos y los secretos se preparan siguiendo el README principal (PostgreSQL con Docker Compose y `dotnet user-secrets`).
- Al arrancar, la API aplica también las migraciones del módulo Permissions (esquema `permissions`).

## Personas de la prueba

| Persona | Cómo obtenerla |
| ------- | -------------- |
| **Administrador** | `POST /auth/login` con `Admin__Email` y `Admin__Password` (los secretos de la API). Su token se llama **token de Administrador**. |
| **Usuario Estándar** | Registrarse en `POST /auth/register`, activar la cuenta con el enlace del correo (o leerlo sin esperar el correo con `SELECT "Body" FROM notifications."QueuedEmails" ORDER BY "CreatedAt" DESC;`) y hacer `POST /auth/login`. Su token se llama **token de Estándar**. |

Un Administrador **no** tiene concesiones automáticas: para usar `POST /permissions/demo-action` necesita que le aprueben el permiso.

## Catálogo de permisos

El catálogo se arma en DI con los `IPermissionDefinition` que registra cada módulo. El propio módulo Permissions registra uno:

| Código | Descripción |
| ------ | ----------- |
| `core.demo-action` | Acción de demostración del Core. |

Un código que no esté en el catálogo se rechaza con **400** al solicitarlo. El código se valida por forma: minúsculas, dígitos, puntos y guiones, máximo 100 caracteres (`^[a-z0-9]+([.-][a-z0-9]+)*$`).

## Rutas

| Ruta | Sesión | Cuerpo | Qué hace |
| ---- | ------ | ------ | -------- |
| `POST /permissions/requests` | Autenticado | `{ "permissionCode": "core.demo-action" }` | Crea la solicitud. Responde **201** con el `id`. |
| `GET /permissions/requests/mine` | Autenticado | — | Lista paginada de las solicitudes del usuario actual. |
| `POST /permissions/demo-action` | Autenticado **con la concesión** | — | **200** si el usuario tiene el permiso, **403** si no. |
| `GET /admin/permissions/requests?page=1&pageSize=20` | Administrador | — | Lista paginada de las solicitudes pendientes de todos. |
| `POST /admin/permissions/requests/{id}/approve` | Administrador | — | Aprueba y aplica. Responde **204**. |
| `POST /admin/permissions/requests/{id}/reject` | Administrador | `{ "reason": "..." }` | Rechaza. Responde **204**. |

Sin sesión → **401**. Con sesión pero sin rol de Administrador en las rutas `/admin/permissions` → **403** ("No tienes permiso para realizar esta operación.").

Las rutas de administración cuelgan de un único grupo, `/admin/permissions`, protegido por la política `RequireAdministrator` (el mismo patrón que `/admin/users`). Ningún handler vuelve a comprobar el rol.

## Cómo provocar cada criterio de aceptación

Las pruebas se hacen desde Scalar (`/scalar/v1`) y con consultas SQL en un cliente de PostgreSQL (por ejemplo DBeaver: host `localhost`, el puerto de `POSTGRES_PORT`, y la base, usuario y contraseña de tu `.env`). PostgreSQL distingue mayúsculas en los nombres con comillas, así que las consultas llevan comillas dobles.

Para autenticarte en Scalar: ejecuta `POST /auth/login`, copia el `accessToken` de la respuesta y pégalo en el botón de autorización (esquema Bearer) de Scalar. Para probar las rutas de administración, pega el token del Administrador.

### Solicitud de permiso

- [x] **RF-GP-01** — Con el **token de Estándar**, crea una solicitud y comprueba que nace `Pending`:

  ```
  POST /permissions/requests
  { "permissionCode": "core.demo-action" }
  ```

  Responde **201** con el `id` de la solicitud. En la base:

  ```sql
  SELECT "Id", "RequesterId", "PermissionCode", "Status", "CreatedAt"
  FROM permissions."PermissionRequests"
  ORDER BY "CreatedAt" DESC;
  ```

  La fila recién creada tiene `"Status" = 'Pending'`.

  - Un código inexistente en el catálogo, por ejemplo `{ "permissionCode": "otro.permiso" }` → **400**.
  - Un código mal formado, por ejemplo `{ "permissionCode": "Core Demo" }` → **400** con el error en el campo `PermissionCode`.
  - Repetir la misma solicitud mientras la anterior siga `Pending` → **409**.

### Consulta de solicitudes

- [x] **RF-GP-02** — Con el **token de Administrador**:

  ```
  GET /admin/permissions/requests?page=1&pageSize=20
  ```

  Responde **200** con `items`, `page`, `pageSize`, `totalItems`, `totalPages`, `hasPreviousPage` y `hasNextPage`. Cada item trae `id`, `requesterId`, `permissionCode`, `status`, `approverId`, `createdAt`, `resolvedAt` y `rejectionReason` (sin nombres de usuario: basta el `id`).

  Con el **token de Estándar**, la misma ruta responde **403**:

  ```powershell
  Invoke-RestMethod -Method Get -Uri "http://localhost:5299/admin/permissions/requests?page=1&pageSize=20" `
    -Headers @{ Authorization = "Bearer <token de un usuario Estándar>" }
  ```

  Y con ese mismo token de Estándar:

  ```
  GET /permissions/requests/mine
  ```

  Responde **200** con **solo** sus solicitudes (las del `requesterId` de su token). Para verlo mejor, crea antes una solicitud con otro usuario: la de ese otro usuario no aparece en esta lista.

### Aprobación

- [x] **RF-GP-03** — Con el **token de Administrador**, aprueba la solicitud pendiente:

  ```
  POST /admin/permissions/requests/{id}/approve
  ```

  Responde **204**. En la base, la solicitud queda `Applied`, con el aprobador y la fecha:

  ```sql
  SELECT "Id", "Status", "ApproverId", "ResolvedAt"
  FROM permissions."PermissionRequests"
  WHERE "Id" = '<id de la solicitud>';
  ```

  `"Status" = 'Applied'`, `"ApproverId"` con el `id` del Administrador y `"ResolvedAt"` con valor.

  Un `id` inexistente → **404**.

### Rechazo

- [x] **RF-GP-04** — Rechazar **sin motivo** deja la solicitud como estaba:

  ```
  POST /admin/permissions/requests/{id}/reject
  { "reason": "" }
  ```

  Responde **400** ("El motivo del rechazo es obligatorio.") y la solicitud **sigue `Pending`**:

  ```sql
  SELECT "Status", "ApproverId", "ResolvedAt", "RejectionReason"
  FROM permissions."PermissionRequests"
  WHERE "Id" = '<id de la solicitud>';
  ```

  Con un motivo:

  ```
  POST /admin/permissions/requests/{id}/reject
  { "reason": "No corresponde al perfil del usuario" }
  ```

  Responde **204** y en la base queda `"Status" = 'Rejected'`, `"RejectionReason"` con el motivo, `"ApproverId"` y `"ResolvedAt"` con valor.

  Un `id` inexistente → **404**.

### Concesión aplicada

- [x] **RF-GP-05** — Con el **token de Estándar** que creó la solicitud, antes de que el Administrador la apruebe:

  ```
  POST /permissions/demo-action
  ```

  Responde **403** ("No tienes el permiso requerido para esta operación.").

  El Administrador aprueba la solicitud (RF-GP-03) y, **con el mismo token de Estándar**, la misma ruta responde **200**:

  ```json
  { "message": "Acción de demostración ejecutada." }
  ```

  En la base existe la fila de la concesión:

  ```sql
  SELECT "UserId", "PermissionCode", "GrantedAt", "RequestId"
  FROM permissions."PermissionGrants"
  ORDER BY "GrantedAt" DESC;
  ```

  Una fila con el `UserId` del Estándar, el `PermissionCode` `core.demo-action` y el `RequestId` de la solicitud aprobada.

  Un Administrador **sin** la concesión también recibe **403** en esa ruta.

### Transiciones prohibidas

- [x] **RF-GP-06** — Aprobar **dos veces** la misma solicitud:

  ```
  POST /admin/permissions/requests/{id}/approve
  ```

  La segunda vez responde **409** y el estado **no cambia**:

  ```sql
  SELECT "Status" FROM permissions."PermissionRequests" WHERE "Id" = '<id>';
  ```

  Rechazar y luego intentar aprobar la misma solicitud → **409**, y el estado sigue siendo `Rejected`.

  **Aclaración:** la transición `Rejected → Applied` **no se puede provocar por HTTP**. La aplicación es automática y ocurre dentro de la misma operación que aprueba (no hay ruta que la aplique por separado), y una solicitud rechazada ni siquiera llega a `Approved`. Esa transición exacta —y el hecho de que el estado no cambie— la cubre la prueba unitaria `RF_GP_06_Aplicar_una_rechazada_es_rechazado_y_conserva_Rejected` del proyecto `Permissions.Test`.

### Resolver la propia solicitud

- [x] **RF-GP-07** — Con el **token de Administrador**, crea una solicitud (el Administrador es un usuario más):

  ```
  POST /permissions/requests
  { "permissionCode": "core.demo-action" }
  ```

  Responde **201**. Ahora, con **ese mismo token**, intenta resolverla:

  ```
  POST /admin/permissions/requests/{id}/approve
  ```

  Responde **403** ("No puedes resolver tu propia solicitud de permiso."). Lo mismo con `POST /admin/permissions/requests/{id}/reject` → **403**.

  En la base, la solicitud sigue `Pending`:

  ```sql
  SELECT "Status", "ApproverId", "ResolvedAt" FROM permissions."PermissionRequests"
  WHERE "Id" = '<id de la solicitud>';
  ```

### Persistencia

- [x] **RD-09** — Reiniciar la API (Ctrl+C y volver a ejecutarla) y comprobar que las solicitudes siguen ahí:

  ```sql
  SELECT "Id", "PermissionCode", "Status" FROM permissions."PermissionRequests" ORDER BY "CreatedAt" DESC;
  ```

  Las solicitudes y las concesiones siguen existiendo: viven en PostgreSQL, esquema `permissions`.

## Pruebas automáticas

Las 71 pruebas de `Permissions.Test` están en `src/backend/src/Core/Permissions/Permissions.Test/` y se ejecutan sin levantar la API ni PostgreSQL:

```bash
cd src/backend
dotnet test
```

Cubren el dominio (reglas y matriz completa de transiciones), los casos de uso (handlers con dobles de sus puertos) e infraestructura (`PermissionRequestReadStore` y `PermissionChecker` sobre EF InMemory). Cada prueba lleva el ID del requisito en el nombre (`RF_GP_0X_...`) y el README principal enumera los defectos que la suite detecta.

## Notas de diseño que afectan a la verificación

- **Un solo lugar para los estados (RD-04):** los estados están en `PermissionRequestStatus` y la tabla de transiciones en `PermissionRequestStateMachine.AllowedTransitions`. `PermissionRequest.TransitionTo` es el único código que cambia `Status`; los métodos `Approve`, `Reject` y `MarkApplied` solo lo llaman.
- **El dominio no lee el reloj (RD-11):** las fechas llegan como parámetro `utcNow`; el handler lo obtiene del `IClock` inyectado.
- **Aplicación automática:** al aprobar se hace `Pending → Approved → Applied` y se crea el `PermissionGrant` **en la misma transacción**. Si algo falla, se revierte todo y la solicitud sigue `Pending`.
- **Carreras entre peticiones simultáneas:** un índice único parcial sobre `PermissionRequests (RequesterId, PermissionCode)` con filtro `"Status" = 'Pending'` impide dos solicitudes pendientes del mismo permiso, y un índice único sobre `PermissionGrants (UserId, PermissionCode)` impide dos concesiones del mismo permiso. La tabla `PermissionRequests` usa la columna de sistema `xmin` de PostgreSQL como token de concurrencia: una actualización simultánea se rechaza con **409**.
- **Errores por código HTTP:** 400 (datos inválidos o código fuera del catálogo), 403 (sin rol, sin concesión, o la propia solicitud), 404 (la solicitud no existe), 409 (estado que no admite la transición, solicitud o concesión duplicada). Nunca un 500 por estos casos.
- **Sin notificaciones ni auditoría:** esta pieza no envía correos y no registra bitácora de decisiones.
- **Referencia lógica, no clave foránea:** `RequesterId`, `ApproverId` y `UserId` apuntan a usuarios del módulo de Control de acceso sin clave foránea entre esquemas.

## Pull requests de esta pieza

- [x] Solicitud y consulta de permisos (RF-GP-01, RF-GP-02)
- [x] Aprobación y rechazo (RF-GP-03, RF-GP-04)
- [x] Concesión aplicada y comprobación de permisos (RF-GP-05)
- [x] Transiciones prohibidas y estados terminales (RF-GP-06)
- [x] Nadie resuelve la propia solicitud (RF-GP-07)
- [x] Persistencia en PostgreSQL (RD-09)
