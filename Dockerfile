# Imagen de Wheelby: una sola compilación y dos etapas finales (api y worker).
# Ni ARG ni ENV con secretos: llegan en tiempo de ejecución desde env_file (.env).

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero la solución y los .csproj, con sus rutas, para aprovechar la caché de restore.
COPY src/backend/Wheelby.slnx src/backend/
COPY src/backend/src/Core/AccessControl/AccessControl.Application/AccessControl.Application.csproj src/backend/src/Core/AccessControl/AccessControl.Application/
COPY src/backend/src/Core/AccessControl/AccessControl.Contracts/AccessControl.Contracts.csproj src/backend/src/Core/AccessControl/AccessControl.Contracts/
COPY src/backend/src/Core/AccessControl/AccessControl.Domain/AccessControl.Domain.csproj src/backend/src/Core/AccessControl/AccessControl.Domain/
COPY src/backend/src/Core/AccessControl/AccessControl.Infrastructure/AccessControl.Infrastructure.csproj src/backend/src/Core/AccessControl/AccessControl.Infrastructure/
COPY src/backend/src/Core/Documents/Documents.Application/Documents.Application.csproj src/backend/src/Core/Documents/Documents.Application/
COPY src/backend/src/Core/Documents/Documents.Contracts/Documents.Contracts.csproj src/backend/src/Core/Documents/Documents.Contracts/
COPY src/backend/src/Core/Documents/Documents.Domain/Documents.Domain.csproj src/backend/src/Core/Documents/Documents.Domain/
COPY src/backend/src/Core/Documents/Documents.Infrastructure/Documents.Infrastructure.csproj src/backend/src/Core/Documents/Documents.Infrastructure/
COPY src/backend/src/Core/Notifications/Notifications.Application/Notifications.Application.csproj src/backend/src/Core/Notifications/Notifications.Application/
COPY src/backend/src/Core/Notifications/Notifications.Contracts/Notifications.Contracts.csproj src/backend/src/Core/Notifications/Notifications.Contracts/
COPY src/backend/src/Core/Notifications/Notifications.Domain/Notifications.Domain.csproj src/backend/src/Core/Notifications/Notifications.Domain/
COPY src/backend/src/Core/Notifications/Notifications.Infrastructure/Notifications.Infrastructure.csproj src/backend/src/Core/Notifications/Notifications.Infrastructure/
COPY src/backend/src/Core/Permissions/Permissions.Application/Permissions.Application.csproj src/backend/src/Core/Permissions/Permissions.Application/
COPY src/backend/src/Core/Permissions/Permissions.Contracts/Permissions.Contracts.csproj src/backend/src/Core/Permissions/Permissions.Contracts/
COPY src/backend/src/Core/Permissions/Permissions.Domain/Permissions.Domain.csproj src/backend/src/Core/Permissions/Permissions.Domain/
COPY src/backend/src/Core/Permissions/Permissions.Infrastructure/Permissions.Infrastructure.csproj src/backend/src/Core/Permissions/Permissions.Infrastructure/
COPY src/backend/src/Domain/Wheelby/Wheelby.Application/Wheelby.Application.csproj src/backend/src/Domain/Wheelby/Wheelby.Application/
COPY src/backend/src/Domain/Wheelby/Wheelby.Contracts/Wheelby.Contracts.csproj src/backend/src/Domain/Wheelby/Wheelby.Contracts/
COPY src/backend/src/Domain/Wheelby/Wheelby.Domain/Wheelby.Domain.csproj src/backend/src/Domain/Wheelby/Wheelby.Domain/
COPY src/backend/src/Domain/Wheelby/Wheelby.Infrastructure/Wheelby.Infrastructure.csproj src/backend/src/Domain/Wheelby/Wheelby.Infrastructure/
COPY src/backend/src/Host/Host.WebAPI/Host.WebAPI.csproj src/backend/src/Host/Host.WebAPI/
COPY src/backend/src/Host/Host.Worker/Host.Worker.csproj src/backend/src/Host/Host.Worker/
COPY src/backend/src/Shared/Shared.Application/Shared.Application.csproj src/backend/src/Shared/Shared.Application/
COPY src/backend/src/Shared/SharedKernel/SharedKernel.csproj src/backend/src/Shared/SharedKernel/

WORKDIR /src/src/backend
RUN dotnet restore src/Host/Host.WebAPI/Host.WebAPI.csproj \
 && dotnet restore src/Host/Host.Worker/Host.Worker.csproj

# Después el resto del código y la publicación de los dos Hosts.
COPY src/backend/ ./
RUN dotnet publish src/Host/Host.WebAPI -c Release -o /out/api --no-restore \
 && dotnet publish src/Host/Host.Worker -c Release -o /out/worker --no-restore

# ---- test ----
# Pruebas automáticas sin instalar .NET: docker build --target test .
# Las etapas api y worker no dependen de esta, así que compose no la ejecuta.
FROM build AS test
RUN dotnet test Wheelby.slnx

# ---- api ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
COPY --from=build /out/api ./

# La carpeta del volumen debe pertenecer al usuario de la app: si no,
# el volumen se monta como root y la subida falla con permiso denegado.
USER root
RUN mkdir -p /app/data/documents && chown -R $APP_UID /app/data
USER $APP_UID

ENV Documents__StoragePath=/app/data/documents
# La imagen ya escucha en 8080 (ASPNETCORE_HTTP_PORTS).
EXPOSE 8080
ENTRYPOINT ["dotnet", "Host.WebAPI.dll"]

# ---- worker ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS worker
WORKDIR /app
COPY --from=build /out/worker ./
USER $APP_UID
ENTRYPOINT ["dotnet", "Host.Worker.dll"]
