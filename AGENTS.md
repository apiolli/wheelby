# Contexto para agentes: Wheelby

Documento de traspaso para cualquier agente que trabaje en este repositorio. Léelo completo antes de tocar código. Si algo de aquí contradice al código, manda el código: verifícalo y **avísale al estudiante**. El agente no edita este archivo (sección 2.3).

> **Tarea actual: Práctica 5.** Notificaciones, cola de correos completa y reportes con agregación. Antes de escribir código lee `docs/criterios-notificaciones-reportes.md` (el contrato con el que se acepta el trabajo) y `docs/practica-5/tarea-agente.md` (diseño, rutas y orden de commits). Ver sección 12.

Estado al 26 de noviembre de 2026, sobre `main` en `a1bb16a`.

---

## 1. Qué es

Wheelby es un marketplace de alquiler de vehículos entre particulares, proyecto individual del curso **Programación III · ITLA · 2026-C-3**. Se entrega por prácticas, cada una con una **etiqueta de git** que el profesor evalúa.

- **Backend:** C# .NET 10, monolito modular con Clean Architecture, CQRS (MediatR), DDD y FluentValidation, sobre PostgreSQL 17 con EF Core 10 (Npgsql).
- **Frontend:** React 19 + Vite + TypeScript en `src/frontend`, con Tailwind 4, TanStack Query, Zustand, React Router 8 y Zod.
- **Ejecución:** todo el backend corre con `docker compose up --build` desde la raíz. No se usa `dotnet run` ni `user-secrets`.
- **Idioma:** código en inglés; mensajes al usuario, documentación y commits en español.

---

## 2. Reglas que no se rompen

### 2.1 Git y entrega

- **Autoría:** los commits van a nombre del estudiante (`apiolli <cesardiaz.miduho@gmail.com>`). **Sin trailers** (`Co-Authored-By`, `Claude-Session`) ni menciones a agentes en commits o PR.
- **Formato del mensaje:** `REQUISITO: mensaje`, primera línea de **50 caracteres o menos**, en español, minúsculas, **sin tildes**, sin paréntesis ni comillas, en presente y sin punto final. Ejemplos: `RF-DOC-01: agrega entidad Document`, `RD-09: alinea EF Core Relational en 10.0.12`. Si hace falta explicar, va en el cuerpo.
- **Un cambio lógico por commit**, y cada commit compila con las pruebas en verde.
- **Nunca `--force`**, nunca reescribir historia publicada y **nunca mover ni borrar etiquetas ya subidas** (`asignacion-*`, `practica-*`). Se evalúa el punto etiquetado.
- No hacer push a `main` directamente: se trabaja en una rama y se integra con PR. Al fusionar, usar **"Create a merge commit"** o **"Squash and merge"**; "Rebase and merge" cambia los hashes y confunde el historial.
- **Práctica 5: solo "Create a merge commit", nunca squash.** El commit de `AGENTS.md` y los criterios tiene que quedar en el historial **antes** del primer commit de notificaciones, y un squash lo fundiría con el código. Por eso la documentación va en un PR propio que se fusiona primero, y el código en otro PR posterior.
- El agente **no hace push ni crea etiquetas**: eso lo hace el estudiante.
- No borrar ramas existentes (`prueba/a`, `prueba/b`, `temp/defecto-2`, `feature/*`).

### 2.2 Reglas de diseño (RD)

| Regla | Qué exige |
|---|---|
| RD-03 | El Core nunca referencia a Wheelby. Wheelby usa el Core solo por sus `*.Contracts`. |
| RD-04 | Los estados y transiciones de la reserva viven en un solo lugar: `ReservationStateMachine`. |
| RD-06 | La autorización se decide en el servidor: política en el grupo de rutas y regla de acceso en el dominio o el handler. |
| RD-07 | Se validan todas las entradas: FluentValidation para la forma, dominio para las reglas. Rechazo controlado, nunca excepción sin manejar. |
| RD-08 | Los errores no exponen detalles internos. `GlobalExceptionHandler` traduce `AppException` a `problem+json` con `code` y `traceId`. |
| RD-09 | Persistencia en PostgreSQL con migraciones que aplica **solo la API** al arrancar. El Worker no migra. |
| RD-10 | Secretos solo por variables de entorno desde `.env`, que no se versiona. Nada real en código, `Dockerfile`, compose, README ni commits. |
| RD-11 | Fechas en UTC. El dominio recibe `utcNow` por parámetro y nunca lee el reloj. |
| RD-12 | Las pruebas no levantan la aplicación ni necesitan PostgreSQL. |

