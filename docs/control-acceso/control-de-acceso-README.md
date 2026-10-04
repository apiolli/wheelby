# 1. Control de acceso

Semanas 2–4 · en progreso

Registro con activación por correo, sesión, roles, administración de usuarios, y recuperación/restablecimiento de contraseña.

**Estado por pull request**

| PR                                  | Requisitos                   | Estado       |
| ----------------------------------- | ---------------------------- | ------------ |
| Registro y activación               | RF-CA-01, 02, 14, 15, 16, 17 | Implementado |
| Sesión                              | RF-CA-03, 07, 18, 19         | Pendiente    |
| Administración de usuarios          | RF-CA-04, 05, 06, 08, 20, 21 | Pendiente    |
| Recuperación de contraseña          | RF-CA-09 a 13, 22            | Pendiente    |
| Cola de correos (mínima)            | RF-NOT-08, 09, 12, 13        | Implementado |
| Estructura de la máquina de estados | RF-NEG-03, 04, 05, RD-04     | Pendiente    |

Las secciones marcadas como **Pendiente** se completan en el PR que las implementa. Mientras tanto no se pueden verificar.

## Requisitos previos

- .NET SDK 10 y Docker Desktop.
- Un servidor SMTP (por ejemplo Gmail con contraseña de aplicación).
- Todos los comandos se ejecutan desde la carpeta `backend`.
- La base de datos y los secretos se preparan siguiendo el README principal (PostgreSQL con Docker Compose y `dotnet user-secrets`).

## Variables de entorno

Nunca se guardan valores en el repositorio: `.env.example` solo trae ejemplos. El archivo `.env` lo lee **solo Docker Compose**. La API y el Worker leen sus valores de `dotnet user-secrets` (separador `:`) o de variables de entorno (separador `__`). Por ejemplo, `Smtp:Host` en user-secrets es `Smtp__Host` como variable de entorno.

| Variable                                                                                    | Para qué sirve                                                                                           | Quién la lee   | Estado                           |
| ------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------- | -------------- | -------------------------------- |
| `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`, `POSTGRES_PORT`                        | Crean la base de datos del contenedor y el puerto publicado                                              | Docker Compose | Implementado                     |
| `ConnectionStrings__Postgres`                                                               | Conexión a la base donde persisten usuarios, tokens y correos en cola                                    | API y Worker   | Implementado                     |
| `Smtp__Host`                                                                                | Host del servidor de correo saliente                                                                     | Worker         | Implementado                     |
| `Smtp__Port`                                                                                | Puerto del servidor de correo (587 con STARTTLS)                                                         | Worker         | Implementado                     |
| `Smtp__User`                                                                                | Usuario con el que se autentica el envío                                                                 | Worker         | Implementado                     |
| `Smtp__Password`                                                                            | Contraseña de aplicación del servidor de correo                                                          | Worker         | Implementado                     |
| `Smtp__From`                                                                                | Remitente de los correos                                                                                 | Worker         | Implementado                     |
| `App__ActivationUrl`                                                                        | URL a la que apunta el enlace del correo de activación (hoy, el endpoint `GET /auth/activate` de la API) | API            | Implementado                     |
| `Jwt__Key` (mínimo 32 caracteres), `Jwt__Issuer`, `Jwt__Audience`, `Jwt__ExpirationMinutes` | Firma y vigencia de la credencial de sesión                                                              | API            | Pendiente (PR de sesión)         |
| `Admin__Email`, `Admin__Password`                                                           | Cuenta del Administrador que se crea al arrancar                                                         | API            | Pendiente (PR de administración) |
| Vigencia del código de recuperación                                                         | Vencimiento del código (RF-CA-10)                                                                        | API            | Pendiente (PR de contraseñas)    |

Notas:

- La vigencia del token de activación (RF-CA-15) es de **24 horas** y está fijada en el código (`User.ActivationTokenLifetime`), no es una variable de entorno.
- Para que el enlace del correo funcione con la API local, define en user-secrets de la API:

  ```bash
  dotnet user-secrets set "App:ActivationUrl" "http://localhost:5299/auth/activate" --project src/Host/Host.WebAPI
  ```

  Si tu API escucha en otro puerto (aparece en la consola al arrancar), ajústalo.

## Cómo ejecutar

```bash
# 1. Base de datos
docker compose up -d
docker compose ps                                 # debe decir "healthy"

# 2. API (aplica las migraciones al arrancar; Scalar en /scalar/v1)
dotnet run --project src/Host/Host.WebAPI

# 3. Proceso aparte que envía los correos pendientes (una sola pasada)
dotnet run --project src/Host/Host.Worker
```

