# Wheelby

Marketplace de alquiler de vehículos entre particulares. Backend en C# (.NET 10), monolito modular con Clean Architecture, CQRS y DDD, sobre PostgreSQL.

Programación III · ITLA · 2026-C-3 · Práctica 4

**Todo el proyecto corre en contenedores**: PostgreSQL, la API y el Worker de correos se levantan con un solo comando, `docker compose up --build`, desde la raíz del repositorio. No hace falta instalar .NET ni PostgreSQL. Para comprobar cada criterio de aceptación, ver [Cómo verificar los criterios](#7-cómo-verificar-los-criterios).

---

## Índice

1. [Requisitos](#1-requisitos)
2. [Contenedor](#2-contenedor)
3. [Correos de activación y recuperación](#3-correos-de-activación-y-recuperación)
4. [Los seis frentes](#4-los-seis-frentes)
5. [Manejador de documentos](#5-manejador-de-documentos)
6. [API externa](#6-api-externa)
7. [Cómo verificar los criterios](#7-cómo-verificar-los-criterios)
   - [Pruebas](#pruebas)
   - [Defectos](#defectos)
8. [Variables de configuración](#8-variables-de-configuración)
9. [Ver los datos en PostgreSQL](#9-ver-los-datos-en-postgresql)
10. [Problemas frecuentes](#10-problemas-frecuentes)
11. [Estructura del repositorio](#11-estructura-del-repositorio)

---

## 1. Requisitos

| Herramienta    | Versión               | Cómo comprobarla                            |
| -------------- | --------------------- | ------------------------------------------- |
| Git            | cualquiera reciente   | `git --version`                             |
| Docker Desktop | con Docker Compose v2 | `docker version` y `docker compose version` |

- **Solo Docker.** No hace falta instalar .NET, PostgreSQL ni `dotnet-ef`: la API y el Worker se compilan dentro de la imagen y la API crea las tablas sola al arrancar.
- **Docker Desktop debe estar abierto y en estado "running"** antes de los pasos siguientes.
- Opcional: credenciales SMTP para que los correos lleguen de verdad (sección 2.3) y una clave de ExchangeRate-API para convertir precios (sección 6).

Los comandos se muestran para **PowerShell (Windows)** y, donde cambian, para **bash** (Git Bash, macOS o Linux).

---

## 2. Contenedor

### 2.1 Obtener el código

```bash
git clone <URL del repositorio>
cd <carpeta del repositorio>
git checkout practica-4
```

**Todos los comandos de este documento se ejecutan desde la raíz del repositorio**, donde están `docker-compose.yml`, `Dockerfile` y `.env.example`.

### 2.2 Crear el archivo `.env`

`.env` es la **única fuente de configuración**: Docker Compose lo lee y se lo entrega a la API y al Worker. Se crea copiando la plantilla:

```powershell
Copy-Item .env.example .env
```

```bash
cp .env.example .env
```

Con los valores de ejemplo **la aplicación arranca**. `.env` está en `.gitignore` y en `.dockerignore`: nunca se sube al repositorio ni entra en la imagen.

### 2.3 Credenciales SMTP (opcional)

Para que los correos de activación y de recuperación lleguen de verdad, abre `.env` y reemplaza las líneas `Smtp__*` por las credenciales que van **en el comentario de la entrega en Moodle**. Sin ellas todo funciona igual y los correos se leen de la base (sección 3).

### 2.4 Levantar el proyecto

```bash
docker compose up --build
```

La primera vez descarga las imágenes y compila (unos minutos). Arrancan tres servicios:

| Servicio   | Qué hace                                                                 | Puerto publicado |
| ---------- | ------------------------------------------------------------------------ | ---------------- |
| `postgres` | PostgreSQL 17. Queda `healthy` antes de que arranque la API              | ninguno          |
| `api`      | La API. Aplica las migraciones de todos los módulos y crea el Administrador | **8080**         |
| `worker`   | Envía los correos pendientes de la cola cada 10 segundos, sin parar      | ninguno          |

**Solo la API publica un puerto.** Compruébalo en otra terminal con `docker compose ps`.

- API: `http://localhost:8080`
- Documentación interactiva (Scalar): **`http://localhost:8080/scalar/v1`**

Si falta una variable obligatoria en `.env`, `docker compose up` se detiene antes de crear los contenedores y dice cuál falta (por ejemplo `Falta Admin__Email en .env`).

### 2.5 Detener el proyecto

- `Ctrl+C` en la terminal donde corre, o `docker compose down` desde otra. Los datos **se conservan**.
- `docker compose down -v` **borra también los datos** (la base y los documentos subidos). Úsalo solo para empezar de cero.

### 2.6 Comprobar la persistencia

Los datos viven en dos volúmenes con nombre: `wheelby-pgdata` (la base) y `wheelby-documents` (los archivos subidos).

1. Regístrate e inicia sesión (frentes 1 y 2 de la sección 4), y sube un documento (sección 5).
2. Detén todo con `docker compose down` (**sin** `-v`).
3. Vuelve a levantarlo con `docker compose up`.
4. Inicia sesión con el mismo usuario → **200**.
5. `GET /documents` lista el documento y `GET /documents/{id}/content` lo descarga con el mismo contenido.

---

## 3. Correos de activación y recuperación

Los correos no se envían dentro de la petición: quedan en una cola de la base con estado `Pending` y el servicio `worker` los envía en su siguiente vuelta (cada `Worker__IntervalSeconds`, 10 segundos por defecto). Ya no hace falta ejecutar nada a mano.

- **Vía principal:** con las credenciales SMTP reales en `.env` (sección 2.3), el correo llega en unos segundos. Revisa la carpeta de spam.
- **Respaldo sin SMTP:** con los valores de ejemplo el envío falla, el correo sigue `Pending` y el Worker lo reintenta. El enlace de activación y el código de recuperación se leen directamente de la cola:

  En bash (Git Bash, macOS o Linux):

  ```bash
  docker compose exec postgres psql -U wheelby -d wheelby -c "SELECT \"To\", \"Status\", \"Body\" FROM notifications.\"QueuedEmails\" ORDER BY \"CreatedAt\" DESC LIMIT 5;"
  ```

  En PowerShell, abre la consola de PostgreSQL y escribe la consulta:

  ```powershell
  docker compose exec postgres psql -U wheelby -d wheelby
  ```

  ```sql
  SELECT "To", "Status", "Body" FROM notifications."QueuedEmails" ORDER BY "CreatedAt" DESC LIMIT 5;
  ```

  Sal con `\q`. Si cambiaste `POSTGRES_USER` o `POSTGRES_DB` en `.env`, usa esos valores en `-U` y `-d`.

- Para ver lo que hace el Worker: `docker compose logs -f worker`. Cada vez que envía algo escribe `Pending emails processed. Sent: N. Failed: M.`

---

## 4. Los seis frentes

Con el proyecto corriendo (sección 2), en Scalar (`http://localhost:8080/scalar/v1`). Para usar un token en Scalar: copia el `accessToken` de la respuesta de `POST /auth/login` y pégalo en el botón de autorización (esquema Bearer).

Los frentes 1 a 4 son de Control de acceso, el 5 de Gestión de permisos y el 6 del negocio Wheelby. El detalle de cada funcionalidad del negocio está en [docs/wheelby/README.md](docs/wheelby/README.md).

### Roles

Cada usuario tiene **exactamente un rol**. En las peticiones, en las respuestas y en la base de datos los roles se escriben así, en inglés:

| Rol | Valor exacto | Quién lo tiene |
|---|---|---|
| Estándar | `Standard` | Todo usuario que se registra con `POST /auth/register` |
| Administrador | `Administrator` | El Administrador fijo (`Admin__Email` / `Admin__Password` del `.env`), que la API crea al arrancar, y quien reciba ese rol con `PATCH /admin/users/{id}/role` |

### Rutas y formato de los cuerpos

Todos los cuerpos son JSON (`Content-Type: application/json`) y los nombres de los campos van en camelCase. Los errores se devuelven como `application/problem+json`: **400** datos inválidos, **401** sin sesión, **403** sin permiso, **404** no existe, **409** estado que no admite la operación y **503** un servicio externo no está disponible (sección 6). Una ruta con sesión exige la cabecera `Authorization: Bearer <accessToken>`.

| Ruta | Sesión | Cuerpo de la petición | Respuesta correcta |
|---|---|---|---|
| `POST /auth/register` | No | `{ "fullName": "Ana Pérez", "email": "ana@correo.com", "password": "Clave1234" }` | 201 `{ "message": "..." }` |
| `GET /auth/activate?userId=<id>&token=<token>` | No | — (es el enlace del correo) | 200 `{ "message": "Cuenta activada..." }` |
| `POST /auth/activate` | No | `{ "userId": "<id>", "token": "<token>" }` | 200 `{ "message": "Cuenta activada..." }` |
| `POST /auth/resend-activation` | No | `{ "email": "ana@correo.com" }` | 200 `{ "message": "..." }` |
| `POST /auth/login` | No | `{ "email": "ana@correo.com", "password": "Clave1234" }` | 200 `{ "accessToken": "...", "expiresAt": "..." }` |
| `GET /auth/me` | Sí | — | 200 `{ "id": "...", "fullName": "...", "email": "...", "role": "Standard" }` |
| `POST /auth/logout` | Sí | — | 204 |
| `POST /auth/refresh` | Sí | — | 200 `{ "accessToken": "...", "expiresAt": "..." }` |
| `POST /auth/forgot-password` | No | `{ "email": "ana@correo.com" }` | 200 `{ "message": "..." }` |
| `POST /auth/reset-password` | No | `{ "email": "ana@correo.com", "code": "<código del correo>", "newPassword": "Nueva1234" }` | 200 `{ "message": "Contraseña restablecida..." }` |
| `POST /auth/change-password` | Sí | `{ "currentPassword": "Clave1234", "newPassword": "Nueva1234" }` | 200 `{ "accessToken": "...", "expiresAt": "..." }` |
| `GET /admin/users?page=1&pageSize=20` | `Administrator` | — | 200, lista paginada |
| `PATCH /admin/users/{id}/role` | `Administrator` | `{ "role": "Administrator" }` o `{ "role": "Standard" }` | 204 |
| `POST /admin/users/{id}/disable` | `Administrator` | — | 204 |
| `POST /admin/users/{id}/enable` | `Administrator` | — | 204 |
| `POST /admin/users/{id}/force-password-reset` | `Administrator` | — | 204 |

Contraseñas: al menos 8 caracteres, con letras y números. En las rutas `/admin/...`: sin sesión, **401**; con sesión pero sin el rol `Administrator`, **403**.

Si no quieres esperar el correo, el enlace de activación y el código de recuperación también se leen de la cola (sección 3).

### Frente 1: registro con activación por correo

1. `POST /auth/register` con un correo real:

   ```json
   {
     "fullName": "Tu Nombre",
     "email": "<tu correo real>",
     "password": "Clave1234"
   }
   ```

   → **201**.
2. `POST /auth/login` con ese correo y esa contraseña, **antes** de activar → **403** "La cuenta no está activa…".
3. En unos segundos el Worker envía el correo «Activa tu cuenta de Wheelby» (revisa spam). Sin SMTP real, lee el enlace de la cola (sección 3).
4. Abrir el enlace del correo → **200** "Cuenta activada. Ya puedes iniciar sesión."
5. Abrir el mismo enlace otra vez → **400** (el enlace es de un solo uso).

### Frente 2: inicio y cierre de sesión

1. `POST /auth/login` con el usuario activado:

   ```json
   { "email": "<tu correo>", "password": "Clave1234" }
   ```

   → **200** con `accessToken`. Pega el token en Scalar.
2. `GET /auth/me` → **200** con `id`, `fullName`, `email` y `role` = `Standard`.
3. `POST /auth/logout` → **204**.
4. `GET /auth/me` con el **mismo token** → **401**: la sesión quedó cerrada.

### Frente 3: rechazo a un Estándar en una operación de Administrador

1. Inicia sesión otra vez con el usuario Estándar y pega su token.
2. `GET /admin/users` → **403** "No tienes permiso para realizar esta operación."

   La misma petición construida a mano, fuera de Scalar:

   ```powershell
   Invoke-RestMethod -Method Get -Uri "http://localhost:8080/admin/users" `
     -Headers @{ Authorization = "Bearer <token del usuario Estándar>" }
   ```

   `Invoke-RestMethod` lanza una excepción con el 403: es el resultado esperado.
3. Sin token → **401**.
4. Comparación: `POST /auth/login` con `Admin__Email` y `Admin__Password` del `.env` (rol `Administrator`), pega ese token y repite `GET /admin/users` → **200**.

### Frente 4: recuperación de contraseña

1. `POST /auth/forgot-password`:

   ```json
   { "email": "<tu correo>" }
   ```

   → **200**.
2. En unos segundos el Worker envía el correo «Recupera tu contraseña de Wheelby» con un código. Sin SMTP real, lee el código de la cola (sección 3).
3. `POST /auth/reset-password`:

   ```json
   { "email": "<tu correo>", "code": "<código del correo>", "newPassword": "Nueva1234" }
   ```

   → **200** "Contraseña restablecida. Ya puedes iniciar sesión."
4. `POST /auth/login` con la contraseña **anterior** (`Clave1234`) → **401**. Con la **nueva** (`Nueva1234`) → **200**.
5. Repetir el paso 3 con el mismo código → **400** (el código es de un solo uso).

### Frente 5: Gestión de permisos

Se necesitan dos tokens: el **token de Estándar** (`POST /auth/login` con el usuario de los frentes 1 a 4) y el **token de Administrador** (`POST /auth/login` con `Admin__Email` y `Admin__Password` del `.env`). En cada paso se indica cuál pegar en Scalar. Detalle de cada criterio y consultas SQL en [docs/gestion-permisos/README.md](docs/gestion-permisos/README.md).

| Ruta | Sesión | Cuerpo de la petición | Respuesta correcta |
|---|---|---|---|
| `POST /permissions/requests` | Sí | `{ "permissionCode": "core.demo-action" }` | 201 con el `id` de la solicitud |
| `GET /permissions/requests/mine` | Sí | — | 200, solo las solicitudes del usuario |
| `POST /permissions/demo-action` | Sí, con la concesión | — | 200 con permiso, 403 sin permiso |
| `GET /admin/permissions/requests?page=1&pageSize=20` | `Administrator` | — | 200, solicitudes pendientes de todos |
| `POST /admin/permissions/requests/{id}/approve` | `Administrator` | — | 204 |
| `POST /admin/permissions/requests/{id}/reject` | `Administrator` | `{ "reason": "..." }` | 204 |

**Crear y aprobar una solicitud**

1. Con el **token de Estándar**, `POST /permissions/demo-action` → **403**: todavía no tiene el permiso.
2. Con el **token de Estándar**, `POST /permissions/requests`:

   ```json
   { "permissionCode": "core.demo-action" }
   ```

   → **201** con el `id` de la solicitud (anótalo como `<id 1>`). `GET /permissions/requests/mine` la muestra en estado `Pending`.
3. Con el **token de Administrador**, `GET /admin/permissions/requests?page=1&pageSize=20` → **200**, aparece `<id 1>`.
4. Con el **token de Administrador**, `POST /admin/permissions/requests/<id 1>/approve` → **204**. La solicitud queda `Applied` con el aprobador y la fecha.
5. Con el **token de Estándar**, `POST /permissions/demo-action` → **200** `{ "message": "Acción de demostración ejecutada." }`.

**Aprobar dos veces da 409 y el estado no cambia**

6. Con el **token de Administrador**, repetir `POST /admin/permissions/requests/<id 1>/approve` → **409**. Con el **token de Estándar**, `GET /permissions/requests/mine` sigue mostrando `<id 1>` en `Applied`.

**Rechazar otra solicitud con motivo**

7. Con el **token de Estándar**, `POST /permissions/requests`:

   ```json
   { "permissionCode": "wheelby.publish-vehicle" }
   ```

   → **201** (anótalo como `<id 2>`).
8. Con el **token de Administrador**, `POST /admin/permissions/requests/<id 2>/reject` **sin motivo**:

   ```json
   { "reason": "" }
   ```

   → **400** "El motivo del rechazo es obligatorio." y la solicitud **sigue `Pending`**.
9. Con el **token de Administrador**, `POST /admin/permissions/requests/<id 2>/reject` con motivo:

   ```json
   { "reason": "No corresponde al perfil del usuario" }
   ```

   → **204**. `GET /permissions/requests/mine` (token de Estándar) muestra `<id 2>` en `Rejected`.

**Transición fuera de la tabla: aprobar una rechazada da 409 y el estado no cambia**

10. Con el **token de Administrador**, `POST /admin/permissions/requests/<id 2>/approve` → **409**. `<id 2>` sigue en `Rejected`.

Comprobación en la base (opcional):

```sql
SELECT "Id", "PermissionCode", "Status", "ApproverId", "ResolvedAt", "RejectionReason"
FROM permissions."PermissionRequests"
ORDER BY "CreatedAt" DESC;
```

### Frente 6: el negocio, una operación válida y la transición prohibida

Hacen falta tres tokens: **anfitrión** (un Estándar), **inquilino** (otro Estándar, registrado y activado como en el frente 1) y **Administrador**.

**Operación válida: publicar, reservar y confirmar**

1. Con el **token de anfitrión**, `POST /permissions/requests`:

   ```json
   { "permissionCode": "wheelby.publish-vehicle" }
   ```

   → **201** con el `id` de la solicitud. Con el **token de Administrador**, `POST /admin/permissions/requests/<id>/approve` → **204**.
2. Con el **token de anfitrión**, `POST /vehicles`:

   ```json
   { "brand": "Toyota", "model": "Corolla", "year": 2020, "plate": "ABC-123", "pricePerDay": 2500 }
   ```

   → **201** con el `id` del vehículo (`<vehículo>`). El precio está en pesos dominicanos (DOP).
3. Con el **token de inquilino**, `POST /reservations`:

   ```json
   { "vehicleId": "<vehículo>", "start": "2030-01-10T10:00:00Z", "end": "2030-01-12T10:00:00Z" }
   ```

   → **201** con el `id` de la reserva (`<reserva>`), en estado `Pending`.
4. Con el **token de anfitrión**, `POST /reservations/<reserva>/confirm` → **204**. La reserva pasa a `Confirmed`.

**Transición prohibida: cancelar un alquiler en curso (`InProgress → Cancelled`)**

5. Para iniciar el alquiler sin esperar a 2030, mueve las fechas de la reserva alrededor del momento actual (en bash; en PowerShell abre `psql` como en la sección 3 y escribe la consulta):

   ```bash
   docker compose exec postgres psql -U wheelby -d wheelby -c "UPDATE wheelby.\"Reservations\" SET \"StartDate\" = now() - interval '1 hour', \"EndDate\" = now() + interval '1 day' WHERE \"Id\" = '<reserva>';"
   ```

6. Con el **token de anfitrión**, `POST /reservations/<reserva>/start` → **204**. La reserva pasa a `InProgress`.
7. Con el **token de inquilino**, `POST /reservations/<reserva>/cancel`:

   ```json
   { "reason": "Cambio de planes" }
   ```

   → **409** "Transición InProgress → Cancelled prohibida: Un alquiler en curso no se cancela; debe completarse."
8. Con el **token de inquilino**, `GET /reservations/mine?as=renter&page=1&pageSize=20` → la reserva sigue en `InProgress`: el estado no cambió.

---

## 5. Manejador de documentos

Cualquier usuario con sesión (Estándar o Administrador) sube, lista, descarga y elimina documentos. Un Estándar solo ve y maneja **los suyos**; el Administrador, los de todos. Los archivos se guardan en el volumen `wheelby-documents` y sus datos (nombre, tipo, tamaño, fecha y quién lo subió) en la tabla `documents."Documents"`.

| Ruta | Respuesta correcta |
|---|---|
| `POST /documents` (formulario con el campo `file`) | **201** `{ "id": "..." }` |
| `GET /documents?page=1&pageSize=20` | **200**, lista paginada sin los eliminados |
| `GET /documents/{id}/content` | **200**, el archivo como adjunto (`Content-Disposition: attachment`) |
| `DELETE /documents/{id}` | **204** |

### 5.1 Subir

- **Desde Scalar:** abre `POST /documents`, pega tu token, elige un archivo en el campo `file` y envía → **201** con el `id`.
- **Con curl** (en PowerShell usa `curl.exe` para no llamar al alias de `Invoke-WebRequest`):

  ```bash
  curl -X POST http://localhost:8080/documents -H "Authorization: Bearer <token>" -F "file=@contrato.pdf"
  ```

Rechazos:

- Sin el campo `file` → **400** "Debes adjuntar un archivo en el campo file."
- Archivo vacío → **400** "El archivo está vacío."
- Más grande que `Documents__MaxFileSizeBytes` (10 MB por defecto) → **400** con el límite en MB.

### 5.2 Listar, descargar y eliminar

1. `GET /documents?page=1&pageSize=20` → **200**. Cada elemento trae `id`, `ownerId`, `fileName`, `contentType`, `sizeBytes` y `uploadedAt`.
2. `GET /documents/<id>/content` → **200** y se descarga el archivo con su nombre original. Con curl: `curl -OJ -H "Authorization: Bearer <token>" http://localhost:8080/documents/<id>/content`.
3. `DELETE /documents/<id>` → **204**. El documento ya no se lista y descargarlo da **404**. Eliminarlo otra vez → **404**.

### 5.3 Otro usuario no puede descargar ni eliminar

1. El usuario A sube un documento (`<id>`).
2. El usuario B (otro Estándar) pide `GET /documents/<id>/content` → **403** "No tienes permiso para acceder a este documento."
3. El usuario B pide `DELETE /documents/<id>` → **403**, y el documento sigue intacto.
4. El Administrador sí puede descargarlo → **200**.

### 5.4 La eliminación es lógica

Después de eliminar, la fila **sigue** en la base, con quién y cuándo la eliminó, y el archivo sigue en el volumen:

```sql
SELECT "Id", "FileName", "OwnerId", "UploadedAt", "DeletedAt", "DeletedBy" FROM documents."Documents" ORDER BY "UploadedAt" DESC;
```

`DeletedAt` y `DeletedBy` tienen valor. (Cómo abrir `psql`: sección 3.)

---

## 6. API externa

| | |
|---|---|
| **API** | [ExchangeRate-API](https://www.exchangerate-api.com), endpoint de par: `GET https://v6.exchangerate-api.com/v6/pair/DOP/<moneda>` |
| **Operación que la usa** | `GET /vehicles/available?...&currency=USD` (búsqueda de vehículos) |
| **Variable** | `ExchangeRate__ApiKey` en `.env` |

Los precios publicados están en **pesos dominicanos (DOP)**. Si la búsqueda trae `currency` (3 letras, por ejemplo `USD` o `EUR`), cada vehículo agrega `convertedPricePerDay` (redondeado a 2 decimales), `convertedCurrency` y `exchangeRate`. Sin `currency`, o con `DOP`, la búsqueda no llama a la API externa.

**Cómo obtener una clave gratis:** regístrate en [exchangerate-api.com](https://www.exchangerate-api.com) con tu correo; el panel muestra la clave (plan gratis: 1500 llamadas al mes). Pégala en `.env` como `ExchangeRate__ApiKey=<tu clave>` y reinicia con `docker compose up`. La clave viaja en la cabecera `Authorization: Bearer` y nunca aparece en la URL ni en el log. Cada tasa se guarda en memoria 60 minutos para no gastar la cuota.

### 6.1 Sin clave o con la API caída

El resto del proyecto funciona igual. Solo la búsqueda **con** `currency` responde **503** con un mensaje controlado:

| Situación | Respuesta |
|---|---|
| `ExchangeRate__ApiKey` vacía | **503** "La conversión de moneda no está disponible: falta configurar ExchangeRate__ApiKey." |
| Clave de ejemplo o inválida | **503** "El servicio de tasas de cambio rechazó la clave configurada." |
| La API no responde o responde algo inválido | **503** "El servicio de tasas de cambio no respondió. Intenta más tarde." |
| Se agotó la cuota | **503** "Se agotó la cuota del servicio de tasas de cambio. Intenta más tarde." |
| Moneda que el proveedor no conoce (por ejemplo `XYZ`) | **400** "La moneda XYZ no está soportada." |
| `currency` que no son 3 letras (`US`, `12$`) | **400** |

Para comprobarlo con la clave de ejemplo de `.env.example`, con el token de un Estándar:

```
GET /vehicles/available?from=2030-01-10T10:00:00Z&to=2030-01-12T10:00:00Z&currency=USD
```

→ **503**. La misma búsqueda **sin** `currency` → **200**.

### 6.2 Ejemplo real de respuesta

Con una clave real en `.env`:

```
GET /vehicles/available?from=2030-01-10T10:00:00Z&to=2030-01-12T10:00:00Z&currency=USD
```

<!-- PEGAR RESPUESTA REAL -->

---

## 7. Cómo verificar los criterios

- **Control de acceso y correo por cola** (RF-CA-01 a 22, RF-NOT-08, 09, 12, 13): pasos exactos, cuerpos de petición y consultas SQL para cada criterio en [docs/control-acceso/README.md](docs/control-acceso/README.md).
- **Máquina de estados del negocio** (RF-NEG-03, 04, 05, RD-04): tabla de transiciones y ubicación en el código en [docs/maquina-de-estados.md](docs/maquina-de-estados.md).
- **Gestión de permisos** (RF-GP-01 a 07, RD-09): pasos exactos, cuerpos de petición y consultas SQL para cada criterio en [docs/gestion-permisos/README.md](docs/gestion-permisos/README.md).
- **Módulo de negocio Wheelby** (RF-NEG-01 a 06, RD-03, RD-07, RD-08, RD-09): pasos exactos, cuerpos de petición y consultas SQL para cada criterio en [docs/wheelby/README.md](docs/wheelby/README.md).
- **Manejador de documentos** (RF-DOC-01 a 05): sección 5 de este documento.
- **API externa** (RD-07, RD-08, RD-10): sección 6 de este documento.

Atajos útiles:

- **Simular SMTP caído (RF-NOT-08):** pon `Smtp__Port=9999` en `.env` y recrea el Worker con `docker compose up -d worker`. Registra un usuario: la API responde 201 y el correo queda `Pending`; `docker compose logs worker` muestra `Failed: 1` en cada vuelta. Restaura el puerto y repite `docker compose up -d worker`: el correo sale (`Sent: 1`) y no se duplica.
- **Peticiones construidas a mano (RF-CA-06),** fuera de Scalar:

  ```powershell
  Invoke-RestMethod -Method Get -Uri "http://localhost:8080/admin/users" `
    -Headers @{ Authorization = "Bearer <token de un usuario Estándar>" }
  ```

  Un 403 hace que `Invoke-RestMethod` lance una excepción con el código de estado: es el resultado esperado.

- **Persistencia (RD-09):** sección 2.6.
- **Credenciales fuera del repositorio (RD-10, RF-NOT-13):** desde la raíz del repositorio,

  ```bash
  git grep -n -i "password" -- ':!*.md'
  git log --all --oneline -- .env '**/.env'
  ```

  El primero solo muestra nombres de variables y valores de ejemplo (`change_me`, `wheelby_local_123`). El segundo no devuelve nada: `.env` nunca se subió.

---

## Pruebas

Las pruebas del repositorio se ejecutan con **un solo comando**, desde un clon limpio:

```bash
cd src/backend
dotnet test
```

`dotnet test` compila la solución completa (`Wheelby.slnx`) y ejecuta **165 pruebas** en dos proyectos (una prueba con varios casos cuenta una vez por caso):

| Proyecto | Carpeta | Pruebas | Resultado esperado |
|---|---|---|---|
| `Permissions.Test` | `src/backend/src/Core/Permissions/Permissions.Test/` | 71 | `Failed: 0, Passed: 71` |
| `Wheelby.Test` | `src/backend/src/Domain/Wheelby/Wheelby.Test/` | 94 | `Failed: 0, Passed: 94` |

**No necesitan Docker ni PostgreSQL** y no levantan la API: la base se sustituye por EF InMemory. La primera vez, `dotnet test` restaura los paquetes automáticamente; no hace falta instalar `dotnet-ef`.

Todas usan fechas y reloj fijos, siguen Arrange/Act/Assert, verifican un solo comportamiento, son independientes entre sí (cada una crea sus dobles y, con EF InMemory, una base con nombre único) y llevan el ID del requisito al inicio del nombre (`RF_GP_0X_...`, `RF_NEG_0X_...`).

### Wheelby.Test (Práctica 3)

Abreviaturas: **D** = `Domain/`, **A** = `Application/`, **I** = `Infrastructure/`.

- **Máquina de estados de la reserva (D, `ReservationStateMachineTests.cs`)**, con `Reservation` y `ReservationStateMachine` reales:
  - una prueba por cada una de las **5 filas permitidas** de [docs/maquina-de-estados.md](docs/maquina-de-estados.md) (estado resultante y, al cancelar el anfitrión, el motivo guardado);
  - cada fila con **actor incorrecto** y con **guarda no cumplida** (por ejemplo `RF_NEG_03_Pending_a_Confirmed_despues_de_StartDate_guarda_no_cumplida_se_rechaza`): rechazo y el estado no cambia;
  - la **prohibida explícita** `InProgress → Cancelled` con cada actor (`RF_NEG_04_InProgress_a_Cancelled_con_Host_es_prohibida_explicitamente`, `..._con_Renter_...`);
  - los **terminales** `Completed` y `Cancelled` hacia cada estado y con cada actor (`RF_NEG_05_desde_estado_terminal_cualquier_transicion_se_rechaza`);
  - la **matriz de pares fuera de la tabla**, escrita a mano en la prueba y nunca leída de `AllowedTransitions` (`RF_NEG_04_par_fuera_de_la_tabla_se_rechaza_y_estado_no_cambia`);
  - la **prueba estructural de la tabla** (`RF_NEG_03_AllowedTransitions_contiene_exactamente_las_cinco_filas_de_la_tabla`, `RF_NEG_05_TerminalStates_es_exactamente_Completed_y_Cancelled`);
  - la creación de la reserva (RD-07).
- **Casos de uso (A)**, handlers reales con dobles hechos a mano de sus puertos (reloj fijo, usuario actual mutable, repositorios en memoria, unidad de trabajo que cuenta los guardados y un `IPermissionChecker` falso solo para estas pruebas): publicar (F1, incluido `RF_NEG_02_F1_sin_permiso_lanza_403_y_no_guarda_nada`), buscar (F2), solicitar (F3), confirmar y rechazar (F4), el ciclo del alquiler con su precedencia de errores (F5) y los listados (F6).
- **Rutas (I, `EndpointRouteTests.cs`)**: construye la aplicación **sin arrancarla** y comprueba que las rutas de `/vehicles` y `/reservations` exigen `RequireStandard`, las de `/admin/reservations` exigen `RequireAdministrator` y ninguna queda sin autorización (RF-NEG-06).
- **Integración con Gestión de permisos (I, `PermissionsIntegrationTests.cs`)**: usa los módulos Permissions y Wheelby **reales** con su cableado de producción (`AddPermissions` y `AddWheelby`, MediatR, validadores, repositorios y el catálogo). Solo se sustituyen la base (EF InMemory), el usuario actual y el reloj; **no** hay `IPermissionChecker` falso. Escenarios:
  - `RF_NEG_06_integracion_publicar_sin_aprobacion_es_rechazado_y_despues_de_aprobar_se_permite`: un Estándar no puede publicar (403), tampoco tras solicitar el permiso; un Administrador aprueba y entonces publica, queda un vehículo suyo y la solicitud queda `Applied`.
  - `RF_NEG_06_integracion_si_el_administrador_rechaza_publicar_sigue_rechazado`.
  - Catálogo: solicitar `wheelby.publish-vehicle` es aceptado.

### Permissions.Test (Práctica 2)

- **Dominio:** reglas de `PermissionRequest` y la matriz completa de transiciones de `PermissionRequestStateMachine` (32 combinaciones de origen, destino y actor, más la tabla declarada exacta).
- **Casos de uso:** los handlers de crear, listar, aprobar y rechazar se prueban con dobles de sus puertos: reloj fijo, usuario actual fijo, catálogo fijo, repositorios en memoria y unidad de trabajo que cuenta los guardados.
- **Infraestructura:** `PermissionRequestReadStore` y `PermissionChecker` se prueban sobre `PermissionsDbContext` con EF InMemory, con un nombre de base único por prueba.

#### Pruebas por requisito de Gestión de permisos

Carpeta: `src/backend/src/Core/Permissions/Permissions.Test/`. Abreviaturas: **D** = `Domain/`, **A** = `Application/`, **I** = `Infrastructure/`.

| Requisito | Lo que se verifica | Pruebas |
|---|---|---|
| **RF-GP-01** | La solicitud nace en estado Pendiente | `RF_GP_01_Crear_solicitud_nace_en_Pending` (D); `RF_GP_01_Crear_guarda_una_vez_y_devuelve_el_id`, `RF_GP_01_Crear_con_codigo_inexistente_es_rechazado`, `RF_GP_01_Crear_con_solicitud_pendiente_duplicada_es_rechazado`, `RF_GP_01_Crear_con_concesion_existente_es_rechazado`, `RF_GP_01_Crear_tras_una_rechazada_es_permitido` (A) |
| **RF-GP-02** | Un Estándar no ve las solicitudes de otros usuarios | `RF_GP_02_Listar_mis_solicitudes_usa_el_usuario_actual` (A); `RF_GP_02_Las_solicitudes_de_una_persona_excluyen_a_los_demas`, `RF_GP_02_Las_pendientes_excluyen_las_resueltas` (I) |
| **RF-GP-03** | Queda registrado quién aprobó y cuándo | `RF_GP_03_Aprobar_registra_el_aprobador_y_la_fecha_del_reloj` (D y A) |
| **RF-GP-04** | Sin motivo, ausente o vacío, el rechazo no se completa y el estado no cambia | `RF_GP_04_Rechazar_con_motivo_ausente_no_cambia_la_solicitud` (D), `RF_GP_04_Deja_la_solicitud_y_los_datos_sin_cambios_sin_motivo` (A), ambas con los casos `null`, `""` y `"   "`; `RF_GP_04_Rechazar_con_motivo_guarda_motivo_recortado` (D), `RF_GP_04_Rechazar_guarda_un_motivo_recortado` (A) |
| **RF-GP-05** | Tras aprobar, la solicitud llega a Aplicada y el permiso queda efectivo | `RF_GP_05_Aplicar_aprobada_deja_Applied` (D); `RF_GP_05_Aprobar_deja_la_solicitud_en_Applied`, `RF_GP_05_Aprobar_crea_la_concesion_del_solicitante` (A); `RF_GP_05_Checker_devuelve_true_solo_para_usuario_y_codigo` (I) |
| **RF-GP-06** | Aprobar dos veces, Rechazada → Aplicada y cualquier otra transición fuera de la tabla se rechazan y el estado no cambia | `RF_GP_06_Aprobar_dos_veces_es_rechazado_y_el_estado_no_cambia`, `RF_GP_06_Aplicar_una_rechazada_es_rechazado_y_conserva_Rejected` (D); `RF_GP_06_Todas_las_transiciones_fuera_de_la_tabla_son_rechazadas` (32 casos), `RF_GP_06_Tabla_de_transiciones_declarada_es_exacta`, `RF_GP_06_Aprobar_una_solicitud_Applied_es_rechazado`, `RF_GP_06_Aprobar_una_rechazada_es_rechazado` (D, máquina de estados); `RF_GP_06_Aprobar_dos_veces_no_crea_segunda_concesion`, `RF_GP_06_Aprobar_una_solicitud_Applied_es_rechazado`, `RF_GP_06_Rechazar_una_aprobada_es_rechazado`, `RF_GP_06_Rechazar_una_aplicada_es_rechazado`, `RF_GP_06_Rechazar_una_rechazada_es_rechazado` (A) |
| **RF-GP-07** | Un Administrador que aprueba o rechaza su propia solicitud recibe un rechazo | `RF_GP_07_Aprobar_la_propia_solicitud_es_rechazado`, `RF_GP_07_Rechazar_la_propia_solicitud_es_rechazado` (D y A); `RF_GP_07_Aprobar_la_propia_resuelta_prioriza_403`, `RF_GP_07_Rechazar_la_propia_resuelta_prioriza_403` (A) |

---

## Defectos

### Práctica 3

Cada etiqueta `practica-3-defecto-N` es **un solo commit cuyo padre es `practica-3`**. Modifica únicamente código de producción de Wheelby, nunca archivos de pruebas, y compila. Su mensaje indica el archivo y la línea. Las líneas son las de `practica-3`.

Para comprobar un defecto:

```bash
git checkout practica-3-defecto-1   # cambiar el número
cd src/backend
dotnet test                         # debe reportar las pruebas fallidas de la tabla
```

En `practica-3` pasan todas (165).

Ruta común: `src/backend/src/Domain/Wheelby/`.

| Etiqueta | Requisito | Archivo y línea | Defecto sembrado | Pruebas que fallan (lo que comprueban) |
|---|---|---|---|---|
| `practica-3-defecto-1` | RF-NEG-04 | `Wheelby.Domain/Reservations/ReservationStateMachine.cs`, línea 58 (fila nueva) y líneas 89 a 91 | La primera transición prohibida, `InProgress → Cancelled`, pasa a permitida: se agrega la fila (Renter y Host, sin guarda) y se elimina el bloque de `Validate` que rechaza la prohibida explícita, porque sin ese segundo cambio la fila nueva queda enmascarada. Un alquiler en curso se puede cancelar | **6:** `RF_NEG_04_InProgress_a_Cancelled_con_Host_es_prohibida_explicitamente` y `RF_NEG_04_InProgress_a_Cancelled_con_Renter_es_prohibida_explicitamente` (D), `RF_NEG_04_par_fuera_de_la_tabla_se_rechaza_y_estado_no_cambia` en los casos InProgress → Cancelled con Host y con Renter (D), `RF_NEG_03_AllowedTransitions_contiene_exactamente_las_cinco_filas_de_la_tabla` (D) y `RF_NEG_02_F5_cancelar_alquiler_en_curso_lanza_409` (A). Comprueban que cancelar un alquiler en curso se rechaza, que el estado no cambia y que la tabla tiene exactamente cinco filas |
| `practica-3-defecto-2` | RF-NEG-05 | `Wheelby.Domain/Reservations/ReservationStateMachine.cs`, línea 58 (fila nueva) y línea 70 | El terminal `Completed` deja de serlo: se agrega la fila `Completed → InProgress` (Host, sin guarda) y se quita `Completed` de `TerminalStates`, porque sin ese segundo cambio el chequeo terminal (línea 85) rechaza antes. Un alquiler completado se puede reabrir | **4:** `RF_NEG_05_desde_estado_terminal_cualquier_transicion_se_rechaza` en los casos Completed → InProgress con Host y con Renter, `RF_NEG_05_TerminalStates_es_exactamente_Completed_y_Cancelled` y `RF_NEG_03_AllowedTransitions_contiene_exactamente_las_cinco_filas_de_la_tabla` (D). Comprueban que del terminal no se sale y que la tabla y los terminales declarados son exactamente los del documento |
| `practica-3-defecto-3` | RF-NEG-03 | `Wheelby.Domain/Reservations/ReservationStateMachine.cs`, línea 25 | Se elimina la condición de la primera transición con condición, `Pending → Confirmed`: `static (r, _, utcNow, _) => utcNow <= r.StartDate` pasa a `null`. Se puede confirmar después de la fecha de inicio | **1:** `RF_NEG_03_Pending_a_Confirmed_despues_de_StartDate_guarda_no_cumplida_se_rechaza` (D). Comprueba que confirmar después de `StartDate` se rechaza y el estado sigue `Pending` |
| `practica-3-defecto-4` | RF-NEG-06 | `Wheelby.Application/Features/Commands/PublishVehicle/PublishVehicleCommandHandler.cs`, líneas 23 a 30 | Se elimina la consulta a Gestión de permisos (`IPermissionChecker.HasPermissionAsync`) y su `ForbiddenException`: cualquier Estándar publica sin que le aprueben `wheelby.publish-vehicle` | **3:** la prueba de integración `RF_NEG_06_integracion_publicar_sin_aprobacion_es_rechazado_y_despues_de_aprobar_se_permite` (I), que comprueba que publicar se rechaza mientras la solicitud no está aprobada; también `RF_NEG_06_integracion_si_el_administrador_rechaza_publicar_sigue_rechazado` (I) y `RF_NEG_02_F1_sin_permiso_lanza_403_y_no_guarda_nada` (A) |

### Práctica 2

Variante A. Cada etiqueta `practica-2-defecto-N` es un solo commit cuyo padre es `practica-2`. Modifica únicamente código de producción de Gestión de permisos, nunca archivos de pruebas, y su mensaje indica el archivo y la línea. Las líneas son las de `practica-2`.

Para comprobar un defecto:

```bash
git checkout practica-2-defecto-1   # cambiar el número
cd src/backend
dotnet test                         # debe reportar las pruebas fallidas de la tabla
```

| Etiqueta | Requisito | Archivo y línea | Comportamiento con el defecto | Pruebas que fallan (lo que comprueban) |
|---|---|---|---|---|
| `practica-2-defecto-1` | RF-GP-06 | `PermissionRequestStateMachine.cs`, `Validate`, líneas 69 y 77 | Al aprobar, la condición «el estado es Pendiente» pasa a «el estado no es Rechazada»: una solicitud ya aprobada o aplicada puede volver a aprobarse | **6:** `RF_GP_06_Aprobar_dos_veces_es_rechazado_y_el_estado_no_cambia` (D), `RF_GP_06_Aprobar_una_solicitud_Applied_es_rechazado` (D y A), `RF_GP_06_Aprobar_dos_veces_no_crea_segunda_concesion` (A) y `RF_GP_06_Todas_las_transiciones_fuera_de_la_tabla_son_rechazadas` en los casos Approved → Approved y Applied → Approved. Comprueban que aprobar dos veces se rechaza y que el estado no cambia |
| `practica-2-defecto-2` | RF-GP-06 | `PermissionRequestStateMachine.cs`, `AllowedTransitions` (fila nueva) y `Validate`, líneas 69 y 73-75 | Se declara Rechazada → Aplicada. Los dos cambios de `Validate` son los necesarios para que esa fila se alcance, porque Rejected es terminal y Rejected → Applied está prohibida de forma explícita: una solicitud rechazada puede llegar a Aplicada | **3:** `RF_GP_06_Aplicar_una_rechazada_es_rechazado_y_conserva_Rejected` (D), `RF_GP_06_Tabla_de_transiciones_declarada_es_exacta` y `RF_GP_06_Todas_las_transiciones_fuera_de_la_tabla_son_rechazadas` en el caso Rejected → Applied. Comprueban que Rechazada → Aplicada se rechaza y que la tabla declarada es exactamente la permitida |
| `practica-2-defecto-3` | RF-GP-07 | `ApprovePermissionRequestCommandHandler.cs`, línea 26; `RejectPermissionRequestCommandHandler.cs`, línea 24; `PermissionRequestStateMachine.cs`, líneas 25 y 39-40 | Se elimina la verificación de autorresolución en los cuatro lugares donde se aplica: un Administrador puede aprobar o rechazar su propia solicitud | **6:** `RF_GP_07_Aprobar_la_propia_solicitud_es_rechazado` (D y A), `RF_GP_07_Rechazar_la_propia_solicitud_es_rechazado` (D y A), `RF_GP_07_Aprobar_la_propia_resuelta_prioriza_403` y `RF_GP_07_Rechazar_la_propia_resuelta_prioriza_403` (A). Comprueban que resolver la propia solicitud se rechaza |
| `practica-2-defecto-4` | RF-GP-02 | `PermissionRequestReadStore.cs`, línea 31 | `RequesterId == requesterId` pasa a `!=`: un Estándar ve las solicitudes de otros y no las suyas | **1:** `RF_GP_02_Las_solicitudes_de_una_persona_excluyen_a_los_demas` (I). Comprueba que la consulta de un usuario devuelve solo sus solicitudes |
| `practica-2-defecto-5` | RF-GP-06 | `PermissionRequestStateMachine.cs`, `AllowedTransitions` (fila nueva después de Approved → Applied) | Se declara Aprobada → Pendiente: una solicitud aprobada puede volver a Pendiente | **2:** `RF_GP_06_Tabla_de_transiciones_declarada_es_exacta` y `RF_GP_06_Todas_las_transiciones_fuera_de_la_tabla_son_rechazadas` en el caso Approved → Pending. Comprueban que ninguna transición fuera de la tabla se acepta |
| `practica-2-defecto-6` | RF-GP-04 | `PermissionRequest.cs`, línea 67 | Se elimina `RejectionReason = rejectionReason?.Trim();`: una solicitud rechazada queda sin su motivo | **13:** `RF_GP_04_Rechazar_con_motivo_guarda_motivo_recortado` (D) y `RF_GP_04_Rechazar_guarda_un_motivo_recortado` (A), que comprueban que se guarda el motivo. También fallan las pruebas de RF-GP-06 que parten de una solicitud rechazada y comprueban que su motivo no cambia: `RF_GP_06_Aplicar_una_rechazada_es_rechazado_y_conserva_Rejected` (D), `RF_GP_06_Aprobar_una_rechazada_es_rechazado` (D), `RF_GP_06_Rechazar_una_rechazada_es_rechazado` (A) y 8 casos de `RF_GP_06_Todas_las_transiciones_fuera_de_la_tabla_son_rechazadas` con origen Rejected |

---

---

## 8. Variables de configuración

Todas se definen en **`.env`** (copia de `.env.example`). Ningún valor real está en el repositorio: `.env.example` solo trae valores de ejemplo, con los que la aplicación arranca. Las que van con `__` son la forma de entorno de las claves de .NET (`Jwt__Key` = `Jwt:Key`).

| Variable | ¿Obligatoria? | Para qué sirve | Quién la lee |
|---|---|---|---|
| `API_PORT` | No (8080) | Puerto de la API en tu máquina. Si lo cambias, cambia también el puerto de `App__ActivationUrl` | Docker Compose |
| `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB` | **Sí** | Crear la base del contenedor. Compose arma con ellas `ConnectionStrings__Postgres` para la API y el Worker | Docker Compose |
| `App__ActivationUrl` | **Sí** | Dirección del enlace del correo de activación (`http://localhost:8080/auth/activate`) | API |
| `Jwt__Key` | **Sí** | Clave secreta que firma la credencial de sesión (mínimo 32 caracteres) | API |
| `Jwt__Issuer`, `Jwt__Audience` | **Sí** | Emisor y destinatario de la credencial | API |
| `Jwt__ExpirationMinutes` | No (60) | Vigencia de la sesión, en minutos | API |
| `Admin__Email`, `Admin__Password` | **Sí** | Administrador fijo (rol `Administrator`), que la API crea al arrancar. Contraseña de 8+ caracteres, con letras y números | API |
| `Smtp__Host`, `Smtp__Port`, `Smtp__User`, `Smtp__Password`, `Smtp__From` | Para que lleguen los correos | Servidor de correo saliente (587, STARTTLS). Las credenciales reales van en el comentario de Moodle | Worker |
| `Worker__IntervalSeconds` | No (10, mínimo 1) | Segundos entre vueltas del Worker | Worker |
| `Documents__MaxFileSizeBytes` | No (10485760 = 10 MB) | Tamaño máximo de cada documento | API |
| `Documents__StoragePath` | No (la imagen usa `/app/data/documents`) | Carpeta de los documentos, dentro del volumen `wheelby-documents` | API |
| `ExchangeRate__ApiKey` | No | Clave de ExchangeRate-API. Sin ella, solo la búsqueda con `currency` responde 503 | API |
| `ExchangeRate__BaseUrl` | No (`https://v6.exchangerate-api.com/v6/`) | Dirección del proveedor | API |
| `ExchangeRate__TimeoutSeconds` | No (5) | Tiempo máximo de espera de la API externa | API |
| `ExchangeRate__CacheMinutes` | No (60) | Minutos que se guarda cada tasa en memoria | API |
| `OpenApi__Enabled` | No (el compose la pone en `true`) | Publica Scalar y OpenAPI fuera de `Development` | API |
| `Cors__AllowedOrigins__0` | No (`http://localhost:5173`) | Origen permitido para el frontend | API |

Si falta una obligatoria, `docker compose up` se detiene y dice cuál. Si la clave JWT es corta o la contraseña del Administrador no cumple la política, la API no arranca y el log (`docker compose logs api`) dice el motivo, sin mostrar el valor.

Valores fijados en el código (no son variables): vigencia del enlace de activación, 24 horas; vigencia del código de recuperación, 30 minutos; bloqueo tras 5 intentos fallidos, durante 15 minutos.

---

## 9. Ver los datos en PostgreSQL

La base no publica su puerto: se consulta desde el propio contenedor, sin instalar nada.

```bash
docker compose exec postgres psql -U wheelby -d wheelby
```

Cada módulo tiene su propio esquema. Los nombres de tabla llevan **comillas dobles** porque tienen mayúsculas:

```sql
SELECT * FROM access_control."Users";
SELECT * FROM access_control."Sessions";
SELECT * FROM notifications."QueuedEmails";
SELECT * FROM permissions."PermissionRequests";
SELECT * FROM permissions."PermissionGrants";
SELECT * FROM wheelby."Vehicles";
SELECT * FROM wheelby."Reservations";
SELECT * FROM documents."Documents";
```

Sal con `\q`.

---

## 10. Problemas frecuentes

| Síntoma | Causa | Solución |
|---|---|---|
| `no configuration file provided: not found` | Comando ejecutado fuera de la raíz del repositorio | Ir a la carpeta donde están `docker-compose.yml` y `.env.example` |
| `docker version` muestra error del servidor | Docker Desktop no está abierto | Abrirlo y esperar a que diga "running" |
| `env file .../.env not found` | No se creó `.env` | Sección 2.2 |
| `required variable ... is missing a value: Falta ... en .env` | Esa línea no está en `.env` o está vacía | Agregarla con el valor de `.env.example`, o volver a copiar la plantilla |
| `Bind for 0.0.0.0:8080 failed: port is already allocated` | Otro programa usa el puerto 8080 | Poner `API_PORT=8081` y `App__ActivationUrl=http://localhost:8081/auth/activate` en `.env`, y usar `http://localhost:8081` en todo el documento |
| `docker compose logs api` dice `Jwt:Key debe tener al menos 32 caracteres` o que la contraseña del Administrador no es válida | Valores de `.env` que no cumplen las reglas | Corregirlos y repetir `docker compose up` |
| `password authentication failed for user "wheelby"` | Cambiaste `POSTGRES_PASSWORD` después del primer arranque: la base conserva la anterior | `docker compose down -v` y `docker compose up --build` (borra los datos) |
| Los correos quedan `Pending` y el Worker dice `Failed: 1` | SMTP de ejemplo, o credenciales o puerto incorrectos | Sección 2.3; con Gmail, usar la contraseña de aplicación. Mientras tanto, leer el enlace de la cola (sección 3) |
| El enlace de activación no abre | `App__ActivationUrl` no tiene el puerto real de la API | Que coincida con `API_PORT` |
| `GET /auth/me` u otras rutas responden 401 en Scalar | Falta pegar el token, o ya venció (60 min), o se cerró la sesión | Volver a iniciar sesión y pegar el nuevo `accessToken` |
| Empezar de cero | — | `docker compose down -v` y `docker compose up --build` |

---

## 11. Estructura del repositorio

```
<raíz del repositorio>/
├─ Dockerfile                        Imagen de la API y del Worker (y etapa de pruebas)
├─ .dockerignore
├─ docker-compose.yml                postgres, api y worker con un solo comando
├─ .env.example                      Plantilla del .env (solo valores de ejemplo)
├─ docs/
│  ├─ maquina-de-estados.md          Máquinas de estados de la Reserva y de los Permisos
│  ├─ wheelby/README.md              Cómo probar el módulo de negocio Wheelby
│  ├─ control-acceso/README.md       Cómo verificar cada criterio de Control de acceso
│  ├─ gestion-permisos/README.md     Cómo verificar cada criterio de Gestión de permisos
│  └─ bitacora-asignacion-2.md       Bitácora de la sesión con el agente
└─ src/
   └─ backend/
      ├─ Wheelby.slnx
      └─ src/
         ├─ Host/
         │  ├─ Host.WebAPI/          API (Minimal APIs, Scalar)
         │  └─ Host.Worker/          Envío continuo de los correos pendientes
         ├─ Shared/                  SharedKernel y Shared.Application
         ├─ Core/
         │  ├─ AccessControl/        Control de acceso
         │  ├─ Documents/            Manejador de documentos (incluye Documents.Test)
         │  ├─ Notifications/        Cola de correos
         │  └─ Permissions/          Gestión de permisos (incluye Permissions.Test)
         └─ Domain/
            └─ Wheelby/              Módulo de negocio: vehículos, reservas y su máquina de estados
               └─ Wheelby.Test/      Pruebas del negocio, de rutas y de integración con permisos
```