### 2.3 Restricciones para el agente

**Secretos**

- **Nunca leer, abrir, imprimir, copiar, buscar dentro de, ni modificar `.env` ni `src/frontend/.env`.** Tampoco con `cat`, `grep`, `type`, `Get-Content` ni desde un script. Para saber qué variables existen, lee `.env.example` y la sección 8 del README.
- **Nunca escribir credenciales** (`Smtp__*` reales, `Jwt__Key`, `Admin__Password`, `POSTGRES_PASSWORD`, `ExchangeRate__ApiKey`) en código, pruebas, documentación, `.env.example`, mensajes de commit, descripciones de PR ni salidas de comandos que se peguen en un documento. En `.env.example` van solo valores de ejemplo (`change_me`, `smtp.example.com`).
- Si una tarea parece exigir un secreto, detente y pídeselo al estudiante; él lo pone en su `.env`.

**Partes que no se tocan**

- Las **migraciones existentes** de cualquier módulo. Los cambios de esquema van siempre en una migración nueva.
- `ReservationStateMachine.cs` y `PermissionRequestStateMachine.cs`: su tabla de transiciones, los estados terminales y las prohibidas.
- Las **pruebas existentes** de `Documents.Test`, `Permissions.Test` y `Wheelby.Test`: no se editan ni se borran, solo se agregan nuevas. La única excepción en la Práctica 5 son las pruebas que comprueban que los avisos de reserva usaban `IEmailQueue`, que se adaptan al motor nuevo y se listan en el PR.
- `src/frontend/`, salvo en una tarea que lo autorice de forma explícita (la del frontend de notificaciones y reportes lo hace). En esa tarea el backend no se toca.
- `AGENTS.md`, `docs/criterios-notificaciones-reportes.md` y `docs/practica-5/tarea-agente.md`.
- Las etiquetas, las ramas existentes y los commits de defectos.
- El comportamiento de los seis frentes del README y la persistencia de documentos.

**Partes que solo admiten agregados**

- `Dockerfile`: solo una línea `COPY` por cada `.csproj` **de producción** nuevo (los de pruebas no la necesitan).
- `docker-compose.yml` y `.env.example`: solo variables nuevas con valores de ejemplo. No se quita ni se cambia nada de lo que ya existe.

**Si algo no está claro**, o un criterio choca con una regla de este archivo, detente y pregunta en lugar de suponer.

---

## 3. Arquitectura

```
<raíz>/
├─ Dockerfile            etapas build, test, api, worker
├─ .dockerignore         excluye .git, bin, obj, docs, src/frontend, .env…
├─ docker-compose.yml    postgres (sin puertos) + api (8080) + worker
├─ .env.example          plantilla de TODAS las variables (valores de ejemplo)
├─ README.md             guía oficial que el profesor sigue al pie de la letra
├─ docs/                 detalle por módulo (ver sección 10)
└─ src/
   ├─ backend/
   │  ├─ Wheelby.slnx
   │  └─ src/
   │     ├─ Host/Host.WebAPI      API (Minimal APIs, Scalar, GlobalExceptionHandler)
   │     ├─ Host/Host.Worker      BackgroundServices: cola de correos y expiración de solicitudes
   │     ├─ Shared/               SharedKernel (AggregateRoot, AppException…) y Shared.Application (IClock, ValidationBehavior)
   │     ├─ Core/AccessControl    registro, sesión (JWT + sesiones revocables), roles, recuperación de contraseña
   │     ├─ Core/Notifications    cola de correos (QueuedEmails) y envío por SMTP con MailKit; en la P5 suma bandeja interna, preferencias y reintentos
   │     ├─ Core/Permissions      solicitudes de permiso, aprobación, concesiones, catálogo (IPermissionChecker)
   │     ├─ Core/Documents        manejador de documentos (subir, listar, descargar, eliminación lógica)
   │     └─ Domain/Wheelby        negocio: vehículos, reservas, máquina de estados, fotos, reseñas
   └─ frontend/                   React (no está en el compose; ver sección 9)
```

