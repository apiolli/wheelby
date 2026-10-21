# Máquina de estados — Reserva (Wheelby)

Estructura declarada en la semana 4 (Práctica 1, sección 1.6). Las pruebas llegan en la semana 8.
Código: `src/backend/src/Domain/Wheelby/Wheelby.Domain/Reservations/`.

## Estados (RF-NEG-03)

Un único lugar: `ReservationStatus.cs`. Cinco estados, el máximo permitido.

| Estado | Significado |
|---|---|
| `Pending` | El inquilino solicitó la reserva; pendiente de evaluación del anfitrión. Estado inicial. |
| `Confirmed` | El anfitrión aceptó la reserva; el alquiler aún no comenzó. |
| `InProgress` | El alquiler está en curso. |
| `Completed` | El alquiler terminó. Terminal. |
| `Cancelled` | La reserva se canceló (incluye el rechazo del anfitrión, con motivo obligatorio). Terminal. |

El rechazo del anfitrión es una cancelación con motivo obligatorio, en lugar de un sexto estado.

## Actores

`ReservationActor.cs`: quién puede ejecutar cada transición.

| Actor | Quién es |
|---|---|
| `Renter` | El inquilino que solicita la reserva. |
| `Host` | El anfitrión, dueño del vehículo. Perfil de negocio de Wheelby, no un rol del Core. |

La máquina verifica el **tipo** de actor. Comprobar que un usuario concreto es el dueño del vehículo o el inquilino de esa reserva será trabajo de los casos de uso (no incluido aquí).

## Tabla de transiciones (RD-04)

Único lugar: `ReservationStateMachine.AllowedTransitions`. Idéntica al código. Agregar una transición es agregar una entrada; `Reservation.TransitionTo` es el único código que cambia `Status`.

| Desde | Hacia | Quién la ejecuta | Condición |
|---|---|---|---|
| `Pending` | `Confirmed` | `Host` | El anfitrión evaluó al inquilino (decisión humana, descriptiva). Guarda: `utcNow <= StartDate` (no confirmar algo ya vencido). |
| `Pending` | `Cancelled` | `Renter`, `Host` | Motivo obligatorio si cancela el anfitrión; solo antes del inicio (`utcNow < StartDate`). |
| `Confirmed` | `InProgress` | `Host` | El alquiler comienza; solo desde la fecha de inicio (`StartDate <= utcNow <= EndDate`). |
| `Confirmed` | `Cancelled` | `Renter`, `Host` | Motivo obligatorio si cancela el anfitrión; solo antes del inicio (`utcNow < StartDate`). |
| `InProgress` | `Completed` | `Host` | El alquiler terminó (`utcNow >= EndDate`). |

Las comprobaciones con la fecha de inicio son guardas de cada transición (`Func` en la misma tabla). Los métodos de dominio reciben `utcNow` por parámetro (RD-11). Las fechas se comparan en UTC; la zona horaria del usuario se tratará en la interfaz.

## Transición explícitamente prohibida (RF-NEG-04)

`ReservationStateMachine.ExplicitlyForbidden`:

| Desde | Hacia | Motivo |
|---|---|---|
| `InProgress` | `Cancelled` | Un alquiler en curso no se cancela; debe completarse. |

Cualquier otra combinación fuera de la tabla también se rechaza. Si la máquina rechaza, el estado no cambia.

## Estados terminales (RF-NEG-05)

`ReservationStateMachine.TerminalStates`: `Completed` y `Cancelled`. Ninguna transición parte de ellos.

## Rechazos

| Situación | Excepción | HTTP |
|---|---|---|
| Datos inválidos al crear (fechas no UTC, `StartDate >= EndDate`, inicio no futuro, `Guid` vacío) | `InvalidReservationDataException` | 400 |
| El actor no está en la lista de la transición, o no se cumple su condición (motivo, fechas) | `ReservationTransitionNotAllowedException` | 403 |
| Par (origen, destino) fuera de la tabla, transición prohibida explícita, u origen terminal | `InvalidReservationTransitionException` | 409 |

## Diagrama

```mermaid
stateDiagram-v2
    [*] --> Pending
    Pending --> Confirmed : Host\nevaluó al inquilino
    Pending --> Cancelled : Renter / Host\n(motivo si Host)
    Confirmed --> InProgress : Host\ndesde StartDate
    Confirmed --> Cancelled : Renter / Host\n(motivo si Host)
    InProgress --> Completed : Host\ntras EndDate
    Completed --> [*]
    Cancelled --> [*]
    note right of InProgress : InProgress → Cancelled\nPROHIBIDA
```

## Notas de diseño

- **Un solo lugar (RD-04):** estados en `ReservationStatus`, tabla en `ReservationStateMachine.AllowedTransitions`. Buscar asignaciones a `Status` en `src/` solo encuentra la creación y `Reservation.TransitionTo`.
- **El Core no depende del negocio (RD-03):** ningún proyecto de `Core/` ni de `Shared/` referencia `Wheelby.*`; solo `Host.WebAPI` referencia `Wheelby.Infrastructure`. `VehicleId` y `RenterId` son `Guid` sin clave foránea hacia otros esquemas; la integridad entre módulos se garantizará en los casos de uso.
- **Persistencia:** esquema propio `wheelby`, tabla `Reservations` (`Status` guardado como texto), migración `InitialReservations` aplicada al arrancar por `Host.WebAPI`.