## Ejecutar el proceso de la cola de correos

```bash
dotnet run --project src/Host/Host.Worker
```

Procesa los correos pendientes en una sola pasada y termina. El log dice cuántos se enviaron y cuántos fallaron; el código de salida es `0` si todo salió bien y `1` si alguno falló. Ejecutarlo de nuevo no reenvía los que ya salieron (ver RF-NOT-09 / RF-NOT-12 abajo).

## Cómo provocar cada criterio de aceptación

Las pruebas se hacen desde Scalar (`/scalar/v1`) y con consultas SQL en un cliente de PostgreSQL (por ejemplo DBeaver: host `localhost`, el puerto de `POSTGRES_PORT`, y la base, usuario y contraseña de tu `.env`). PostgreSQL distingue mayúsculas en los nombres con comillas, así que las consultas llevan comillas dobles.

Para obtener el enlace de activación sin esperar el correo, el cuerpo del mensaje también queda en la cola:

```sql
SELECT "To", "Status", "Body" FROM notifications."QueuedEmails" ORDER BY "CreatedAt" DESC;
```

### Registro y activación

Estado: implementado. Las pruebas de sesión (iniciar sesión antes y después de activar) se completan en el PR de sesión.

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

- [x] **RF-CA-14** — Registrar con una contraseña de menos de 8 caracteres, y con una sin números o sin letras → 400 con el error en el campo `Password`. También con un correo mal formado o vacío (error en `Email`) y con un JSON roto (400 controlado, no 500).

  ```
  POST /auth/register
  { "fullName": "Ana Pérez", "email": "ana2@correo.com", "password": "abc12" }
  ```

- [x] **RF-CA-15** — Registrar un usuario y comprobar que nace inactivo y que el correo sale por la cola.

  ```sql
  SELECT "Email", "ActivatedAt" FROM access_control."Users";   -- ActivatedAt vacío
  ```

  Luego ejecutar el Worker y confirmar que el correo llega de verdad. El rechazo de inicio de sesión de una cuenta inactiva se verifica en el PR de sesión.

- [x] **RF-CA-16** — Abrir el enlace de activación (`GET /auth/activate?userId=...&token=...`) → 200 y `ActivatedAt` con valor. Abrir el mismo enlace una segunda vez → 400 y el estado no cambia. Un `userId` mal formado (`?userId=abc`) también da 400 controlado. El inicio de sesión posterior se verifica en el PR de sesión.

- [x] **RF-CA-17** — Pedir reenvío con un correo que no existe y con uno pendiente → misma respuesta 200 en ambos casos. Tras el reenvío, abrir el **enlace anterior** → 400 (quedó invalidado).

  ```
  POST /auth/resend-activation
  { "email": "no-existe@correo.com" }
  ```

### Sesión

Estado: **pendiente** (PR de sesión). Las rutas y los pasos exactos se agregan al implementarlo.

- [ ] **RF-CA-03** — Iniciar sesión con credenciales correctas (recibe credencial de sesión) y con incorrectas (correo inexistente y contraseña incorrecta) → ambos rechazos con el mismo mensaje.
- [ ] **RF-CA-07** — Consultar el usuario autenticado sin sesión → rechazo. Con sesión válida → devuelve usuario y rol.
- [ ] **RF-CA-18** — Cerrar sesión y reutilizar la misma credencial → rechazo.
- [ ] **RF-CA-19** — Fallar el inicio de sesión 5 veces seguidas, luego intentar con la contraseña correcta durante el bloqueo → rechazo. Al vencer el bloqueo (15 minutos) y repetir con la correcta → funciona, contador en cero.

### Roles y administración de usuarios

Estado: **pendiente** (PR de administración).

- [ ] **RF-CA-04 / RF-CA-05** — Ubicar en el código el punto único donde cada operación declara el rol que exige. _(ruta por definir)_
- [ ] **RF-CA-06** — Con sesión de Estándar, construir a mano una petición a una operación de Administrador → rechazo explícito.
- [ ] **RF-CA-08** — Como Administrador, cambiar el rol de otro usuario → funciona. Como Estándar, intentar cambiar el propio rol o el de otro → rechazo.
- [ ] **RF-CA-20** — Como Administrador, desactivar un usuario con sesión abierta → esa sesión deja de servir y no puede iniciar sesión. Reactivarlo → vuelve a poder. Intentar que el Administrador se desactive a sí mismo → rechazo.
- [ ] **RF-CA-21** — Como Administrador, listar usuarios con rol y estado → sin hashes ni tokens. Como Estándar → rechazo.

