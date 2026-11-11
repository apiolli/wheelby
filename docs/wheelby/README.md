# Módulo de negocio Wheelby

Asignación 3 (RF-NEG-01 a RF-NEG-06, RD-03, RD-04, RD-07, RD-08, RD-09).

Publicación de vehículos, búsqueda de disponibilidad, solicitud, confirmación, rechazo, inicio, completado y cancelación de reservas, y listados de reservas. Todo protegido por control de acceso y con persistencia en PostgreSQL (esquema `wheelby`).

## Requisitos previos

- .NET SDK 10 y Docker Desktop.
- Todos los comandos se ejecutan desde la carpeta `src/backend` (la que contiene `Wheelby.slnx`).
- La base de datos y los secretos se preparan siguiendo el README principal (PostgreSQL con Docker Compose y `dotnet user-secrets`).
- Al arrancar, la API aplica también las migraciones del módulo Wheelby (esquema `wheelby`, migración `AddVehicles`).

## Personas de la prueba

| Persona | Cómo obtenerla |
| ------- | -------------- |
| **Administrador** | `POST /auth/login` con `Admin__Email` y `Admin__Password` (los secretos de la API). Su token se llama **token de Administrador**. |
| **Anfitrión Estándar** | Registrarse en `POST /auth/register`, activar la cuenta y hacer `POST /auth/login`. Su token se llama **token de anfitrión**. |
| **Inquilino Estándar** | Igual que el anfitrión, otro correo. Su token se llama **token de inquilino**. |

Un Estándar **no** puede publicar hasta que un Administrador le apruebe el permiso `wheelby.publish-vehicle` en Gestión de permisos. El rol `Administrator` no opera como miembro: recibe 403 en las rutas de `/vehicles` y `/reservations`.

## Rutas

| Ruta | Grupo y política | Qué hace |
| ---- | ---------------- | -------- |
| `POST /vehicles` | `/vehicles`, `RequireStandard` | F1: publica un vehículo. 201 con `{ "id" }`. |
| `GET /vehicles/available?from=&to=&page=&pageSize=&currency=` | `/vehicles`, `RequireStandard` | F2: lista vehículos disponibles paginados. 200. `currency` (opcional) convierte los precios, que están en DOP. |
| `POST /reservations` | `/reservations`, `RequireStandard` | F3: solicita una reserva. 201 con `{ "id" }`. |
| `GET /reservations/mine?as=renter\|host&page=&pageSize=` | `/reservations`, `RequireStandard` | F6: lista las reservas del usuario (como inquilino o anfitrión). 200. |
| `POST /reservations/{id}/confirm` | `/reservations`, `RequireStandard` | F4: confirma una reserva. 204. |
| `POST /reservations/{id}/reject` | `/reservations`, `RequireStandard` | F4: rechaza una reserva (`{ "reason": "..." }`). 204. |
| `POST /reservations/{id}/start` | `/reservations`, `RequireStandard` | F5: inicia el alquiler. 204. |
| `POST /reservations/{id}/complete` | `/reservations`, `RequireStandard` | F5: completa el alquiler. 204. |
| `POST /reservations/{id}/cancel` | `/reservations`, `RequireStandard` | F5: cancela una reserva (`{ "reason": "..." }`). 204. |
| `GET /admin/reservations?status=&page=&pageSize=` | `/admin/reservations`, `RequireAdministrator` | F6: lista todas las reservas, opcionalmente filtradas por estado. 200. |

En el borde HTTP las fechas llegan como `DateTimeOffset` y se convierten a `DateTime` UTC antes de crear el comando.

Sin sesión → **401**. Con sesión pero sin el rol del grupo → **403**.

## Cómo provocar cada criterio de aceptación

Las pruebas se hacen desde Scalar (`/scalar/v1`) y con consultas SQL en un cliente de PostgreSQL (por ejemplo DBeaver: host `localhost`, el puerto de `POSTGRES_PORT`, y la base, usuario y contraseña de tu `.env`). PostgreSQL distingue mayúsculas en los nombres con comillas, así que las consultas llevan comillas dobles.

Para autenticarte en Scalar: ejecuta `POST /auth/login`, copia el `accessToken` de la respuesta y pégalo en el botón de autorización (esquema Bearer) de Scalar.

### F1 — Publicar vehículo

- [x] **RF-NEG-01** — Con el **token de anfitrión**, antes de tener el permiso:

  ```
  POST /vehicles
  { "brand": "Toyota", "model": "Corolla", "year": 2020, "plate": "ABC-123", "pricePerDay": 15000 }
  ```

  Responde **403** ("No tienes el permiso wheelby.publish-vehicle para publicar vehículos.").

