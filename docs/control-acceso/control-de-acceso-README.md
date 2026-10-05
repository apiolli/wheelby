# 1. Control de acceso

Semanas 2–4

Registro con activación por correo, sesión, roles, administración de usuarios, y recuperación/restablecimiento de contraseña.

**Estado por pull request**

| PR                                  | Requisitos                                 | Estado       |
| ----------------------------------- | ------------------------------------------ | ------------ |
| Registro y activación               | RF-CA-01, 02, 14, 15, 16, 17               | Implementado |
| Sesión                              | RF-CA-03, 07, 18, 19 (y renovación, extra) | Implementado |
| Administración de usuarios          | RF-CA-04, 05, 06, 08, 20, 21               | Implementado |
| Recuperación de contraseña          | RF-CA-09 a 13, 22                          | Implementado |
| Cola de correos (mínima)            | RF-NOT-08, 09, 12, 13                      | Implementado |
| Estructura de la máquina de estados | RF-NEG-03, 04, 05, RD-04                   | Pendiente    |

Las secciones marcadas como **Pendiente** se completan en el PR que las implementa. Mientras tanto no se pueden verificar.

## Requisitos previos

- .NET SDK 10 y Docker Desktop.
- Un servidor SMTP (por ejemplo Gmail con contraseña de aplicación).
- Todos los comandos se ejecutan desde la carpeta `backend`.
- La base de datos y los secretos se preparan siguiendo el README principal (PostgreSQL con Docker Compose y `dotnet user-secrets`).

## Variables de entorno

Nunca se guardan valores en el repositorio: `.env.example` solo trae ejemplos. El archivo `.env` lo lee **solo Docker Compose**. La API y el Worker leen sus valores de `dotnet user-secrets` (separador `:`) o de variables de entorno (separador `__`). Por ejemplo, `Smtp:Host` en user-secrets es `Smtp__Host` como variable de entorno.

| Variable                                                             | Para qué sirve                                                                                            | Quién la lee   |
| -------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------- | -------------- |
| `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`, `POSTGRES_PORT` | Crean la base de datos del contenedor y el puerto publicado                                               | Docker Compose |
| `ConnectionStrings__Postgres`                                        | Conexión a la base donde persisten usuarios, sesiones y correos en cola                                   | API y Worker   |
| `Smtp__Host`                                                         | Host del servidor de correo saliente                                                                      | Worker         |
| `Smtp__Port`                                                         | Puerto del servidor de correo (587 con STARTTLS)                                                          | Worker         |
| `Smtp__User`                                                         | Usuario con el que se autentica el envío                                                                  | Worker         |
| `Smtp__Password`                                                     | Contraseña de aplicación del servidor de correo                                                           | Worker         |
| `Smtp__From`                                                         | Remitente de los correos                                                                                  | Worker         |
| `App__ActivationUrl`                                                 | URL a la que apunta el enlace del correo de activación (hoy, el endpoint `GET /auth/activate` de la API)  | API            |
| `Jwt__Key`                                                           | Clave secreta con la que se firma la credencial de sesión (mínimo 32 caracteres)                          | API            |
| `Jwt__Issuer`                                                        | Emisor que se escribe y se exige en la credencial                                                         | API            |
| `Jwt__Audience`                                                      | Destinatario que se escribe y se exige en la credencial                                                   | API            |
| `Jwt__ExpirationMinutes`                                             | Vigencia de la sesión y de su credencial, en minutos (60 por defecto)                                     | API            |
| `Admin__Email`                                                       | Correo del Administrador fijo, que se crea o restaura al arrancar la API                                  | API            |
| `Admin__Password`                                                    | Contraseña inicial del Administrador fijo (debe cumplir la política: 8+ caracteres, con letras y números) | API            |

Notas:

- Algunas vigencias están fijadas en el código y no son variables de entorno:
  - token de activación (RF-CA-15): **24 horas** (`User.ActivationTokenLifetime`);
  - código de recuperación (RF-CA-10): **30 minutos** (`User.PasswordResetTokenLifetime`);
  - bloqueo por intentos fallidos (RF-CA-19): **5 intentos, 15 minutos** (`User`).