> Cómo obtener el Administrador para estas pruebas: se documenta al implementar el seed (variables `Admin__Email` y `Admin__Password`).

### Contraseñas: recuperación, cambio y restablecimiento

Estado: **pendiente** (PR de recuperación de contraseña).

- [ ] **RF-CA-09** — Pedir recuperación con un correo inexistente y con uno existente → misma respuesta en ambos casos.
- [ ] **RF-CA-10** — Usar el código dos veces, y usarlo después de vencido → ambos rechazados, la contraseña no cambia.
- [ ] **RF-CA-11 / RF-CA-12** — Cambiar la contraseña con un código válido → la anterior deja de servir, y una credencial emitida antes del cambio queda rechazada.
- [ ] **RF-CA-13** — Como Administrador, forzar el restablecimiento de un usuario → la contraseña anterior deja de servir y llega por la cola el correo con el código nuevo.
- [ ] **RF-CA-22** — Con sesión, cambiar la propia contraseña con la actual incorrecta → rechazo. Con la actual correcta → funciona, aplicando RF-CA-14 y RF-CA-12.

### Correo por cola

Estado: implementado.

- [ ] **RF-NOT-08** — Apagar el acceso al servidor SMTP (por ejemplo, poner un `Smtp:Port` inválido como `9999` en los secretos del Worker) y registrar un usuario → la API responde 201 y el correo queda `Pending`.

  ```sql
  SELECT "To", "Status", "SentAt" FROM notifications."QueuedEmails" ORDER BY "CreatedAt" DESC;
  ```

- [ ] **RF-NOT-09 / RF-NOT-12** — Restaurar el puerto SMTP correcto y ejecutar el Worker → el log dice `Sent: 1` y el correo llega de verdad (`Status = Sent`, `SentAt` con valor). Ejecutarlo una segunda vez → `Sent: 0`, no se duplica ningún envío.

  ```bash
  dotnet run --project src/Host/Host.Worker
  ```

- [ ] **RF-NOT-13** — Confirmar que las credenciales no aparecen en el repositorio ni en su historial.

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

- [ ] **RD-09** — Reiniciar la aplicación (y, por separado, `docker compose down` seguido de `docker compose up -d`) → los usuarios registrados siguen existiendo.

  ```sql
  SELECT "Email", "ActivatedAt" FROM access_control."Users";
  ```

## Notas de diseño que afectan a la verificación

- **Token de activación:** en la base solo se guarda su hash (SHA-256); el valor real viaja únicamente en el enlace del correo. Por eso no se puede leer el enlace desde `access_control."Users"`, pero sí desde el cuerpo del correo en la cola.
- **Mismo rechazo para tokens inválidos:** un token inexistente, incorrecto, vencido o ya usado produce el mismo 400 con el mismo mensaje, igual que un usuario inexistente.
- **Reenvío de activación:** la respuesta es idéntica exista o no el correo, y esté o no activa la cuenta. El tiempo de respuesta puede diferir.
- **Correo por la cola:** el registro guarda al usuario y después encola el correo. Si el encolado fallara, el usuario queda sin correo y el reenvío lo resuelve.
- **Posible reenvío del mismo correo:** si el Worker se cae justo entre enviar y marcar `Sent`, ese correo se reenvía en la siguiente ejecución.
- **Enlace de activación:** hoy abre un endpoint de la API (`GET /auth/activate`). Con un frontend, se cambia `App__ActivationUrl` y se usa `POST /auth/activate`. Un antivirus o cliente de correo que abra el enlace antes que el usuario podría consumir el token.
- **Endpoint temporal:** `POST /test/enqueue-email` existe solo para probar la cola y se elimina más adelante.
- **Credenciales SMTP para el calificador:** no están en el repositorio. Se entregan por separado (comentario de la entrega en Moodle) y se revocan al terminar la calificación.

## Pull requests de esta pieza

Cada uno con las cuatro secciones (Qué cambia, Por qué, Cómo probarlo, Qué NO incluye):

- [x] Registro y activación (RF-CA-01, 02, 14, 15, 16, 17)
- [ ] Sesión (RF-CA-03, 07, 18, 19)
- [ ] Recuperación de contraseña (RF-CA-09 a 13, 22)
- [ ] Administración de usuarios (RF-CA-04, 05, 06, 08, 20, 21)
