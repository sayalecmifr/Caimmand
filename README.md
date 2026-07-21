# Caimmand

Portal de operación para procesos automatizados.

## Estado

🚧 En diseño (PoC)

Actualmente se encuentra en etapa de definicion funcional, con un PoC end-to-end del flujo `APPOINTMENT_REMINDER` (Recordatorio de Turno).

## Documentacion

- Producto y UX: [`docs/00-product/`](docs/00-product/)
- Arquitectura y ADRs: [`docs/01-architecture/`](docs/01-architecture/)
- Modelo de dominio y MVP: [`docs/02-development/`](docs/02-development/)
- Plan de implementacion y API: [`docs/03-implementation/`](docs/03-implementation/)
  - Ejemplos de la Command API (curl + n8n HTTP node): [`docs/03-implementation/api-examples.md`](docs/03-implementation/api-examples.md)

## Como correr el PoC

### Opcion A - Docker Compose (recomendada para probar)

Requisitos: Docker (con Docker Compose v2).

```bash
# 1) Copiar variables de entorno (solo la primera vez)
cp .env.example .env

# 2) Levantar PostgreSQL + Web (Blazor Server + Minimal API)
docker compose up --build
```

- **UI**: `http://localhost:8080/` (Dashboard, listado y detalle de casos).
- **Command API**: `http://localhost:8080/api/...`. Ver [`docs/03-implementation/api-examples.md`](docs/03-implementation/api-examples.md) por el contrato completo con ejemplos `curl` y n8n.

PostgreSQL levanta con healthcheck; al arrancar, la Web aplica migraciones de EF Core y siembra la `CaseDefinition` `APPOINTMENT_REMINDER` si la tabla esta vacia.

### Opcion B - Local (dev / debugger)

Requisitos: .NET 10 SDK, Docker.

```bash
# 1) Levantar PostgreSQL (Docker)
./scripts/run-postgres.ps1

# 2) Compilar
dotnet build src/Caimmand.slnx

# 3) Arrancar la Web (Blazor Server + Minimal API en un unico proceso)
dotnet run --project src/Caimmand.Web
```

- **UI**: `https://localhost:5001/`
- **Command API**: `https://localhost:5001/api/...`

Util para iterar con hot reload o debugger. Connexion PostgreSQL por defecto en `appsettings.Development.json`:
`Host=localhost;Port=5432;Database=caimmand;Username=postgres;Password=postgres`.

## Tests

```bash
dotnet test src/Caimmand.slnx
```