- Si falta alguna variable `Jwt__*`, o la clave tiene menos de 32 caracteres, **la API no arranca** y el mensaje lo indica (sin mostrar la clave).
- Si falta `Admin__Email` o `Admin__Password`, **la API tampoco arranca**: sin Administrador no se puede administrar nada.
- Secretos de la API (ejecutar desde `backend`; usa el puerto de tu API, que aparece en la consola al arrancar):

  ```bash
  dotnet user-secrets set "App:ActivationUrl" "http://localhost:5299/auth/activate" --project src/Host/Host.WebAPI

  # Genera una clave aleatoria y no la compartas ni la subas al repositorio (PowerShell)
  $key = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
  dotnet user-secrets set "Jwt:Key" $key --project src/Host/Host.WebAPI
  dotnet user-secrets set "Jwt:Issuer" "wheelby" --project src/Host/Host.WebAPI
  dotnet user-secrets set "Jwt:Audience" "wheelby-api" --project src/Host/Host.WebAPI
  dotnet user-secrets set "Jwt:ExpirationMinutes" "60" --project src/Host/Host.WebAPI

  # Administrador fijo (usa tus propios valores)
  dotnet user-secrets set "Admin:Email" "<correo del administrador>" --project src/Host/Host.WebAPI
  dotnet user-secrets set "Admin:Password" "<contraseña del administrador>" --project src/Host/Host.WebAPI
  ```

## Cómo ejecutar

```bash
# 1. Base de datos
docker compose up -d
docker compose ps                                 # debe decir "healthy"

# 2. API (aplica las migraciones y el seed del Administrador al arrancar; Scalar en /scalar/v1)
dotnet run --project src/Host/Host.WebAPI

# 3. Proceso aparte que envía los correos pendientes (una sola pasada)
dotnet run --project src/Host/Host.Worker
```

## Ejecutar el proceso de la cola de correos

```bash
dotnet run --project src/Host/Host.Worker
```

Procesa los correos pendientes en una sola pasada y termina. El log dice cuántos se enviaron y cuántos fallaron; el código de salida es `0` si todo salió bien y `1` si alguno falló. Ejecutarlo de nuevo no reenvía los que ya salieron (ver RF-NOT-09 / RF-NOT-12 abajo). Los correos de activación y de recuperación de contraseña solo llegan a la bandeja después de ejecutarlo.

## Cómo provocar cada criterio de aceptación

Las pruebas se hacen desde Scalar (`/scalar/v1`) y con consultas SQL en un cliente de PostgreSQL (por ejemplo DBeaver: host `localhost`, el puerto de `POSTGRES_PORT`, y la base, usuario y contraseña de tu `.env`). PostgreSQL distingue mayúsculas en los nombres con comillas, así que las consultas llevan comillas dobles.

Para autenticarte en Scalar: ejecuta `POST /auth/login`, copia el `accessToken` de la respuesta y pégalo en el botón de autorización (esquema Bearer) de Scalar.

Para leer el enlace de activación o el código de recuperación sin esperar el correo, el cuerpo del mensaje también queda en la cola:

```sql
SELECT "To", "Status", "Body" FROM notifications."QueuedEmails" ORDER BY "CreatedAt" DESC;
```

### Registro y activación

- [x] **RF-CA-01** — Registrar un correo y repetir el registro con el mismo correo (también cambiando mayúsculas) → el segundo intento se rechaza con 409.

  ```
  POST /auth/register
  { "fullName": "Ana Pérez", "email": "ana@correo.com", "password": "Clave1234" }
  ```

  Repetir con `"email": "ANA@correo.com"` → 409.

- [x] **RF-CA-02** — Registrar dos usuarios con la misma contraseña y revisar la base: los valores almacenados no coinciden entre sí ni con la contraseña en texto plano.

  ```sql
  SELECT "Email", "PasswordHash" FROM access_control."Users";
  ```

- [x] **RF-CA-14** — Registrar con una contraseña de menos de 8 caracteres, y con una sin números o sin letras → 400 con el error en el campo `Password`. También con un correo mal formado o vacío (error en `Email`) y con un JSON roto (400 controlado, no 500). La misma política se aplica al restablecer y al cambiar la contraseña (campo `NewPassword`).

  ```
  POST /auth/register
  { "fullName": "Ana Pérez", "email": "ana2@correo.com", "password": "abc12" }
  ```