---

# Máquina de estados — Permisos (Core)

Código: `src/backend/src/Core/Permissions/Permissions.Domain/PermissionRequests/`.

## Estados (RF-GP-01)

Un único lugar: `PermissionRequestStatus.cs`. Cuatro estados.

| Estado | Significado |
|---|---|
| `Pending` | El usuario solicitó un permiso; pendiente de que un Administrador lo resuelva. Estado inicial. |
| `Approved` | Un Administrador aprobó la solicitud. Estado intermedio: la aplicación es automática. |
| `Applied` | El permiso quedó concedido (`PermissionGrants`). Terminal. |
| `Rejected` | Un Administrador rechazó la solicitud, con motivo obligatorio. Terminal. |

## Actores

`PermissionActor.cs`: quién puede ejecutar cada transición.

| Actor | Quién es |
|---|---|
| `Administrator` | Un usuario con rol Administrador. La máquina verifica el **tipo** de actor; que el usuario concreto tenga ese rol lo exige el grupo de rutas `/admin/permissions` (política `RequireAdministrator`). |
| `System` | La propia aplicación, al aplicar la aprobación en la misma operación. |

## Tabla de transiciones (RD-04)

Único lugar: `PermissionRequestStateMachine.AllowedTransitions`. `PermissionRequest.TransitionTo` es el único código que cambia `Status`.

| Desde | Hacia | Quién la ejecuta | Condición |
|---|---|---|---|
| `Pending` | `Approved` | `Administrator` | Guarda: `resolverId != RequesterId` (nadie resuelve su propia solicitud). |
| `Approved` | `Applied` | `System` | Automática, en la misma operación que la aprobación. |
| `Pending` | `Rejected` | `Administrator` | Motivo no vacío y `resolverId != RequesterId`. |

Los métodos de dominio reciben `utcNow` por parámetro (RD-11) y no leen el reloj. `Approve`, `Reject` y `MarkApplied` son métodos de conveniencia que solo llaman a `TransitionTo`.

## Transición explícitamente prohibida (RF-GP-06)

`PermissionRequestStateMachine.ExplicitlyForbidden`:

| Desde | Hacia | Motivo |
|---|---|---|
| `Rejected` | `Applied` | Una solicitud rechazada no se puede aplicar. |

Cualquier otro par fuera de la tabla también se rechaza. Si la máquina rechaza, el estado no cambia y tampoco cambian `ApproverId`, `ResolvedAt` ni `RejectionReason`.

## Estados terminales (RF-GP-06)

`PermissionRequestStateMachine.TerminalStates`: `Applied` y `Rejected`. Ninguna transición parte de ellos.

## Rechazos

| Situación | Excepción | HTTP |
|---|---|---|
| Datos inválidos al crear (código mal formado, ids vacíos, fecha no UTC) | `InvalidPermissionRequestDataException` | 400 |
| Rechazo sin motivo | `RejectionReasonRequiredException` | 400 |
| Resolver la propia solicitud | `CannotResolveOwnPermissionRequestException` | 403 |
| El actor no puede ejecutar la transición | `PermissionRequestTransitionNotAllowedException` | 403 |
| Par (origen, destino) fuera de la tabla, transición prohibida explícita u origen terminal | `InvalidPermissionRequestTransitionException` | 409 |

## Diagrama

```mermaid
stateDiagram-v2
    [*] --> Pending
    Pending --> Approved : Administrator\n(no es el solicitante)
    Pending --> Rejected : Administrator\n(motivo obligatorio)
    Approved --> Applied : System\n(misma operación)
    Approved --> [*]
    Rejected --> [*]
    note right of Rejected : Rejected → Applied\nPROHIBIDA
```

## Notas de diseño

- **Un solo lugar (RD-04):** estados en `PermissionRequestStatus`, tabla en `PermissionRequestStateMachine.AllowedTransitions`. Buscar asignaciones a `Status` en `src/Core/Permissions/` solo encuentra la creación y `PermissionRequest.TransitionTo`.
- **El Core es independiente (RD-03):** `Permissions.*` no referencia `Wheelby.*` ni ningún otro módulo de negocio; solo conversa con `AccessControl.Contracts` (`ICurrentUser`, `AccessControlPolicies`).
- **Persistencia:** esquema propio `permissions`, tablas `PermissionRequests` (`Status` guardado como texto) y `PermissionGrants`, migraciones aplicadas al arrancar por `Host.WebAPI`.
- **Concurrencia:** `PermissionRequests` usa la columna de sistema `xmin` de PostgreSQL como token de concurrencia; una actualización simultánea se rechaza con 409.
- **Aplicación automática:** la transición `Approved → Applied` no tiene ruta HTTP: ocurre dentro de la misma operación que aprueba, junto con la creación de `PermissionGrant`.
