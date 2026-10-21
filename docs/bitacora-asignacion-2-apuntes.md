# Apuntes de la sesión con el agente — Asignación 2

Registro de los errores **reales** que cometió el agente durante la implementación del módulo de Gestión de permisos. Sirve de insumo para llenar `docs/bitacora-asignacion-2.md`, que el estudiante debe redactar con su propia experiencia.

Fecha de la sesión: 20 de octubre de 2026. Rama de trabajo: `feature/permissions`.

---

## 1. `git add` con una ruta relativa equivocada

**Qué pasó:** al preparar el commit `RF-GP-03: agrega comando aprobar solicitud`, el comando `git add src/backend/src/Core/Permissions/Permissions.Application` se ejecutó con la carpeta de trabajo en `src/backend`. Git respondió `fatal: pathspec 'src/backend/src/backend/src/Core/Permissions/' did not match any files` y el commit **no** se creó.

**Cómo se detectó:** por el mensaje de error de Git en la consola, en la misma línea en que se ejecutó el `git add`.

**Cómo se corrigió:** se repitió el `git add` desde la raíz del repositorio (`C:\...\vehicle-marketplace\wheelby`), con la ruta completa `src/backend/src/Core/Permissions/Permissions.Application`, y el commit se creó.

---

## 2. Las migraciones se generaron en la carpeta equivocada

**Qué pasó:** `dotnet ef migrations add InitialPermissions` dejó los archivos en `src/backend/src/Core/Permissions/Permissions.Infrastructure/Migrations/`, en la raíz del proyecto, cuando la convención del repositorio (AccessControl, Notifications y Wheelby) es `Persistence/Migrations/`.

**Cómo se detectó:** al listar los archivos generados después de la migración, se vio que la carpeta `Migrations` estaba en la raíz del proyecto y no dentro de `Persistence/`, y que el `namespace` generado era `Permissions.Infrastructure.Migrations` en lugar de `Permissions.Infrastructure.Persistence.Migrations`.

**Cómo se corrigió:** se borró la carpeta generada y se volvió a generar la migración con `--output-dir Persistence/Migrations`, con lo que el `namespace` quedó igual que en el resto de módulos.

---

## 3. La migración de concurrencia intentaba crear la columna `xmin`

**Qué pasó:** al añadir el token de concurrencia con `builder.Property<uint>("xmin").IsRowVersion()`, la migración autogenerada `AddPermissionRequestConcurrency` contenía un `AddColumn` de la columna `xmin` (`type: "xid"`, `rowVersion: true`). En PostgreSQL, `xmin` es una **columna de sistema** que ya existe en cada fila: un `ALTER TABLE ... ADD COLUMN "xmin"` falla con `column name "xmin" conflicts with a system column name`, y la API no habría podido arrancar.

**Cómo se detectó:** al leer el archivo de la migración generado se vio el `AddColumn` de `xmin`. Se comprobó con `dotnet ef migrations script` (que genera el SQL sin necesidad de base de datos) que el SQL resultante **no** incluía la columna: el proveedor de Npgsql omite las columnas de sistema al generar el DDL, así que la migración era un DDL engañoso que, aplicado con otra herramienta, sí habría fallado.

**Cómo se corrigió:** se reescribió la migración `AddPermissionRequestConcurrency` **sin operaciones de DDL** (el `Up` y el `Down` quedan vacíos, con un comentario que explica que `xmin` es una columna de sistema), de modo que solo actualiza el snapshot del modelo. Se verificó después con `dotnet ef migrations script` (la segunda migración aparece con su registro en `__EFMigrationsHistory` y sin SQL) y con `dotnet ef migrations has-pending-model-changes` ("No changes have been made to the model since the last migration").

---

## 4. Decisión de trabajo: el cambio previo de `Wheelby.slnx`

**Contexto (no es un error del agente, pero conviene dejarlo escrito):** antes de empezar, `Wheelby.slnx` ya tenía un cambio **sin commitear** que agregaba el proyecto `AccessControl.Test`, y ese proyecto estaba sin versionar. Para no incluir ese trabajo ajeno en los commits de la asignación (y para que cada commit compile), la línea de `AccessControl.Test` se dejó fuera del índice de Git durante toda la sesión: los commits de `Wheelby.slnx` solo contienen las entradas de `Permissions.*`. **Al terminar la sesión hay que restaurar esa línea en el archivo**, para que el estudiante conserve su cambio previo sin commitear.