- [x] **RF-CA-15** — Registrar un usuario y comprobar que nace inactivo y que el correo sale por la cola. Luego, **antes** de abrir el enlace, iniciar sesión con la contraseña correcta → 403 "La cuenta no está activa…".

  ```sql
  SELECT "Email", "ActivatedAt" FROM access_control."Users";   -- ActivatedAt vacío
  ```

  Ejecutar el Worker y confirmar que el correo llega de verdad.

- [x] **RF-CA-16** — Abrir el enlace de activación (`GET /auth/activate?userId=...&token=...`) → 200 y `ActivatedAt` con valor. **Después** de activar, iniciar sesión funciona. Abrir el mismo enlace una segunda vez → 400 y el estado no cambia. Un `userId` mal formado (`?userId=abc`) también da 400 controlado.

- [x] **RF-CA-17** — Pedir reenvío con un correo que no existe y con uno pendiente → misma respuesta 200 en ambos casos. Tras el reenvío, abrir el **enlace anterior** → 400 (quedó invalidado).

  ```
  POST /auth/resend-activation
  { "email": "no-existe@correo.com" }
  ```

### Sesión

Usar un usuario ya activado.

- [x] **RF-CA-03** — Iniciar sesión con credenciales correctas y con incorrectas.

  ```
  POST /auth/login
  { "email": "ana@correo.com", "password": "Clave1234" }
  ```

  Correctas → 200 con `accessToken` y `expiresAt`, y una fila nueva en `Sessions`. Con una contraseña incorrecta y con un correo inexistente → dos 401 con **exactamente el mismo cuerpo** ("Correo o contraseña incorrectos.").

  ```sql
  SELECT "Id", "UserId", "ExpiresAt", "RevokedAt" FROM access_control."Sessions";
  ```

- [x] **RF-CA-07** — Consultar el usuario autenticado.

  ```
  GET /auth/me
  ```

  Sin token → 401 con cuerpo `application/problem+json`. Con el token → 200 con `id`, `fullName`, `email` y `role` (nunca hashes ni tokens). Con un token alterado (cambiar un carácter) → 401.

- [x] **RF-CA-18** — Cerrar sesión y reutilizar la misma credencial.

  ```
  POST /auth/logout        → 204
  GET  /auth/me            → 401 (con el mismo token)
  ```

  La fila de `Sessions` queda con `RevokedAt`. Cerrar una sesión no afecta a otras sesiones del mismo usuario.

- [x] **RF-CA-19** — Fallar el inicio de sesión 5 veces seguidas y luego usar la contraseña correcta durante el bloqueo → 403 "La cuenta está bloqueada temporalmente…". Para no esperar 15 minutos, simular el vencimiento:

  ```sql
  UPDATE access_control."Users" SET "LockedUntil" = now() - interval '1 minute' WHERE "Email" = 'ana@correo.com';
  ```

  Con la contraseña correcta, el inicio de sesión funciona y `FailedLoginAttempts` queda en 0. Un acierto antes del quinto fallo también reinicia el contador.

- [x] **Extra: renovación** — `POST /auth/refresh` con un token todavía válido → 200 con un `accessToken` nuevo. El token anterior da 401 y su sesión queda con `RevokedAt`. No renueva un token ya vencido.

- [x] **El rol se lee de la base, no del token** — Con un usuario Estándar y su token, cambiar el rol en la base y llamar `GET /auth/me` con **el mismo token**: ya devuelve el rol nuevo. Devolver el rol al terminar.

  ```sql
  UPDATE access_control."Users" SET "Role" = 'Administrator' WHERE "Email" = 'ana@correo.com';
  ```

### Roles y administración de usuarios

**Cómo obtener el Administrador:** al arrancar, la API crea (o restaura) al Administrador con los valores de `Admin__Email` y `Admin__Password`. Para probar, iniciar sesión con esas credenciales:

```
POST /auth/login
{ "email": "<Admin__Email>", "password": "<Admin__Password>" }
```

**Rutas de administración.** Todas cuelgan de un único grupo, `/admin/users`, protegido por la política `RequireAdministrator`:

