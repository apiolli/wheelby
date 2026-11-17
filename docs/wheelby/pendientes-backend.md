# Wheelby — Ajustes pendientes del backend (módulo de negocio)

> Estado al 2026-11-17: Prioridades 1 a 4 hechas. Solo queda la convención de fechas (Prioridad 2), pendiente de decisión.

Lista de cambios recomendados al módulo Wheelby, surgida de contrastar el diseño del frontend con la API actual. Está ordenada por prioridad. **Auditoría y reportes del Core no entran aquí:** están pendientes aparte y no se mezclan con este documento.

Estado de referencia: máquina de estados de reservas implementada (Pending, Confirmed, InProgress, Completed, Cancelled), publicación de vehículos con el permiso `wheelby.publish-vehicle`, búsqueda por fechas, listados de reservas y errores en `problem+json`.

## Ya verificado (no requiere cambios)

- [x] `Confirm` **revalida el solapamiento** con `HasBlockingOverlapAsync(..., excludeReservationId)` antes de confirmar. Dos solicitudes Pending sobre las mismas fechas pueden existir, pero solo una se confirma; la otra recibe 409.
- [x] El solape se define como `start < r.EndDate && end > r.StartDate`: dos reservas consecutivas (una termina cuando empieza otra) no chocan. Solo Confirmed e InProgress bloquean.
- [x] Los listados devuelven `PagedResult<T>(Items, Page, PageSize, TotalItems)`.
- [x] Los errores de validación son por campo (`HttpValidationProblemDetails`) y todo `problem+json` lleva `traceId`.
- [x] ~~Limitación conocida~~: dos confirmaciones **simultáneas** pueden pasar ambas la verificación. El cierre correcto es una restricción `EXCLUDE` de PostgreSQL (ver Prioridad 4). *Cerrada con la migración `NoBlockingOverlap`.*

## Prioridad 1 — el frontend no puede cerrar el flujo sin esto

- [x] **`GET /reservations/{id}`** — hoy solo hay listados. El detalle de reserva no tiene de dónde leer. Debe permitir ver la reserva al inquilino, al dueño del vehículo y al Administrador; para cualquier otro, 404 (no revelar existencia). *Hecho: el Administrador lo ve en `GET /admin/reservations/{id}`, porque el grupo `/reservations` exige `RequireStandard`.*
- [x] **`GET /vehicles/{id}`** — detalle de un vehículo. Lo ve cualquier usuario con sesión. *Hecho para Estándar; el Administrador no entra al grupo `/vehicles`.*
- [x] **`GET /vehicles/mine`** — vehículos del anfitrión autenticado, paginados. Sin esto el anfitrión no puede ver lo que publicó.
- [x] **Enriquecer `ReservationSummaryDTO`** con: `Brand`, `Model`, `Year`, `Plate`, `OwnerId`, `PricePerDay`, `TotalAmount` y `CancellationReason`. Hoy el cliente necesita un segundo fetch por tarjeta y calcula el total por su cuenta. *Hecho; además trae `Days` y `Currency` (`"DOP"`).*
- [x] **Congelar el precio en la reserva:** `PricePerDayAtBooking` y `TotalAmount` se guardan al crearla (`Reservation.Create`). Si el anfitrión cambia el precio después, el total de reservas ya hechas no debe cambiar. *Hecho: migración `FreezeReservationPrice` (columnas `PricePerDayAtBooking`, `Days`, `TotalAmount`). Las reservas previas se rellenan con el precio actual del vehículo. Cada fracción de 24 h cuenta como un día.*
- [x] **`GET /vehicles/{id}/availability?from=&to=`** — devuelve los rangos bloqueados (Confirmed e InProgress) para que el calendario no descubra el conflicto recién con un 409.

## Prioridad 2 — contrato de la API

- [x] **Código de error estable** en el `problem+json` (`extensions.code`), junto al `traceId`. Ejemplos: `vehicle.overlap`, `reservation.invalid-transition`, `reservation.own-vehicle`, `reservation.not-participant`. El frontend no debe depender del texto en español. *Hecho: `AppException.Code` + `WheelbyErrorCodes`; tabla de códigos en `docs/wheelby/README.md`.*
- [x] **Enums como texto.** Hoy `ReservationStatus` viaja como número. Registrar `JsonStringEnumConverter` evita que reordenar el enum rompa a los clientes. *Hecho; el tipo del frontend ya usa los nombres.*
- [x] **Tasa de cambio con caché y respaldo.** Cachear la tasa (`IMemoryCache`, unos 30 minutos). Si la API externa falla, responder en DOP con `ConvertedPricePerDay = null`, nunca con un 500. *Hecho: la caché ya existía (`ExchangeRate__CacheMinutes`, 60 por defecto); ahora un 503 del proveedor responde 200 en DOP. Una moneda no soportada sigue siendo 400.*
- [ ] **Convención de fechas.** Decidir y documentar si una reserva es por día (`DateOnly`) o por instante UTC con hora fija de entrega. Hoy las reglas comparan con `utcNow`; con usuarios en UTC-4, una reserva "del 20 al 22" puede empezar el 19 a las 20:00 hora local. **Pendiente de decisión.** Nota: el total ya cobra por bloques de 24 h, coherente con cualquiera de las dos opciones si la hora de entrega es fija.