- [x] El Administrador aprueba el permiso. El anfitrión crea `POST /permissions/requests` con `{ "permissionCode": "wheelby.publish-vehicle" }` → **201**. El Administrador aprueba con `POST /admin/permissions/requests/{id}/approve` → **204**.

- [x] Ahora `POST /vehicles` con el mismo cuerpo → **201** con `{ "id": "<guid>" }`. En la base:

  ```sql
  SELECT "Id", "OwnerId", "Brand", "Model", "Year", "Plate", "PricePerDay", "CreatedAt"
  FROM wheelby."Vehicles"
  WHERE "Plate" = 'ABC-123';
  ```

  La placa queda normalizada a mayúsculas (`ABC-123`).

- [x] Repetir `POST /vehicles` con la misma placa → **409** ("Ya existe un vehículo con esa placa.").
- [x] Con el **token de Administrador**, `POST /vehicles` → **403** (el Administrador no opera como miembro).
- [x] Sin token, `POST /vehicles` → **401**.

### F2 — Buscar vehículos disponibles

- [x] **RF-NEG-02** — Con el **token de inquilino**:

  ```
  GET /vehicles/available?from=2026-11-05T10:00:00Z&to=2026-11-07T10:00:00Z&page=1&pageSize=20
  ```

  Responde **200** con `items`, `page`, `pageSize`, `totalItems`, `totalPages`, `hasPreviousPage`, `hasNextPage`. Cada item trae `id`, `ownerId`, `brand`, `model`, `year`, `plate`, `pricePerDay`, `createdAt`.

  - `from >= to` → **400**.
  - Rango mayor a 365 días → **400**.
  - Los vehículos del propio inquilino y los que tienen una reserva bloqueante (`Confirmed` o `InProgress`) que solapen el rango no aparecen.

- [x] **RD-10** — Precio en otra moneda. Los precios publicados están en **pesos dominicanos (DOP)** y cada item trae `currency: "DOP"`. Con el parámetro opcional `currency` (3 letras, se normaliza a mayúsculas):

  ```
  GET /vehicles/available?from=2026-11-05T10:00:00Z&to=2026-11-07T10:00:00Z&page=1&pageSize=20&currency=USD
  ```

  Cada item agrega `convertedPricePerDay` (redondeado a 2 decimales), `convertedCurrency` y `exchangeRate`, con la tasa DOP→USD de ExchangeRate-API (una sola consulta por búsqueda).

  - Sin `currency`, o con `DOP`, no se llama a la API externa y los tres campos quedan en `null`.
  - `currency` que no son 3 letras (`US`, `USDX`, `12$`) → **400**.
  - Moneda que el proveedor no soporta → **400** ("La moneda XYZ no está soportada.").
  - Sin `ExchangeRate__ApiKey`, con una clave inválida o con la API caída → **503** con un mensaje controlado. Ver la sección «API externa» del README principal.

### F3 — Solicitar reserva

- [x] **RF-NEG-02** — Con el **token de inquilino** y un `vehicleId` publicado por el anfitrión:

  ```
  POST /reservations
  { "vehicleId": "<guid del vehículo>", "start": "2026-11-05T10:00:00Z", "end": "2026-11-07T10:00:00Z" }
  ```

  Responde **201** con `{ "id": "<guid>" }`. La reserva nace en `Pending`.

  - Vehículo inexistente → **404** ("El vehículo no existe.").
  - Solicitar el propio vehículo (inquilino = dueño) → **403** ("No puedes reservar tu propio vehículo.").
  - Solape con una reserva bloqueante ya existente → **409** ("El vehículo ya tiene una reserva bloqueante que solapa esas fechas.").

### F4 — Confirmar y rechazar reserva

- [x] **RF-NEG-02** — Con el **token de anfitrión** (dueño del vehículo) sobre una reserva `Pending`:

  ```
  POST /reservations/<id>/confirm
  ```

  Responde **204**. La reserva pasa a `Confirmed`. Al confirmar se vuelve a comprobar el solape bloqueante excluyendo la propia reserva.

  - Con un token que no sea el dueño del vehículo → **403** ("Solo el dueño del vehículo puede confirmar la reserva.").
  - Reserva inexistente → **404** ("La reserva no existe.").