| Ruta                                                                            | Qué hace                                               |
| ------------------------------------------------------------------------------- | ------------------------------------------------------ |
| `GET /admin/users?page=1&pageSize=20`                                           | Lista paginada de usuarios con rol y estado            |
| `PATCH /admin/users/{id}/role` con `{ "role": "Administrator" }` o `"Standard"` | Cambia el rol de un usuario                            |
| `POST /admin/users/{id}/disable`                                                | Desactiva un usuario y revoca todas sus sesiones       |
| `POST /admin/users/{id}/enable`                                                 | Reactiva un usuario                                    |
| `POST /admin/users/{id}/force-password-reset`                                   | Fuerza el restablecimiento de la contraseña (RF-CA-13) |

Para las peticiones "construidas a mano" (sin pasar por Scalar) se puede usar PowerShell; ajusta el puerto al de tu API. Un 403 hace que `Invoke-RestMethod` lance una excepción: el código de estado aparece en el error.

```powershell
Invoke-RestMethod -Method Patch -Uri "http://localhost:5299/admin/users/<id>/role" `
  -Headers @{ Authorization = "Bearer <token de un usuario Estándar>" } `
  -ContentType "application/json" -Body '{"role":"Administrator"}'
```

- [x] **RF-CA-04** — Todo usuario tiene exactamente un rol. Registrar un usuario → nace con rol `Standard`. Cambiar su rol (RF-CA-08) lo reemplaza, no lo suma. En la base, la columna `Role` es única y solo admite `Standard` o `Administrator`.

  ```sql
  SELECT "Email", "Role" FROM access_control."Users";
  ```

- [x] **RF-CA-05** — Ubicar el punto único donde se declara el rol que exige cada operación de administración:
  - `src/Core/AccessControl/AccessControl.Infrastructure/Endpoints/AccessControlEndpoints.cs`, método `MapAdminUserEndpoints`: una sola llamada `RequireAuthorization(AccessControlPolicies.RequireAdministrator)` sobre el grupo `/admin/users`. Toda ruta añadida al grupo hereda la exigencia.
  - La política que exige el rol Administrador se define una sola vez en `AccessControl.Infrastructure/DependencyInjection.cs` (`AddAuthorizationBuilder`), y su nombre está en `AccessControl.Contracts/AccessControlPolicies.cs`.
  - Ningún handler vuelve a comprobar el rol.

- [x] **RF-CA-06** — Con sesión de Estándar, construir a mano una petición a una operación de Administrador (la de PowerShell de arriba, con el token de un Estándar) → 403 con "No tienes permiso para realizar esta operación." Lo mismo para `GET /admin/users`, `disable`, `enable` y `force-password-reset`. Sin token → 401.

- [x] **RF-CA-08** — Como Administrador, `PATCH /admin/users/{id}/role` con `{ "role": "Administrator" }` → 204, y ese usuario, con su **mismo token**, ve el rol nuevo en `GET /auth/me`. Un rol inválido (`"Superuser"`, vacío) → 400 con error en el campo `Role`. Como Estándar, intentar cambiar el propio rol o el de otro (a mano) → 403 en ambos casos.

- [x] **RF-CA-20** — Desactivar y reactivar:
  1. Un usuario inicia sesión y guarda su token. El Administrador ejecuta `POST /admin/users/{id}/disable` → 204.
  2. Ese token en `GET /auth/me` → 401. Todas sus filas de `Sessions` quedan con `RevokedAt`.
  3. Login con la contraseña correcta → 403 "La cuenta está desactivada…". Con contraseña incorrecta → 401 genérico.
  4. `POST /admin/users/{id}/enable` y login correcto → funciona. El token viejo sigue dando 401 (las sesiones antiguas no reviven).
  5. Reactivar a un usuario que nunca activó su correo → sigue sin poder entrar ("La cuenta no está activa").
  6. El Administrador intenta desactivarse a sí mismo → 403 "No puedes desactivar tu propia cuenta."
  7. `disable` de un id que no existe → 404.

  ```sql
  SELECT "Id", "RevokedAt" FROM access_control."Sessions" WHERE "UserId" = '<id>';
  ```

- [x] **RF-CA-21** — Como Administrador, `GET /admin/users` → 200 con `items`, `page`, `pageSize`, `totalItems`, `totalPages`, `hasPreviousPage` y `hasNextPage`. Cada item trae `id`, `fullName`, `email`, `role`, `status` (`Active`, `Disabled` o `PendingActivation`) y `createdAt`, y **nunca hashes ni tokens**. `?page=2&pageSize=1` devuelve una fila con los indicadores correctos; `?pageSize=500` o `?page=0` → 400 por campo. Como Estándar → 403.