**Cada módulo** tiene `X.Contracts`, `X.Domain`, `X.Application`, `X.Infrastructure` y, si tiene pruebas, `X.Test`. La plantilla a calcar es **Permissions**:

- `DependencyInjection.cs` con `AddX(configuration)` y `ApplyXMigrationsAsync()`;
- `Endpoints/Endpoints.cs` con `MapXEndpoints()`;
- `DbContext` con esquema propio y `XDbContextFactory` de diseño (lee `ConnectionStrings__Postgres` del entorno);
- `XUnitOfWork`;
- `InternalsVisibleTo` hacia el proyecto de pruebas;
- dobles hechos a mano en `X.Test/TestSupport/`.

**Esquemas de la base:** `access_control`, `notifications`, `permissions`, `documents` y `wheelby`. Cada uno tiene su `__EFMigrationsHistory`. No hay claves foráneas entre esquemas: los Guid entre módulos se validan en los casos de uso.

**Roles:** `Standard` y `Administrator` (`AccessControlRoles`). Políticas: `RequireStandard` y `RequireAdministrator`. El Administrador no opera como miembro: recibe 403 en `/vehicles` y `/reservations`.

**Permiso de negocio:** publicar un vehículo exige `wheelby.publish-vehicle`, aprobado por un Administrador en Gestión de permisos y comprobado con `IPermissionChecker`.

---

## 4. Máquina de estados de la reserva

Fuente única: `Wheelby.Domain/Reservations/ReservationStateMachine.cs`. Detalle en `docs/maquina-de-estados.md`.

| Desde | Hacia | Actor | Condición |
|---|---|---|---|
| `Pending` | `Confirmed` | `Host` | `utcNow <= StartDate` |
| `Pending` | `Cancelled` | `Renter`, `Host`, `System` | Renter/Host: `utcNow < StartDate`, motivo obligatorio si cancela el Host. System: expira cuando `utcNow >= StartDate` |
| `Confirmed` | `InProgress` | `Host` | `StartDate <= utcNow <= EndDate` |
| `Confirmed` | `Cancelled` | `Renter`, `Host` | `utcNow < StartDate`, motivo obligatorio si cancela el Host |
| `InProgress` | `Completed` | `Host` | `utcNow >= EndDate` |

- **Prohibida explícita:** `InProgress → Cancelled` (409).
- **Terminales:** `Completed` y `Cancelled`.
- `Reservation.TransitionTo` es el **único** código que cambia `Status`. Además escribe en `ReservationStatusHistory` y emite `ReservationStatusChanged`, que se convierte en correo después de guardar.
- **Reservas bloqueantes:** `Confirmed` e `InProgress`. La restricción `EXCLUDE no_blocking_overlap` cierra la carrera entre dos confirmaciones simultáneas (409 `vehicle.overlap`).
- La suite de la Práctica 3 tiene una prueba por fila, la matriz de pares fuera de la tabla y una **prueba estructural** que copia la tabla a mano. Si cambias la tabla, actualiza esas pruebas y `docs/maquina-de-estados.md`.

---

## 5. Cómo se ejecuta

```bash
cp .env.example .env            # PowerShell: Copy-Item .env.example .env
docker compose up --build       # API en http://localhost:8080, Scalar en /scalar/v1
docker compose down             # conserva datos;  down -v los borra
```

- **`.env` es la única fuente de configuración.** No hay `UserSecretsId` en los Hosts. Si falta una variable obligatoria (`App__ActivationUrl`, `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`, `Admin__Email`, `Admin__Password`), compose se detiene antes de arrancar y dice cuál falta.
- El compose arma `ConnectionStrings__Postgres` (`Host=postgres`, con `GSS Encryption Mode=Disable`) y pone `OpenApi__Enabled=true`.
- **Volúmenes:** `wheelby-pgdata` para la base y `wheelby-documents` para los archivos (`/app/data/documents`).
- **Correos:** el servicio `worker` los envía cada `Worker__IntervalSeconds`. Sin SMTP real quedan `Pending` y se leen así:
  ```bash
  docker compose exec postgres psql -U wheelby -d wheelby -c "SELECT \"To\", \"Status\", \"Body\" FROM notifications.\"QueuedEmails\" ORDER BY \"CreatedAt\" DESC LIMIT 5;"
  ```
