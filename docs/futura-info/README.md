# Wheelby
<img width="2400" height="813" alt="wheelby-logo-sobre-ambar" src="https://github.com/user-attachments/assets/216c3a90-8087-47a5-b5a1-3b851f93391e" />

Marketplace de alquiler de vehículos entre particulares (autos, motos y vehículos ligeros sin motor). Proyecto de **Programación III · ITLA · 2026-C-3**.

Dueños de vehículos ("anfitriones") los publican con calendario y precio por día; otros usuarios los rentan por rango de fechas. Un Administrador de la plataforma verifica anfitriones y resuelve excepciones, sin participar en las reservas del día a día.

Construido sobre la especificación del Core del curso — la misma base técnica para los 25 proyectos, con este dominio encima. Cada pieza del Core tiene su propio README con su progreso y cómo verificar cada requisito.

## Componentes del Core

- [ ] **[1. Control de acceso](./docs/control-acceso/README.md)** — Semanas 2–4 · en progreso
- [ ] [2. Gestión de permisos](./docs/gestion-permisos/README.md) — Semanas 6–8
- [ ] [3. Manejador de documentos](./docs/manejador-documentos/README.md) — Semana 9
- [ ] [4. Notificaciones y cola de correos](./docs/notificaciones/README.md) — Semanas 11–12
- [ ] [5. Reportes](./docs/reportes/README.md) — Semana 12
- [ ] [6. Auditoría](./docs/auditoria/README.md) — Semana 14

## Stack

- React con Typescript | C# con .NET Core

## Cómo ejecutar

### 1. Clonar el repositorio

```bash
git clone https://github.com/apiolli/vehicle-marketplace.git
cd vehicle-marketplace
```

### 2. Estado actual del proyecto

Actualmente el repositorio contiene la documentación inicial y la estructura de planificación del proyecto. La tecnología y el punto de entrada de la aplicación todavía están por definir.

Por esta razón, en esta etapa no existe todavía un comando de instalación de dependencias, compilación o ejecución que pueda ser verificado.

### 3. Variables de entorno

Cuando se defina la implementación, las variables de entorno necesarias deberán documentarse en la documentación correspondiente de cada componente.

No se deben agregar credenciales, claves ni valores sensibles al repositorio.

### 4. Verificación

Para verificar el estado actual del repositorio:

```bash
git clone https://github.com/apiolli/vehicle-marketplace.git
cd vehicle-marketplace
git status
```

El repositorio debe clonarse correctamente y mostrar un estado de trabajo limpio.

## Estructura del proyecto

```
docs/
  control-acceso/README.md       ← Pieza 1
  gestion-permisos/README.md     ← Pieza 2
  manejador-documentos/README.md ← Pieza 3
  notificaciones/README.md       ← Pieza 4
  reportes/README.md             ← Pieza 5
  auditoria/README.md            ← Pieza 6
  maquina-de-estados.md          ← tabla de transiciones del dominio (desde semana 4)
[resto de la estructura, según el stack elegido]
```

## Convención de commits y pull requests

Cada commit y PR referencia el identificador del requisito que resuelve, tal como pide el documento de requerimientos del Core:

```
RF-CA-01: rechazar registro con correo duplicado
RF-CA-02: hashear contraseña con sal antes de guardar
```

Cada funcionalidad se construye en su propia rama y se fusiona a `main` por PR con cuatro secciones: **Qué cambia**, **Por qué**, **Cómo probarlo**, **Qué NO incluye**.

## Licencia

MIT — ver [LICENSE](./LICENSE).