- [ ] **Seed del Administrador** (RF-CA-04, 08):
  1. Arrancar sin `Admin:Email` o `Admin:Password` → la API no arranca, con un mensaje claro.
  2. Arrancar con ambas → fila del administrador con `Role = 'Administrator'` y `ActivatedAt` con valor; `PasswordHash` distinto de la contraseña.
  3. Reiniciar la API → no se crea otro usuario y `PasswordHash` no cambia.
  4. Degradarlo o desactivarlo y reiniciar → vuelve a ser Administrador y habilitado, con el mismo `PasswordHash`:

  ```sql
  UPDATE access_control."Users" SET "Role" = 'Standard', "DisabledAt" = now() WHERE "Email" = '<Admin__Email>';
  ```

### Contraseñas: recuperación, cambio y restablecimiento

**Rutas.**

| Ruta                                          | Sesión        | Cuerpo                                                    | Qué hace                                                                 |
| --------------------------------------------- | ------------- | --------------------------------------------------------- | ------------------------------------------------------------------------ |
| `POST /auth/forgot-password`                  | No            | `{ "email": "..." }`                                      | Pide un código de recuperación. Respuesta idéntica exista o no el correo |
| `POST /auth/reset-password`                   | No            | `{ "email": "...", "code": "...", "newPassword": "..." }` | Define una contraseña nueva con el código                                |
| `POST /auth/change-password`                  | Sí            | `{ "currentPassword": "...", "newPassword": "..." }`      | Cambia la propia contraseña; devuelve un token nuevo                     |
| `POST /admin/users/{id}/force-password-reset` | Administrador | —                                                         | Fuerza el restablecimiento y envía el código por la cola                 |

**Cómo obtener el código:** llega por correo después de ejecutar el Worker. También se puede leer del cuerpo del correo en la cola (consulta de arriba). En la base solo se guarda su hash, nunca el código.

- [x] **RF-CA-09** — Pedir recuperación con un correo inexistente y con uno existente (y activo) → misma respuesta 200 y mismo cuerpo en ambos casos. Solo el segundo encola un correo. Con un usuario sin activar o desactivado, la respuesta también es la misma y no se encola nada.

  ```
  POST /auth/forgot-password
  { "email": "no-existe@correo.com" }
  ```

  ```sql
  SELECT "To", "Status", "CreatedAt" FROM notifications."QueuedEmails" ORDER BY "CreatedAt" DESC;
  ```

- [x] **RF-CA-10** — El código es de un solo uso y vence a los 30 minutos:
  1. Usar el código (ver RF-CA-11) y volver a usarlo → 400 "El código de recuperación no es válido o ha vencido." y la contraseña no cambia.
  2. Pedir otro código y simular su vencimiento; al usarlo → 400 y la contraseña no cambia:

     ```sql
     UPDATE access_control."Users" SET "PasswordResetTokenExpiresAt" = now() - interval '1 minute' WHERE "Email" = 'ana@correo.com';
     ```

  3. Pedir dos códigos seguidos y usar el **primero** → 400 (el segundo lo reemplazó).
  4. Un código inventado, o un correo inexistente → el mismo 400 con el mismo mensaje.
  5. El correo sale por la cola: se ve `Pending` en `notifications."QueuedEmails"` hasta ejecutar el Worker.

- [x] **RF-CA-11** — Con un código válido, definir una contraseña nueva:

  ```
  POST /auth/reset-password
  { "email": "ana@correo.com", "code": "<código del correo>", "newPassword": "NuevaClave123" }
  ```

  200. Iniciar sesión con la contraseña **anterior** → 401; con la nueva → 200. En la base, `PasswordHash` cambió y `PasswordResetTokenUsedAt` tiene valor. Si la cuenta estaba bloqueada por intentos, queda desbloqueada (`FailedLoginAttempts` = 0).

- [x] **RF-CA-12** — Iniciar sesión y guardar el token. Restablecer la contraseña (RF-CA-11), forzarlo como Administrador (RF-CA-13) o cambiarla con sesión (RF-CA-22). Usar el token anterior en `GET /auth/me` → 401. Todas sus filas de `Sessions` quedan con `RevokedAt`.