- **API externa:** ExchangeRate-API con `ExchangeRate__ApiKey`. La clave viaja en `Authorization: Bearer`, nunca en la URL ni en el log, y la tasa se cachea por par.

### Pruebas

```bash
docker build --target test .                  # sin instalar .NET
cd src/backend && dotnet test                 # con el SDK de .NET 10
```

Estado actual: **278 pruebas** en verde (Documents 44, Permissions 71, Wheelby 163) y **0 warnings** en Debug y en Release. Mantén ambos en cero: los warnings se corrigen en su origen, no se silencian.

Convenciones de las pruebas:

- Tres fases (`// Arrange`, `// Act`, `// Assert`) y un solo comportamiento por prueba.
- El nombre empieza con el requisito (`RF_DOC_03_...`, `RF_NEG_04_...`, `RD_10_...`).
- Independientes entre sí: cada una crea sus dobles; con EF InMemory, una base con nombre único.
- Reloj fijo; nunca `DateTime.UtcNow`.
- Los valores esperados se escriben **a mano**: nunca se leen de constantes de producción.
- Siempre `async Task`, nunca `async void` ni `.Result` o `.GetAwaiter().GetResult()`.

---

## 6. Historial de entregas

**No muevas estas etiquetas.**

| Etiqueta | Commit | Qué evaluaba | Notas |
|---|---|---|---|
| `practica-1` | `7f6b415` | Control de acceso | — |
| `practica-2` y `-defecto-1` a `6` | `f94a718` | Gestión de permisos, suite y defectos plantados | — |
| `practica-3` y `-defecto-1` a `4` | `5d889eb` | Módulo de negocio, suite de la máquina, prueba de integración con Permissions, defectos 1 a 4 | En esa etiqueta, F2 a F4 dan 500 contra PostgreSQL (`IReadOnlySet.Contains` no se traducía a SQL). Se corrigió después (`4b1061d`) sin mover la etiqueta, por plazo. Estimación: 7.25 de 8. |
| `practica-4` | `5e63db4` | Manejador de documentos, contenedor con un comando, volúmenes, API externa con clave por entorno, seis frentes | Falta en el README el «ejemplo real de respuesta» de la API externa. Estimación: entre 7.25 y 8 de 8. |

Los commits de defecto **nunca** se integran a `main`: cuelgan solo de su etiqueta.

Después de `practica-4` entraron a `main` (ninguna práctica lo evaluó todavía):

- el frontend completo sobre la API real;
- las prioridades 1 a 4 de `docs/wheelby/pendientes-backend.md`: detalle de reserva y vehículo, `GET /vehicles/mine`, disponibilidad, precio congelado, códigos de error estables, enums como texto, historial de estados, correos por transición (RF-NEG-07), documentos de la reserva (RF-NEG-10), editar y desactivar vehículos, tipo, ciudad y descripción, fotos, expiración de solicitudes con actor `System`, restricción `EXCLUDE` y reseñas.

---

## 7. Pendientes y problemas conocidos

Ordenados por prioridad sugerida.

1. **Lint del frontend con 5 errores** (`npm run lint`; el build sí pasa).
   - 3 son `react-refresh/only-export-components` en `src/frontend/src/components/ui/badge.tsx`, `button.tsx` y `tabs.tsx`: exportan funciones junto al componente.
   - 2 son `react-hooks/immutability` en `src/frontend/src/features/auth/shared/layout/hooks/useAuthMotionForms.ts` (líneas 60 y 134), que modifica un valor que React no permite modificar. Este conviene revisarlo.
