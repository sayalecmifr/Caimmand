# syntax=docker/dockerfile:1.7

# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copiar primero la solucion y los csproj para aprovechar cache de restore.
# Los ProjectReference usan rutas ..\ relativas, por lo que los proyectos
# deben quedar en /src/{Caimmand.*}/ (estructura plana bajo /src).
COPY src/Caimmand.slnx ./
COPY src/Caimmand.Domain/Caimmand.Domain.csproj           Caimmand.Domain/
COPY src/Caimmand.Application/Caimmand.Application.csproj Caimmand.Application/
COPY src/Caimmand.Infrastructure/Caimmand.Infrastructure.csproj Caimmand.Infrastructure/
COPY src/Caimmand.Web/Caimmand.Web.csproj                 Caimmand.Web/

# Restore solo del proyecto Web (arrastra Domain/Application/Infrastructure).
# Evita depender de tests/, que no se copia al contexto de build.
RUN dotnet restore Caimmand.Web/Caimmand.Web.csproj

# Copiar el resto del codigo fuente de src/ a /src/
COPY src/ ./

# Publicar Caimmand.Web en Release
RUN dotnet publish Caimmand.Web/Caimmand.Web.csproj \
    -c Release \
    -o /app \
    /p:UseAppHost=false

# ---- final stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Development

EXPOSE 8080

ENTRYPOINT ["dotnet", "Caimmand.Web.dll"]