- [x] **RF-CA-13** — Como Administrador, `POST /admin/users/{id}/force-password-reset` sobre un usuario con sesión abierta → 204. Ese usuario:
  1. Ya no puede iniciar sesión con su contraseña anterior → 401.
  2. Su token anterior → 401.
  3. Recibe por la cola un correo que indica que un administrador restableció su contraseña, con el código. Con ese código define la nueva (`POST /auth/reset-password`) y puede iniciar sesión.

  Como Estándar (a mano) → 403. Sobre un id inexistente → 404.

- [x] **RF-CA-22** — Con sesión, cambiar la propia contraseña:

  ```
  POST /auth/change-password
  { "currentPassword": "Clave1234", "newPassword": "OtraClave456" }
  ```

  1. Con la contraseña actual incorrecta → 400 "La contraseña actual es incorrecta." y nada cambia.
  2. Con la actual correcta y una nueva débil → 400 con error en `NewPassword` (RF-CA-14).
  3. Con la actual correcta y una nueva válida → 200 con un `accessToken` nuevo. Los tokens anteriores, incluido el usado en esta petición, dan 401 (RF-CA-12); el nuevo funciona. Iniciar sesión con la anterior → 401; con la nueva → 200.
  4. Sin token → 401.
  5. Un código de recuperación pedido antes del cambio deja de servir.

### Correo por cola

- [x] **RF-NOT-08** — Apagar el acceso al servidor SMTP (por ejemplo, poner un `Smtp:Port` inválido como `9999` en los secretos del Worker) y registrar un usuario → la API responde 201 y el correo queda `Pending`.

  ```sql
  SELECT "To", "Status", "SentAt" FROM notifications."QueuedEmails" ORDER BY "CreatedAt" DESC;
  ```

- [x] **RF-NOT-09 / RF-NOT-12** — Restaurar el puerto SMTP correcto y ejecutar el Worker → el log dice `Sent: 1` y el correo llega de verdad (`Status = Sent`, `SentAt` con valor). Ejecutarlo una segunda vez → `Sent: 0`, no se duplica ningún envío.

  ```bash
  dotnet run --project src/Host/Host.Worker
  ```

- [x] **RF-NOT-13** — Confirmar que las credenciales no aparecen en el repositorio ni en su historial.

  ```bash
  git grep -n -i "password" -- ':!*.md'
  git log -p | grep -i "smtp__password"
  ```

  Solo deben aparecer nombres de variables y valores de ejemplo (`change_me`).

### Estructura de la máquina de estados de negocio

Estado: **pendiente**.

- [ ] **RF-NEG-03 / RD-04** — Ubicar en el código el punto único donde están declarados los estados (entre 3 y 5) y las transiciones permitidas. _(ruta por definir)_
- [ ] **RF-NEG-04** — Confirmar la transición explícitamente prohibida documentada.
- [ ] **RF-NEG-05** — Confirmar el estado terminal documentado.
- [ ] Tabla de transiciones en [`../maquina-de-estados.md`](../maquina-de-estados.md).

### Persistencia

- [x] **RD-09** — Reiniciar la aplicación (y, por separado, `docker compose down` seguido de `docker compose up -d`) → los usuarios registrados y las sesiones siguen existiendo; un token emitido antes del reinicio sigue sirviendo.

  ```sql
  SELECT "Email", "ActivatedAt" FROM access_control."Users";
  ```

## Notas de diseño que afectan a la verificación