2. **El frontend no está en el README principal ni en el compose.** Se arranca aparte con `npm install` y `npm run dev` (puerto 5173), según `src/frontend/README.md`, que el README principal no enlaza. `.dockerignore` excluye `src/frontend`. Si una práctica evalúa la app completa desde el README, hay que documentarlo o contenedorizarlo.
3. **Comportamiento de la API externa cambió después de `practica-4`:** sin clave o con el proveedor caído, la búsqueda ya no responde 503 sino **200 en DOP sin conversión** (el README sección 6.1 ya lo dice). Si una práctica futura vuelve a pedir «rechazo controlado», hay que justificarlo o revisarlo. El comentario de `ExchangeRate__ApiKey` en `.env.example` todavía dice «responde 503»: está desactualizado.
4. **Convención de fechas sin decidir** (`docs/wheelby/pendientes-backend.md`): la reserva es por día o por instante UTC. Con usuarios en UTC−4, una reserva «del 20 al 22» puede empezar el 19 a las 20:00 hora local.
5. **Prueba de rutas que no comprueba nada:** en `src/backend/src/Domain/Wheelby/Wheelby.Test/Infrastructure/EndpointRouteTests.cs`, la prueba de `/admin/reservations` hace `if (rutasAdmin.Count == 0) return;`. Leer `EndpointDataSource` del contenedor sin arrancar la app devuelve una lista vacía, así que la prueba pasa sin verificar. La forma correcta es la de `Documents.Test/Infrastructure/DocumentEndpointRouteTests.cs`: leer `((IEndpointRouteBuilder)app).DataSources` y exigir el número exacto de endpoints.
6. **Ejemplo real de la API externa** pendiente en el README, si se quiere completar para una entrega futura.
7. **Fuera de alcance por ahora:** auditoría de documentos (RF-DOC-06, semana 12), verificación de identidad del anfitrión, promedio de calificación en la ficha y cualquier cambio del frontend que no esté en una tarea asignada. **Los reportes entran en la Práctica 5** (sección 12).
8. **README desactualizado en pruebas:** dice 234 pruebas y hoy son 278. Se corrige en la Práctica 5.

---

## 8. Trampas aprendidas

- **Las pruebas con dobles en memoria no detectan errores de traducción a SQL.** El 500 de la Práctica 3 pasó todas las pruebas. Después de tocar consultas LINQ de repositorios o read stores, levanta la app contra PostgreSQL real y recorre el flujo por HTTP. En LINQ usa arreglos o listas en `Contains`, no `IReadOnlySet`.
- **Endpoints con `IFormFile`:** declara `[FromServices] ISender` y `.DisableAntiforgery()`. Sin eso, ASP.NET infiere `ISender` como cuerpo JSON o falla en ejecución.
- **Las pruebas de rutas sin arrancar la app** deben leer `((IEndpointRouteBuilder)app).DataSources`, no el `EndpointDataSource` del contenedor.
- **Un `.env` incompleto** es la causa típica de que la API se reinicie en bucle. Hoy compose valida las obligatorias antes de arrancar.
- **Cada `.csproj` nuevo de producción** debe agregarse a la lista de `COPY` del `Dockerfile` (etapa `build`), o la imagen no restaura.
- **El estudiante usa Windows con Git Bash y PowerShell.**
  - En Git Bash, Ctrl+V no pega (usa Shift+Insert) y las rutas van como `/c/Users/...`.
  - En PowerShell, el comillado de `psql -c "..."` es frágil: es mejor abrir `psql` interactivo.
  - Su clon está en `C:\Users\DELL\Desktop\ITLA\P3\vehicle-marketplace\wheelby`.
- **En entornos de agente en la nube** con proxy HTTPS propio, `dotnet restore` dentro de `docker build` falla por certificado. Para probar, usa una copia local del Dockerfile que confíe en el certificado del proxy **solo en la etapa `build`**, y nunca la commitees.
- **Las etiquetas** no se pueden subir desde algunas sesiones de agente (el proxy de git lo bloquea). En ese caso, prepara un script para que el estudiante las suba desde su máquina.

---

## 9. Frontend

