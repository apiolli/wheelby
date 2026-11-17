# Wheelby · Frontend

Interfaz web de Wheelby. Implementa el diseño de `apiolli/wheelby-design` sobre la API real del backend (`src/backend`), solo con las funciones que la API ya ofrece.

## Arranque

Requisitos: Node `^20.19.0 || >=22.12.0` y la API corriendo (`docker compose up --build` desde la raíz del repositorio).

```bash
npm install
npm run dev      # http://localhost:5173
npm run build    # typecheck + build de producción
npm run lint
```

La URL de la API se configura con `VITE_API_URL` (ver `.env.example`; por defecto `http://localhost:8080`). La API ya permite CORS desde `http://localhost:5173`.

**Activación de cuentas:** el enlace del correo debe abrir la página del frontend. En el `.env` de la raíz, usa `App__ActivationUrl=http://localhost:5173/auth/activar`. La página llama a `POST /auth/activate` y lleva al login.

## Stack

| Para qué | Librería |
| --- | --- |
| Navegación | React Router 8 (`createBrowserRouter`) |
| Datos del servidor | axios + TanStack Query |
| Estado global (sesión) | Zustand (`src/stores/session.store.ts`) |
| Validaciones | Zod (`src/lib/schemas.ts`): formularios y parámetros de la URL |
| Animaciones | Motion (`m.*` con `LazyMotion strict`); GSAP solo en la coreografía ya existente de las páginas de acceso |
| UI | Tailwind 4 + primitivos estilo shadcn sobre Base UI (`src/components/ui`) |

## Navegación por URL

Todo lo que se ve vive en la ruta o en su query string, así se puede enlazar, recargar y usar atrás/adelante:

| Ruta | Qué muestra | Parámetros |
| --- | --- | --- |
| `/auth/login`, `/auth/registro`, `/auth/recuperar` | Acceso | `?redirect=` (vuelta tras iniciar sesión) |
| `/auth/activar` | Activación desde el correo | `?userId=&token=` |
| `/` | Catálogo de vehículos libres (solo rol Estándar) | `?desde=&hasta=&moneda=DOP\|USD\|EUR&pagina=&reservar=<id>` |
| `/perfil/cuenta`, `seguridad`, `documentos` | Perfil | `documentos?pagina=` |
| `/perfil/reservas` | Reservas como arrendatario | `?pagina=` |
| `/perfil/anfitrion` | Solicitar ser anfitrión y publicar vehículos | — |
| `/perfil/solicitudes` | Reservas de mis vehículos (confirmar, rechazar, iniciar, completar, cancelar) | `?pagina=` |
| `/admin` | Resumen (solo Administrador) | — |
| `/admin/usuarios` | Usuarios y sus acciones | `?q=&rol=&estado=&pagina=&cuenta=<id>` |
| `/admin/verificaciones` | Solicitudes de permiso pendientes | — |
| `/admin/reservas` | Todas las reservas | `?estado=&pagina=` |
| `/admin/documentos` | Documentos de todas las cuentas | `?pagina=` |

Sin sesión, cualquier ruta protegida lleva a `/auth/login?redirect=…`. Un 401 en cualquier llamada cierra la sesión y muestra "Tu sesión expiró".

## Estructura

```
src/
├── api/            # cliente axios, errores (problem+json) y una función por endpoint
├── stores/         # sesión (Zustand)
├── lib/            # schemas (Zod), formatos, transiciones de Motion, claves de Query, textos de estados
├── router/         # rutas y páginas en carga diferida
├── components/
│   ├── ui/         # primitivos (no llevan lógica de negocio)
│   ├── custom/     # piezas compartidas: ConfirmAction, EmptyState, Pagination, badges de estado…
│   └── layout/     # ProtectedRoute, AppLayout, MotionRoot
└── features/
    ├── auth/        # login, registro, recuperación y activación
    ├── marketplace/ # header (búsqueda por fechas y moneda) y catálogo
    ├── profile/     # secciones del perfil
    └── admin/       # panel de administración
```

## Diferencias con el prototipo de diseño

El prototipo usa datos simulados. Donde la API no tiene la función, se adaptó o se quitó en lugar de inventarla:

- **Búsqueda:** la API busca por fechas (y convierte precios), no por ciudad ni tipo. La barra tiene Recogida, Devolución y Moneda.
- **Categorías y fotos:** los vehículos no tienen categoría, ciudad, fotos ni calificación. Las pestañas del header llevan a Explorar, Mis reservas, Solicitudes y Anfitrión, y las tarjetas usan la ilustración de marca.
- **Favoritos:** no existen en la API; se quitaron.
- **Anfitrión:** es el permiso `wheelby.publish-vehicle`, que se pide y aprueba desde Gestión de permisos. No hay documentos adjuntos a la solicitud.
- **Usuarios:** los estados son Activa, Sin activar y Desactivada (la API no expone el bloqueo por intentos). La búsqueda y los filtros se hacen en el cliente, porque la API solo pagina.
- **Reservas:** la API devuelve el id del vehículo, no sus datos; se muestra su referencia corta.
- **Administrador:** la API no le permite reservar ni publicar, así que no ve el catálogo; entra directo a su panel.
