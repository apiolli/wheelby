# Wheelby

Marketplace de alquiler de vehículos entre particulares. Backend en C# (.NET 10), monolito modular con Clean Architecture, CQRS y DDD, sobre PostgreSQL.

Programación III · ITLA · 2026-C-3 · Práctica 2

Este documento explica, paso a paso, cómo clonar, configurar y ejecutar el backend. Para comprobar cada criterio de aceptación, ver la sección [Cómo verificar los criterios](#9-cómo-verificar-los-criterios).

---

## Índice

1. [Requisitos](#1-requisitos)
2. [Obtener el código](#2-obtener-el-código)
3. [Carpeta de trabajo](#3-carpeta-de-trabajo)
4. [Base de datos con Docker](#4-base-de-datos-con-docker)
5. [Secretos de la aplicación](#5-secretos-de-la-aplicación)
6. [Ejecutar la API](#6-ejecutar-la-api)
7. [Ejecutar el envío de correos (Worker)](#7-ejecutar-el-envío-de-correos-worker)
8. [Control de acceso: los cuatro frentes](#8-control-de-acceso-los-cuatro-frentes)
9. [Cómo verificar los criterios](#9-cómo-verificar-los-criterios)
   - [Pruebas](#pruebas)
   - [Defectos](#defectos)
10. [Variables de configuración](#10-variables-de-configuración)
11. [Ver los datos en PostgreSQL](#11-ver-los-datos-en-postgresql)
12. [Problemas frecuentes](#12-problemas-frecuentes)
13. [Estructura del repositorio](#13-estructura-del-repositorio)

---

## 1. Requisitos

| Herramienta     | Versión                                        | Cómo comprobarla                            |
| --------------- | ---------------------------------------------- | ------------------------------------------- |
| Git             | cualquiera reciente                            | `git --version`                             |
| .NET SDK        | **10.0** o superior                            | `dotnet --version` (debe empezar por `10.`) |
| Docker Desktop  | con Docker Compose v2                          | `docker version` y `docker compose version` |
| Una cuenta SMTP | por ejemplo Gmail con contraseña de aplicación | ver sección 5                               |

- **Docker Desktop debe estar abierto y en estado "running"** antes de los pasos siguientes.
- **No hace falta** instalar PostgreSQL ni la herramienta `dotnet-ef`: la base corre en Docker y la aplicación crea las tablas sola al arrancar.
- Opcional: un cliente de PostgreSQL (DBeaver, pgAdmin) para ver los datos.

Los comandos de este documento están escritos para **PowerShell (Windows)**. Donde el comando cambia en macOS o Linux, se indica.

---

## 2. Obtener el código

```bash
git clone <URL del repositorio>
cd <carpeta del repositorio>
git checkout practica-2
```

---

## 3. Carpeta de trabajo

**Todos los comandos de las secciones 4 a 8 se ejecutan desde la carpeta `src/backend`**, que es donde están `Wheelby.slnx`, `docker-compose.yml` y `.env.example`.

Desde la raíz del repositorio:

```bash
cd src/backend
```

Comprobación: este comando debe listar los tres archivos.

```powershell
ls Wheelby.slnx, docker-compose.yml, .env.example
```

Si alguno falta, no estás en la carpeta correcta.

---

## 4. Base de datos con Docker

### 4.1 Crear el archivo `.env`

El archivo `.env` lo lee **solo Docker Compose** para crear la base de datos. Se crea copiando la plantilla:

```powershell
Copy-Item .env.example .env        # macOS / Linux: cp .env.example .env
```

Abre `.env` y cambia **solo** estas variables:

| Variable            | Qué poner                                                                                     |
| ------------------- | --------------------------------------------------------------------------------------------- |
| `POSTGRES_PASSWORD` | Una contraseña cualquiera para la base de datos (por ejemplo `wheelby123`)                    |
| `POSTGRES_PORT`     | `5432`. Si ese puerto ya está ocupado en tu máquina (por un PostgreSQL instalado), usa `5433` |

Deja `POSTGRES_USER=wheelby` y `POSTGRES_DB=wheelby`. Las demás líneas del `.env` son de referencia y **no** las lee la aplicación (ver sección 5).

### 4.2 Levantar PostgreSQL

```powershell
docker compose up -d
docker compose ps
```

Espera unos segundos hasta que la columna de estado diga **`healthy`**.

- La primera vez descarga la imagen `postgres:17` (puede tardar unos minutos).
- Los datos se guardan en un volumen de Docker: sobreviven a reiniciar la aplicación y al contenedor.
- `docker compose down` detiene el contenedor y **conserva** los datos.
- `docker compose down -v` borra también los datos (base vacía).

---

## 5. Secretos de la aplicación

**La API y el Worker no leen el archivo `.env`.** En desarrollo leen su configuración de `dotnet user-secrets`, que guarda los valores en tu perfil de usuario, fuera del repositorio. Cada proyecto tiene su propio almacén, así que se configuran por separado.

Ejecuta todo lo siguiente desde `src/backend`. Sustituye cada `<...>` por tu valor.

### 5.1 Cadena de conexión

Usa el **mismo puerto y contraseña** que pusiste en el `.env`:

```
Host=localhost;Port=<POSTGRES_PORT>;Database=wheelby;Username=wheelby;Password=<POSTGRES_PASSWORD>
```

Ejemplo con puerto 5432 y contraseña `wheelby123`:
`Host=localhost;Port=5432;Database=wheelby;Username=wheelby;Password=wheelby123`

### 5.2 Secretos de la API

```powershell
$api = "src/Host/Host.WebAPI"

dotnet user-secrets set "ConnectionStrings:Postgres" "<cadena de conexión de 5.1>" --project $api

# Enlace del correo de activación (5299 es el puerto de la API; ver sección 6)
dotnet user-secrets set "App:ActivationUrl" "http://localhost:5299/auth/activate" --project $api

# Firma de la credencial de sesión: clave aleatoria de al menos 32 caracteres
$key = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
dotnet user-secrets set "Jwt:Key" $key --project $api
dotnet user-secrets set "Jwt:Issuer" "wheelby" --project $api
dotnet user-secrets set "Jwt:Audience" "wheelby-api" --project $api
dotnet user-secrets set "Jwt:ExpirationMinutes" "60" --project $api

# Administrador fijo (rol `Administrator`): se crea al arrancar la API.
# La contraseña debe tener al menos 8 caracteres, con letras y números.
dotnet user-secrets set "Admin:Email" "<correo del administrador>" --project $api
dotnet user-secrets set "Admin:Password" "<contraseña del administrador>" --project $api
```

En macOS / Linux, cambia la primera línea por `api="src/Host/Host.WebAPI"`, usa `$api` igual, y genera la clave con `key=$(openssl rand -base64 48)` antes de `dotnet user-secrets set "Jwt:Key" "$key" --project $api`.

### 5.3 Secretos del Worker (envío de correos)

```powershell
$worker = "src/Host/Host.Worker"

dotnet user-secrets set "ConnectionStrings:Postgres" "<cadena de conexión de 5.1>" --project $worker
dotnet user-secrets set "Smtp:Host" "<servidor SMTP>" --project $worker
dotnet user-secrets set "Smtp:Port" "587" --project $worker
dotnet user-secrets set "Smtp:User" "<usuario SMTP>" --project $worker
dotnet user-secrets set "Smtp:Password" "<contraseña SMTP>" --project $worker
dotnet user-secrets set "Smtp:From" "<correo remitente>" --project $worker
```

**Credenciales SMTP:** no están en el repositorio. Se entregan por separado en el comentario de la entrega en Moodle. También sirve una cuenta propia:

- **Gmail:** `Smtp:Host` = `smtp.gmail.com`, `Smtp:Port` = `587`, `Smtp:User` y `Smtp:From` = la dirección de Gmail, y `Smtp:Password` = una **contraseña de aplicación** de 16 caracteres sin espacios (requiere la verificación en dos pasos activada; se crea en `myaccount.google.com/apppasswords`). La contraseña normal de la cuenta no funciona.

### 5.4 Comprobar los secretos

```powershell
dotnet user-secrets list --project src/Host/Host.WebAPI
dotnet user-secrets list --project src/Host/Host.Worker
```

La API debe mostrar 8 claves (`ConnectionStrings:Postgres`, `App:ActivationUrl`, `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpirationMinutes`, `Admin:Email`, `Admin:Password`) y el Worker 6 (`ConnectionStrings:Postgres` y las cinco `Smtp:*`).

> **Alternativa sin user-secrets:** cualquier clave se puede definir como variable de entorno de la terminal, cambiando `:` por `__`. Por ejemplo: `$env:ConnectionStrings__Postgres = "Host=localhost;..."`. Solo dura mientras la terminal esté abierta.

---

## 6. Ejecutar la API

Desde `src/backend`:

```powershell
dotnet run --project src/Host/Host.WebAPI
```

Al arrancar, la API:

1. crea o actualiza las tablas de los cuatro módulos (esquemas `access_control`, `notifications`, `permissions` y `wheelby`);
2. crea el Administrador (rol `Administrator`) con `Admin:Email` y `Admin:Password` si no existe (y lo restaura si perdió el rol o fue desactivado, sin tocar su contraseña).

La consola muestra la dirección, normalmente:

```
Now listening on: http://localhost:5299
```

- **Documentación interactiva (Scalar):** `http://localhost:5299/scalar/v1`
- Si el puerto mostrado **no** es `5299`, actualiza el enlace de activación con ese puerto y reinicia la API:

  ```powershell
  dotnet user-secrets set "App:ActivationUrl" "http://localhost:<puerto>/auth/activate" --project src/Host/Host.WebAPI
  ```

Si falta alguna variable obligatoria (`ConnectionStrings:Postgres`, `App:ActivationUrl`, `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`, `Admin:Email` o `Admin:Password`; ver la sección 10), o `Jwt:Key` tiene menos de 32 caracteres, o `Admin:Password` no cumple la política, **la API no arranca** y la consola indica qué falta.

Deja esta terminal abierta mientras pruebas.

---

## 7. Ejecutar el envío de correos (Worker)

Los correos (activación, recuperación de contraseña) **no se envían dentro de la operación**: quedan en una cola en la base de datos con estado `Pending`. Un proceso aparte los envía.

En **otra terminal**, también desde `src/backend`:

```powershell
dotnet run --project src/Host/Host.Worker
```

El Worker procesa los pendientes **en una sola pasada y termina**. Su última línea indica el resultado, por ejemplo `Pending emails processed. Sent: 1. Failed: 0.`

- Se ejecuta cada vez que se quiera enviar lo que haya en la cola (después de registrarse, de pedir una recuperación, etc.).
- Ejecutarlo dos veces seguidas no reenvía nada.
- Si el SMTP no responde, los correos siguen `Pending` y se envían en la siguiente ejecución.
- Si el correo no aparece en la bandeja de entrada, revisa la carpeta de spam.

---

## 8. Control de acceso: los cuatro frentes

Con la API corriendo (sección 6), en Scalar (`http://localhost:5299/scalar/v1`). Para usar un token en Scalar: copia el `accessToken` de la respuesta de `POST /auth/login` y pégalo en el botón de autorización (esquema Bearer).

### Roles

Cada usuario tiene **exactamente un rol**. En las peticiones, en las respuestas y en la base de datos los roles se escriben así, en inglés:

| Rol | Valor exacto | Quién lo tiene |
|---|---|---|
| Estándar | `Standard` | Todo usuario que se registra con `POST /auth/register` |
| Administrador | `Administrator` | El Administrador fijo (`Admin:Email` / `Admin:Password`), que la API crea al arrancar, y quien reciba ese rol con `PATCH /admin/users/{id}/role` |

### Rutas y formato de los cuerpos

Todos los cuerpos son JSON (`Content-Type: application/json`) y los nombres de los campos van en camelCase. Los errores se devuelven como `application/problem+json`. Una ruta con sesión exige la cabecera `Authorization: Bearer <accessToken>`.

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

Si no quieres esperar el correo, el enlace de activación y el código de recuperación también se leen de la cola:

```sql
SELECT "To", "Status", "Body" FROM notifications."QueuedEmails" ORDER BY "CreatedAt" DESC;
```

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
3. En otra terminal, desde `src/backend`: `dotnet run --project src/Host/Host.Worker` → `Sent: 1`. Llega el correo «Activa tu cuenta de Wheelby» (revisa spam).
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
   Invoke-RestMethod -Method Get -Uri "http://localhost:5299/admin/users" `
     -Headers @{ Authorization = "Bearer <token del usuario Estándar>" }
   ```

   `Invoke-RestMethod` lanza una excepción con el 403: es el resultado esperado.
3. Sin token → **401**.
4. Comparación: `POST /auth/login` con `Admin:Email` y `Admin:Password` (rol `Administrator`), pega ese token y repite `GET /admin/users` → **200**.

### Frente 4: recuperación de contraseña

1. `POST /auth/forgot-password`:

   ```json
   { "email": "<tu correo>" }
   ```

   → **200**.
2. Ejecuta el Worker (`dotnet run --project src/Host/Host.Worker`). Llega el correo «Recupera tu contraseña de Wheelby» con un código.
3. `POST /auth/reset-password`:

   ```json
   { "email": "<tu correo>", "code": "<código del correo>", "newPassword": "Nueva1234" }
   ```

   → **200** "Contraseña restablecida. Ya puedes iniciar sesión."
4. `POST /auth/login` con la contraseña **anterior** (`Clave1234`) → **401**. Con la **nueva** (`Nueva1234`) → **200**.
5. Repetir el paso 3 con el mismo código → **400** (el código es de un solo uso).

---

## 9. Cómo verificar los criterios

- **Control de acceso y correo por cola** (RF-CA-01 a 22, RF-NOT-08, 09, 12, 13): pasos exactos, cuerpos de petición y consultas SQL para cada criterio en [docs/control-acceso/README.md](docs/control-acceso/README.md).
- **Máquina de estados del negocio** (RF-NEG-03, 04, 05, RD-04): tabla de transiciones y ubicación en el código en [docs/maquina-de-estados/README.md](docs/maquina-de-estados/README.md).
- **Gestión de permisos** (RF-GP-01 a 07, RD-09): pasos exactos, cuerpos de petición y consultas SQL para cada criterio en [docs/gestion-permisos/README.md](docs/gestion-permisos/README.md).

Atajos útiles para las pruebas:

- **Leer el enlace de activación o el código de recuperación sin esperar el correo:**

  ```sql
  SELECT "To", "Status", "Body" FROM notifications."QueuedEmails" ORDER BY "CreatedAt" DESC;
  ```

- **Simular SMTP caído (RF-NOT-08):** cambiar el puerto del Worker a uno inválido, registrar un usuario (la API responde 201 y el correo queda `Pending`), ejecutar el Worker (`Failed: 1`) y restaurar el puerto:

  ```powershell
  dotnet user-secrets set "Smtp:Port" "9999" --project src/Host/Host.Worker
  # ... probar ...
  dotnet user-secrets set "Smtp:Port" "587" --project src/Host/Host.Worker
  ```

- **Peticiones construidas a mano (RF-CA-06),** fuera de Scalar:

  ```powershell
  Invoke-RestMethod -Method Get -Uri "http://localhost:5299/admin/users" `
    -Headers @{ Authorization = "Bearer <token de un usuario Estándar>" }
  ```

  Un 403 hace que `Invoke-RestMethod` lance una excepción con el código de estado: es el resultado esperado.

- **Persistencia (RD-09):** detener la API (Ctrl+C) y volver a ejecutarla; o `docker compose down` y `docker compose up -d`. Los usuarios siguen ahí.
- **Credenciales fuera del repositorio (RD-10, RF-NOT-13):** desde la raíz del repositorio,

  ```bash
  git grep -n -i "password" -- ':!*.md'
  ```

  Solo aparecen nombres de variables y valores de ejemplo (`change_me`).

---

## Pruebas

Las pruebas del repositorio se ejecutan con **un solo comando**, desde un clon limpio:

```bash
cd src/backend
dotnet test
```

`dotnet test` compila la solución completa (`Wheelby.slnx`) y ejecuta las **71 pruebas** de `Permissions.Test` (una prueba con varios casos cuenta una vez por caso). Resultado esperado: `Failed: 0, Passed: 71`. No levantan la API ni necesitan PostgreSQL ni Docker.

- **Dominio:** reglas de `PermissionRequest` y la matriz completa de transiciones de `PermissionRequestStateMachine` (32 combinaciones de origen, destino y actor, más la tabla declarada exacta).
- **Casos de uso:** los handlers de crear, listar, aprobar y rechazar se prueban con dobles de sus puertos: reloj fijo, usuario actual fijo, catálogo fijo, repositorios en memoria y unidad de trabajo que cuenta los guardados. Cada prueba crea sus propios dobles, sin estado compartido.
- **Infraestructura:** `PermissionRequestReadStore` y `PermissionChecker` se prueban sobre `PermissionsDbContext` con EF InMemory, con un nombre de base único por prueba.
- Todas usan fechas fijas, siguen Arrange/Act/Assert, verifican un solo comportamiento y llevan el ID del requisito al inicio del nombre (`RF_GP_0X_...`).
- La primera vez, `dotnet test` restaura los paquetes automáticamente. No hace falta instalar `dotnet-ef` ni Docker para ejecutar las pruebas.

### Pruebas por requisito

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

## 10. Variables de configuración

Ningún valor real está en el repositorio. `.env.example` solo contiene valores de ejemplo.

**Obligatorias:** si falta una variable obligatoria, la aplicación que la usa **no arranca** y la consola dice cuál falta (sin mostrar su valor).

| Variable (como user-secret)  | Como variable de entorno | ¿Obligatoria? | Para qué sirve | Quién la lee |
| ---------------------------- | ------------------------ | ------------- | -------------- | ------------ |
| —                            | `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`, `POSTGRES_PORT` | Sí | Crear la base de datos del contenedor y publicar su puerto | Docker Compose (`.env`) |
| `ConnectionStrings:Postgres` | `ConnectionStrings__Postgres` | **Sí** | Conexión a la base de datos | API y Worker |
| `App:ActivationUrl`          | `App__ActivationUrl`          | **Sí** | Dirección a la que apunta el enlace del correo de activación (`http://localhost:5299/auth/activate`) | API |
| `Jwt:Key`                    | `Jwt__Key`                    | **Sí** | Clave secreta que firma la credencial de sesión (mínimo 32 caracteres) | API |
| `Jwt:Issuer`                 | `Jwt__Issuer`                 | **Sí** | Emisor de la credencial (por ejemplo `wheelby`) | API |
| `Jwt:Audience`               | `Jwt__Audience`               | **Sí** | Destinatario de la credencial (por ejemplo `wheelby-api`) | API |
| `Jwt:ExpirationMinutes`      | `Jwt__ExpirationMinutes`      | No (60 por defecto) | Vigencia de la sesión, en minutos | API |
| `Admin:Email`                | `Admin__Email`                | **Sí** | Correo del Administrador fijo (rol `Administrator`) | API |
| `Admin:Password`             | `Admin__Password`             | **Sí** | Contraseña inicial del Administrador fijo (8+ caracteres, con letras y números) | API |
| `Smtp:Host`                  | `Smtp__Host`                  | **Sí**, para el Worker | Servidor de correo saliente | Worker |
| `Smtp:Port`                  | `Smtp__Port`                  | **Sí**, para el Worker | Puerto del servidor de correo (587, STARTTLS) | Worker |
| `Smtp:User`                  | `Smtp__User`                  | **Sí**, para el Worker | Usuario del servidor de correo | Worker |
| `Smtp:Password`              | `Smtp__Password`              | **Sí**, para el Worker | Contraseña del servidor de correo | Worker |
| `Smtp:From`                  | `Smtp__From`                  | **Sí**, para el Worker | Remitente de los correos | Worker |

Valores fijados en el código (no son variables): vigencia del enlace de activación, 24 horas; vigencia del código de recuperación, 30 minutos; bloqueo tras 5 intentos fallidos, durante 15 minutos.

---

## 11. Ver los datos en PostgreSQL

Con cualquier cliente de PostgreSQL (DBeaver, pgAdmin):

| Campo         | Valor                     |
| ------------- | ------------------------- |
| Host          | `localhost`               |
| Puerto        | el de `POSTGRES_PORT`     |
| Base de datos | `wheelby`                 |
| Usuario       | `wheelby`                 |
| Contraseña    | la de `POSTGRES_PASSWORD` |

O desde la terminal, sin instalar nada:

```powershell
docker exec -it wheelby-postgres psql -U wheelby -d wheelby
```

Cada módulo tiene su propio esquema. Los nombres de tabla llevan **comillas dobles** porque tienen mayúsculas:

```sql
SELECT * FROM access_control."Users";
SELECT * FROM access_control."Sessions";
SELECT * FROM notifications."QueuedEmails";
SELECT * FROM permissions."PermissionRequests";
SELECT * FROM permissions."PermissionGrants";
SELECT * FROM wheelby."Reservations";
```

---

## 12. Problemas frecuentes

| Síntoma                                                                  | Causa                                                                                              | Solución                                                                                                                                                                                 |
| ------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `no configuration file provided: not found`                              | Comando ejecutado fuera de `src/backend`                                                           | Volver a la sección 3                                                                                                                                                                    |
| `docker version` muestra error del servidor                              | Docker Desktop no está abierto                                                                     | Abrirlo y esperar a que diga "running"                                                                                                                                                   |
| `Bind for 0.0.0.0:5432 failed: port is already allocated`                | Otro programa usa el puerto 5432                                                                   | Poner `POSTGRES_PORT=5433` en `.env`, `docker compose up -d`, y usar `Port=5433` en la cadena de conexión de **ambos** proyectos                                                         |
| La API no arranca y menciona `ConnectionStrings`, `Jwt`, `Admin` o `App` | Falta un secreto, o la clave JWT es corta, o la contraseña del Administrador no cumple la política | Repetir la sección 5.2 y comprobar con 5.4                                                                                                                                               |
| `password authentication failed for user "wheelby"`                      | La contraseña de la cadena de conexión no coincide con la base                                     | Usar la misma que en `.env`. Si cambiaste `POSTGRES_PASSWORD` después del primer `up`, la base conserva la anterior: `docker compose down -v` y `docker compose up -d` (borra los datos) |
| `Could not find the global property 'UserSecretsId'`                     | El proyecto no tiene almacén de secretos inicializado                                              | `dotnet user-secrets init --project <proyecto>` y repetir los `set`                                                                                                                      |
| El Worker dice `Failed: 1`                                               | Credenciales o puerto SMTP incorrectos, o SMTP no accesible                                        | Revisar la sección 5.3. Con Gmail, usar la contraseña de aplicación, no la normal                                                                                                        |
| El correo no llega                                                       | Todavía no se ejecutó el Worker, o está en spam                                                    | Ejecutar el Worker (sección 7) y revisar spam                                                                                                                                            |
| El enlace de activación no abre                                          | `App:ActivationUrl` no tiene el puerto real de la API                                              | Sección 6                                                                                                                                                                                |
| `GET /auth/me` u otras rutas responden 401 en Scalar                     | Falta pegar el token, o ya venció (60 min), o se cerró la sesión                                   | Volver a iniciar sesión y pegar el nuevo `accessToken`                                                                                                                                   |
| Empezar de cero                                                          | —                                                                                                  | `docker compose down -v`, `docker compose up -d` y arrancar la API                                                                                                                       |

---

## 13. Estructura del repositorio

```
<raíz del repositorio>/
├─ docs/
│  ├─ maquina-de-estados/README.md   Máquinas de estados de la Reserva y de los Permisos
│  ├─ control-acceso/README.md       Cómo verificar cada criterio de Control de acceso
│  ├─ gestion-permisos/README.md     Cómo verificar cada criterio de Gestión de permisos
│  └─ bitacora-asignacion-2.md       Bitácora de la sesión con el agente
└─ src/
   └─ backend/                       ← carpeta de trabajo (sección 3)
      ├─ Wheelby.slnx
      ├─ docker-compose.yml          PostgreSQL
      ├─ .env.example                Plantilla del .env (solo valores de ejemplo)
      └─ src/
         ├─ Host/
         │  ├─ Host.WebAPI/          API (Minimal APIs, Scalar)
         │  └─ Host.Worker/          Envío de los correos pendientes
         ├─ Shared/                  SharedKernel y Shared.Application
         ├─ Core/
         │  ├─ AccessControl/        Control de acceso
         │  ├─ Notifications/        Cola de correos
         │  └─ Permissions/          Gestión de permisos (incluye Permissions.Test)
         └─ Domain/
            └─ Wheelby/              Módulo de negocio (Reserva y su máquina de estados)
```