- **Arranque:** `cd src/frontend && npm install && npm run dev` (http://localhost:5173), con la API corriendo. La URL de la API va en `VITE_API_URL` (`src/frontend/.env.example`).
- **Activación de cuentas desde el frontend:** el estudiante pone en su `.env` de la raíz `App__ActivationUrl=http://localhost:5173/auth/activar`. El agente no edita `.env` (sección 2.3).
- **Errores:** el cliente decide con `extensions.code` del `problem+json`, nunca con el texto de `detail`. Tabla de códigos en `docs/wheelby/README.md`.
- **Navegación:** todo lo visible vive en la ruta o en su query string. Mapa de rutas en `src/frontend/README.md`.

---

## 10. Dónde está cada cosa

| Necesito | Archivo |
|---|---|
| Guía de ejecución y verificación (lo que sigue el profesor) | `README.md` |
| Variables de configuración | `README.md` sección 8 y `.env.example` |
| Criterios de Control de acceso y cola de correos | `docs/control-acceso/README.md` |
| Criterios de Gestión de permisos | `docs/gestion-permisos/README.md` |
| Rutas, formato de respuestas y códigos de error del negocio | `docs/wheelby/README.md` |
| Máquina de estados | `docs/maquina-de-estados.md` |
| Pendientes del backend (checklist) | `docs/wheelby/pendientes-backend.md` |
| Frontend | `src/frontend/README.md` |
| Defectos plantados de cada práctica y qué prueba los detecta | `README.md` sección «Defectos» |
| Criterios de aceptación de la Práctica 5 | `docs/criterios-notificaciones-reportes.md` |
| Diseño y orden de trabajo de la Práctica 5 | `docs/practica-5/tarea-agente.md` |
| Notificaciones: diseño y verificación (lo crea la P5) | `docs/notificaciones/README.md` |

---

## 11. Antes de dar por terminado un cambio

- [ ] `docker build --target test .` (o `dotnet test`) en verde y `dotnet build` con 0 warnings.
- [ ] Si tocaste el frontend: `npm run build` y `npm run lint`.
- [ ] Si tocaste consultas, endpoints o configuración: `docker compose up --build` y el flujo afectado probado por HTTP contra PostgreSQL real.
- [ ] README y `docs/` actualizados si cambió una ruta, una variable, un código de error o un paso de verificación. El README es lo que se evalúa: lo que no dice, no existe.
- [ ] Commits con el formato de la sección 2.1, a nombre del estudiante y sin trailers.
- [ ] Ninguna etiqueta movida y ningún secreto en el diff (`git diff`, `.env` fuera del índice).
- [ ] Nada de la lista "no se toca" de la sección 2.3 aparece en `git diff main --stat`.

---

## 12. Práctica 5: notificaciones, cola de correos y reportes

**Qué se entrega:** la etiqueta `practica-5`, el último hito del cuatrimestre.

| Pieza | Requisitos | Resumen |
|---|---|---|
| Notificaciones | RF-NOT-01 a 07 | Bandeja interna propia y correo por cada notificación. Crear una solicitud de permiso notifica a los Administradores activos; resolverla notifica al solicitante (con el motivo si se rechaza). Las transiciones de reserva pasan por el motor; el evento documentado es "solicitar una reserva notifica al anfitrión". Cada usuario puede desactivar su correo. |
| Cola de correos | RF-NOT-08 a 13 | Máximo de **5 intentos** (`Notifications__MaxEmailAttempts`) y luego `Failed` con su último error. El Administrador consulta la cola (sin el cuerpo). Ejecución manual con `docker compose run --rm worker --once`, sin duplicar envíos. |
| Reportes | RF-REP-01 a 05 | `GET /reports/permission-requests` (solicitudes por mes y estado) y `GET /reports/vehicle-revenue` (ingresos y días por vehículo), con `from`/`to` y agregación en PostgreSQL. El Estándar ve solo lo suyo. |

**Decisiones ya tomadas** (no se discuten en la implementación):

- Los correos de cuenta (activación, recuperación) **no** son notificaciones: siguen por `IEmailQueue`, sin bandeja y sin respetar la preferencia.
- Solo backend. El frontend se trabaja después, sobre la API terminada y en una tarea propia.
- Notificar es un efecto secundario posterior al guardado, igual que RF-NEG-07: no es outbox, y se documenta.

**Fuentes de verdad, en este orden:** `docs/criterios-notificaciones-reportes.md` → `docs/practica-5/tarea-agente.md` → este archivo.
