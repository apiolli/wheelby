# Wheelby

Marketplace de alquiler de vehículos entre particulares. Backend en C# (.NET 10), monolito modular con Clean Architecture, CQRS y DDD, sobre PostgreSQL.

Programación III · ITLA · 2026-C-3 · Práctica 1

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
8. [Prueba rápida de punta a punta](#8-prueba-rápida-de-punta-a-punta)
9. [Cómo verificar los criterios](#9-cómo-verificar-los-criterios)
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
git checkout practica-1
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

# Administrador fijo: se crea al arrancar la API.
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

1. crea o actualiza las tablas de los tres módulos (esquemas `access_control`, `notifications` y `wheelby`);
2. crea el Administrador con `Admin:Email` y `Admin:Password` si no existe (y lo restaura si perdió el rol o fue desactivado, sin tocar su contraseña).

La consola muestra la dirección, normalmente:

```
Now listening on: http://localhost:5299
```

- **Documentación interactiva (Scalar):** `http://localhost:5299/scalar/v1`
- Si el puerto mostrado **no** es `5299`, actualiza el enlace de activación con ese puerto y reinicia la API:

  ```powershell
  dotnet user-secrets set "App:ActivationUrl" "http://localhost:<puerto>/auth/activate" --project src/Host/Host.WebAPI
  ```

Si falta alguna variable, o `Jwt:Key` tiene menos de 32 caracteres, o `Admin:Password` no cumple la política, **la API no arranca** y la consola indica qué falta.

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

## 8. Prueba rápida de punta a punta

Con la API corriendo, en Scalar (`http://localhost:5299/scalar/v1`):

1. **Registrarse:** `POST /auth/register`

   ```json
   {
     "fullName": "Tu Nombre",
     "email": "<tu correo real>",
     "password": "Clave1234"
   }
   ```

   Respuesta: 201.

2. **Intentar iniciar sesión antes de activar:** `POST /auth/login` con ese correo y contraseña → 403 "La cuenta no está activa…".
3. **Enviar el correo:** en la otra terminal, `dotnet run --project src/Host/Host.Worker`.
4. **Activar:** abrir el enlace del correo recibido → "Cuenta activada". Abrirlo otra vez → rechazo.
5. **Iniciar sesión:** `POST /auth/login` → 200 con `accessToken`.
6. **Autenticarse en Scalar:** copiar el `accessToken` y pegarlo en el botón de autorización (esquema Bearer). Luego `GET /auth/me` → 200 con el usuario y su rol.
7. **Administrador:** `POST /auth/login` con `Admin:Email` y `Admin:Password`, autenticarse con ese token y probar `GET /admin/users`.

---

## 9. Cómo verificar los criterios

- **Control de acceso y correo por cola** (RF-CA-01 a 22, RF-NOT-08, 09, 12, 13): pasos exactos, cuerpos de petición y consultas SQL para cada criterio en [docs/control-de-acceso/README.md](docs/control-de-acceso/README.md).
- **Máquina de estados del negocio** (RF-NEG-03, 04, 05, RD-04): tabla de transiciones y ubicación en el código en [docs/maquina-de-estados.md](docs/maquina-de-estados.md).

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

## 10. Variables de configuración

Ningún valor real está en el repositorio. `.env.example` solo contiene valores de ejemplo.

| Variable (como user-secret)  | Como variable de entorno                                             | Para qué sirve                                                         | Quién la lee            |
| ---------------------------- | -------------------------------------------------------------------- | ---------------------------------------------------------------------- | ----------------------- |
| —                            | `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`, `POSTGRES_PORT` | Crear la base de datos del contenedor y publicar su puerto             | Docker Compose (`.env`) |
| `ConnectionStrings:Postgres` | `ConnectionStrings__Postgres`                                        | Conexión a la base de datos                                            | API y Worker            |
| `App:ActivationUrl`          | `App__ActivationUrl`                                                 | Dirección a la que apunta el enlace del correo de activación           | API                     |
| `Jwt:Key`                    | `Jwt__Key`                                                           | Clave secreta que firma la credencial de sesión (mínimo 32 caracteres) | API                     |
| `Jwt:Issuer`                 | `Jwt__Issuer`                                                        | Emisor de la credencial                                                | API                     |
| `Jwt:Audience`               | `Jwt__Audience`                                                      | Destinatario de la credencial                                          | API                     |
| `Jwt:ExpirationMinutes`      | `Jwt__ExpirationMinutes`                                             | Vigencia de la sesión, en minutos                                      | API                     |
| `Admin:Email`                | `Admin__Email`                                                       | Correo del Administrador fijo                                          | API                     |
| `Admin:Password`             | `Admin__Password`                                                    | Contraseña inicial del Administrador fijo                              | API                     |
| `Smtp:Host`                  | `Smtp__Host`                                                         | Servidor de correo saliente                                            | Worker                  |
| `Smtp:Port`                  | `Smtp__Port`                                                         | Puerto del servidor de correo (587, STARTTLS)                          | Worker                  |
| `Smtp:User`                  | `Smtp__User`                                                         | Usuario del servidor de correo                                         | Worker                  |
| `Smtp:Password`              | `Smtp__Password`                                                     | Contraseña del servidor de correo                                      | Worker                  |
| `Smtp:From`                  | `Smtp__From`                                                         | Remitente de los correos                                               | Worker                  |

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
│  ├─ maquina-de-estados.md          Máquina de estados de la Reserva
│  └─ control-de-acceso/README.md    Cómo verificar cada criterio de Control de acceso
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
         │  └─ Notifications/        Cola de correos
         └─ Domain/
            └─ Wheelby/              Módulo de negocio (Reserva y su máquina de estados)
```