## Prioridad 3 — requisitos del negocio que hoy están vacíos

- [x] **Notificaciones por transición (RF-NEG-07).** Cada cambio de estado (solicitada, confirmada, rechazada, cancelada, iniciada, completada) publica un evento que el módulo de notificaciones convierte en correo. El Worker y la cola ya existen. *Hecho: evento `ReservationStatusChanged` publicado tras guardar; el correo va a la contraparte de quien actuó (a los dos si actúa el sistema). Directorio de correos: `IUserDirectory` en AccessControl.Contracts. No es outbox transaccional (ver notas de diseño).*
- [x] **Documentos adjuntos a la reserva (RF-NEG-10).** Tabla `ReservationDocuments(ReservationId, DocumentId, Kind)` con `Kind = Entrega | Devolución`. Reemplaza el módulo de inspecciones con una sola tabla y respeta RD-03: Documents no conoce Wheelby, Wheelby solo guarda los `DocumentId`. *Hecho: `POST/GET /reservations/{id}/documents` y `.../documents/{documentId}/content`. Solo documentos propios, en `Confirmed`, `InProgress` o `Completed`. Lectura vía `IDocumentAccess` (Documents.Contracts).*
- [x] **Historial de estados de la reserva.** `ReservationStatusHistory(ReservationId, From, To, ActorId, Reason, At)` solo como dato del negocio, para la línea de tiempo del detalle. No sustituye ni adelanta la auditoría del Core. *Hecho: tabla `ReservationStatusHistory` escrita por `Reservation.TransitionTo`; `GET /reservations/{id}/history`. Las reservas previas se rellenan en la migración.*

## Prioridad 4 — si sobra tiempo

- [x] **Editar y desactivar vehículos:** `PUT /vehicles/{id}` e `IsActive`. Solo el dueño; placa única; no se puede desactivar con reservas Confirmed o InProgress. *Hecho: `PUT /vehicles/{id}`, `POST /vehicles/{id}/deactivate` y `/activate`. Inactivo: fuera de la búsqueda y sin reservas ni confirmaciones nuevas.*
- [x] **Más datos del vehículo:** `City`, `Type` (enum corto) y `Description`, con sus filtros en `GET /vehicles/available`. *Hecho: `Type` (`Car`, `Motorcycle`, `Bicycle`, `Scooter`, `Other`), `City` y `Description` opcionales; filtros `city` y `type`.*
- [x] **Fotos de vehículos.** `Document.CanBeAccessedBy` solo deja ver al dueño o al Administrador, así que un inquilino no vería las fotos. Solución sin tocar el Core: tabla `VehiclePhotos(VehicleId, DocumentId)` en Wheelby y `GET /vehicles/{id}/photos/{documentId}`, que valida la sesión y lee el archivo a través de una interfaz del módulo Documents. *Hecho como se propuso, más `POST`/`DELETE` y listado. Máximo 10, solo imágenes propias.*
- [x] **Expirar solicitudes.** Job del Worker que pase a Cancelled las reservas Pending cuya `StartDate` ya pasó (hoy `Confirm` exige `utcNow <= StartDate`, así que quedan colgadas). *Hecho: `ReservationExpiryWorker` en el Worker; actor `System` agregado a la fila `Pending → Cancelled` de la máquina (sigue habiendo 5 filas).*
- [x] **Restricción `EXCLUDE` en PostgreSQL** (requiere `btree_gist`) para cerrar la carrera entre confirmaciones simultáneas: *Hecho: migración `NoBlockingOverlap`; la violación se traduce a 409 `vehicle.overlap`.*

  ```sql
  ALTER TABLE wheelby."Reservations"
    ADD CONSTRAINT no_blocking_overlap
    EXCLUDE USING gist (
      "VehicleId" WITH =,
      tstzrange("StartDate", "EndDate") WITH &&
    ) WHERE ("Status" IN ('Confirmed', 'InProgress'));  -- Status se guarda como texto (HasConversion<string>)
  ```

- [x] **Reseñas:** una por actor por reserva Completed, no editables. *Hecho: `POST/GET /reservations/{id}/reviews` y `GET /vehicles/{id}/reviews`; índice único (reserva, autor) y calificación 1-5 con `CHECK`.*

## Fuera de este documento

- Auditoría y reportes del Core (pendientes aparte).
- Pagos, depósitos, seguros y mensajería entre usuarios.

## Orden sugerido

1. Prioridad 1 completa: desbloquea el detalle de reserva, la ficha y "mis vehículos".
2. Código de error estable y enums como texto, antes de que el frontend se acople más al formato actual.
3. Notificaciones y documentos de la reserva.
4. Lo demás según el tiempo disponible.