- **Tokens y códigos de un solo uso:** en la base solo se guarda su hash (SHA-256); el valor real viaja únicamente en el correo. Por eso no se pueden leer desde `access_control."Users"`, pero sí desde el cuerpo del correo en la cola.
- **Mismo rechazo para tokens y códigos inválidos:** uno inexistente, incorrecto, vencido o ya usado produce el mismo 400 con el mismo mensaje, igual que un usuario o correo inexistente.
- **Respuestas idénticas:** el reenvío de activación y la solicitud de recuperación responden igual exista o no el correo, y sea cual sea el estado de la cuenta. El tiempo de respuesta puede diferir, porque cuando corresponde se genera el código y se encola el correo.
- **Sesión:** cada inicio de sesión crea una fila en `Sessions`, y la credencial (JWT) lleva solo el usuario (`sub`) y la sesión (`sid`). En **cada petición protegida** el servidor comprueba en la base que la sesión siga vigente y que el usuario siga activo y no desactivado, y de ahí toma el rol. Por eso cerrar sesión, desactivar a un usuario, cambiarle el rol o cambiar la contraseña surten efecto de inmediato. El costo es una consulta por petición.
- **Credenciales incorrectas:** un correo inexistente y una contraseña incorrecta dan el mismo 401, y el servidor hace el mismo trabajo de hash en ambos casos para que el tiempo de respuesta no delate si el correo existe.
- **Orden del inicio de sesión:** primero se comprueba el bloqueo, luego la contraseña, y solo con la contraseña correcta se informa si la cuenta está desactivada o no activa. Así solo quien conoce la contraseña aprende ese estado.
- **Mensaje de bloqueo específico:** un usuario bloqueado ve un mensaje distinto al de credenciales incorrectas, lo que revela que ese correo existe. Es una decisión consciente a favor de la claridad para el usuario.
- **Bloqueo por intentos:** 5 fallos consecutivos bloquean la cuenta 15 minutos. Los intentos hechos durante el bloqueo se rechazan sin evaluarse y no suman. Al bloquear, el contador se reinicia. Restablecer la contraseña con código desbloquea la cuenta.
- **Renovación:** `POST /auth/refresh` revoca la sesión actual y crea una nueva en una sola transacción. No hay tope absoluto: una sesión renovada a tiempo no caduca.
- **Exigencia de rol:** se declara una sola vez, sobre el grupo `/admin/users`, y se evalúa en el servidor (RD-06). Un Estándar que construye la petición a mano recibe el mismo 403 que desde la interfaz. Sin sesión, 401; con sesión pero sin el rol, 403.
- **Dos ejes de estado de la cuenta:** `ActivatedAt` (confirmó su correo) y `DisabledAt` (un administrador la desactivó). Reactivar no salta la confirmación del correo.
- **Revocación de sesiones:** desactivar, restablecer, forzar el restablecimiento o cambiar la contraseña revocan **todas** las sesiones del usuario en la misma transacción que el cambio. El cambio con sesión devuelve un token nuevo para que el usuario siga conectado.
- **Restablecimiento forzado:** la contraseña anterior se reemplaza por una aleatoria que nadie conoce, así deja de servir aunque el usuario no lea el correo.
- **Cambio de contraseña con sesión:** una contraseña actual incorrecta devuelve 400 (no 401, para no confundirse con una sesión vencida) y no suma al bloqueo de inicio de sesión.
- **Administrador fijo:** el seed lo crea ya activo, o lo restaura (rol y estado) si fue degradado o desactivado, **sin tocar nunca su contraseña**. `Admin__Email` es una configuración de confianza: si coincide con un usuario ya registrado, ese usuario pasa a ser Administrador al arrancar.
- **Sin restricciones extra sobre los roles:** por decisión de diseño, un Administrador puede cambiar su propio rol o quitárselo a otro; solo está prohibido que se desactive a sí mismo. Si el sistema se quedara sin Administradores, el seed lo restaura en el siguiente arranque.
- **Correo por la cola:** las operaciones guardan sus cambios y después encolan el correo. Si el encolado fallara, el usuario puede pedir el reenvío o un código nuevo.
- **Posible reenvío del mismo correo:** si el Worker se cae justo entre enviar y marcar `Sent`, ese correo se reenvía en la siguiente ejecución.
- **Enlace de activación:** hoy abre un endpoint de la API (`GET /auth/activate`). Con un frontend, se cambia `App__ActivationUrl` y se usa `POST /auth/activate`. Un antivirus o cliente de correo que abra el enlace antes que el usuario podría consumir el token.
- **Endpoint temporal:** `POST /test/enqueue-email` existe solo para probar la cola y se elimina más adelante.
- **Credenciales SMTP para el calificador:** no están en el repositorio. Se entregan por separado (comentario de la entrega en Moodle) y se revocan al terminar la calificación.

## Pull requests de esta pieza

Cada uno con las cuatro secciones (Qué cambia, Por qué, Cómo probarlo, Qué NO incluye):

- [x] Registro y activación (RF-CA-01, 02, 14, 15, 16, 17)
- [x] Sesión (RF-CA-03, 07, 18, 19)
- [x] Administración de usuarios (RF-CA-04, 05, 06, 08, 20, 21)
- [x] Recuperación de contraseña (RF-CA-09 a 13, 22)