- [x] **RF-NEG-02** — Rechazar:

  ```
  POST /reservations/<id>/reject
  { "reason": "No disponible en esas fechas" }
  ```

  Responde **204**. La reserva pasa a `Cancelled` con el motivo.

  ```
  POST /reservations/<id>/reject
  { "reason": "" }
  ```

  → **400** ("El motivo del rechazo es obligatorio.") y la reserva no cambia.

### F5 — Ciclo del alquiler

- [x] **RF-NEG-02** — Iniciar alquiler (solo el dueño, actor `Host`):

  ```
  POST /reservations/<id>/start
  ```

  Responde **204**. La reserva pasa a `InProgress`. Requiere que `utcNow` esté entre `StartDate` y `EndDate`.

- [x] Completar alquiler (solo el dueño, actor `Host`):

  ```
  POST /reservations/<id>/complete
  ```

  Responde **204**. La reserva pasa a `Completed`. Requiere que `utcNow >= EndDate`.

- [x] Cancelar reserva (actor `Renter` si es el inquilino, `Host` si es el dueño):

  ```
  POST /reservations/<id>/cancel
  { "reason": "Cambio de planes" }
  ```

  Responde **204**. La reserva pasa a `Cancelled`.

  - Un usuario que no participa (ni inquilino ni dueño) → **403** ("No participas en esta reserva, no puedes cancelarla.").

- [x] **Transición prohibida explícita** — Cancelar un alquiler en curso (`InProgress → Cancelled`) → **409** ("Transición InProgress → Cancelled prohibida: Un alquiler en curso no se cancela; debe completarse.").

### Preparar fechas con SQL para probar sin esperar

`Confirmed → InProgress` requiere `StartDate <= utcNow <= EndDate`, e `InProgress → Completed` requiere `utcNow >= EndDate`. Para probarlas sin esperar, ajusta las fechas en la base:

```sql
UPDATE wheelby."Reservations"
SET "StartDate" = '2026-11-01T10:00:00Z',
    "EndDate" = '2026-11-01T12:00:00Z'
WHERE "Id" = '<id de la reserva>';
```

Con `StartDate` en el pasado y `EndDate` en el futuro respecto a `utcNow`, `POST /reservations/<id>/start` pasa a `InProgress`. Con `EndDate` en el pasado, `POST /reservations/<id>/complete` pasa a `Completed`.

### F6 — Listados de reservas

- [x] **RF-NEG-02** — Con el **token de inquilino**:

  ```
  GET /reservations/mine?as=renter&page=1&pageSize=20
  ```

  Responde **200** con solo las reservas donde el inquilino es el `renterId`.

- [x] Con el **token de anfitrión**:

  ```
  GET /reservations/mine?as=host&page=1&pageSize=20
  ```

  Responde **200** con solo las reservas de vehículos cuyo dueño es el anfitrión (`ownerId`).

  - `as` distinto de `renter` o `host` → **400**.

- [x] **RF-NEG-02** — Con el **token de Administrador**:

  ```
  GET /admin/reservations?status=Confirmed&page=1&pageSize=20
  ```

  Responde **200** con todas las reservas del estado indicado (o todas si no se indica `status`).

  - Con el **token de Estándar**, `GET /admin/reservations` → **403**.

## Notas de diseño

- **Un solo lugar para los estados (RD-04):** todos los estados y transiciones de la reserva viven en `ReservationStateMachine.AllowedTransitions`; `Reservation.TransitionTo` es el único código que cambia `Status`. No hay validaciones de estado fuera de la máquina.
- **Dos defensas intencionales:** el handler comprueba la identidad (¿es el dueño o el inquilino?) y la máquina comprueba el tipo de actor (`Host`/`Renter`). No se unifican.
- **El dominio no lee el reloj ni el usuario:** recibe `utcNow` por parámetro.
- **Los errores no exponen detalles internos (RD-08):** `GlobalExceptionHandler` convierte las excepciones de dominio en HTTP sin detalles internos.
- **Publicar un vehículo depende del permiso `wheelby.publish-vehicle`**, comprobado solo por `IPermissionChecker` (RF-NEG-06).
- **Datos de Wheelby persisten tras reiniciar la API (RD-09):** migración `AddVehicles` aplicada al arrancar.

## Fuera de alcance de esta práctica

Notificaciones (RF-NEG-07), reportes (RF-NEG-08), adjuntar documentos (RF-NEG-10), verificación de identidad del anfitrión (`MemberVerification`, cédula, hash), fotos de vehículos, cálculo de totales o precio congelado en la reserva, y restricción `EXCLUDE` de PostgreSQL contra solapamientos concurrentes (quedan como limitación documentada).
