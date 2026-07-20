# AGENTS.md

Guía para agentes de código (OpenCode, Claude Code, Codex, Gemini CLI, etc.) que programen sobre el repositorio **Caimmand**. Resume `README.md` y `docs/`; ante conflicto, consultar la documentación original.

## Arquitectura

**Modular Monolith orientado al dominio** (ADR-001, estado *Proposed*). No microservicios, no arquitectura por capas tradicionales, no HTTP interno.

Principios:
- **Domain First** y **Case First**: el modelo operativo gira alrededor del Caso. No existen entidades operativas fuera de un Caso.
- **API First**: la Command API (Minimal APIs) es el único punto de entrada autorizado. Sin acceso directo a la base de datos.
- **Modular by Business Capability**: descomposición por capacidades de negocio (Cases, Timeline, Tasks, Participants, Audit, Case Definitions), no por capas técnicas.
- Proceso único y deploy único (Blazor Server + Minimal APIs juntos).

Dependencias entre proyectos:
- `Caimmand.Web` → `Application` + `Domain` (+ `Infrastructure` solo para registro de servicios en DI).
- `Caimmand.Application` → `Domain`.
- `Caimmand.Domain` → nada (entidades, `ICaimmandDbContext`, enums).
- `Caimmand.Infrastructure` → `Domain` (implementa `ICaimmandDbContext` con EF Core + PostgreSQL).

Ciclo de vida del Caso: `Creado → En curso ⇄ Suspendido → Finalizado | Cancelado`. Las transiciones las define `Domain/Enums/CaseStatusTransitions.cs` y las gobierna Caimmand.

## Estructura de la solución

```
src/
    Caimmand.slnx
    Caimmand.Domain/                    # entidades, Value Objects, enums, ICaimmandDbContext
        Entities/  Enums/
    Caimmand.Application/               # casos de uso; consume ICaimmandDbContext
        Cases/{Create,List,GetDetail,UpdateStatus}/
        Timeline/{AddEvent,GetTimeline}/
        Dashboard/GetDashboardKpis/
    Caimmand.Infrastructure/            # EF Core: CaimmandDbContext, migraciones, DI
        Migrations/
    Caimmand.Web/                       # host único: Blazor Server + Minimal APIs + DI
        Components/Pages/               # Home, Cases, CaseView, Error, NotFound
        Components/Layout/
tests/Caimmand.Tests/                   # xUnit; espeja Application (un *HandlerTests.cs por operación)
    Infrastructure/TestDbContext.cs
scripts/run-postgres.ps1
```

Entidades persistidas hoy: `Case`, `CaseDefinition`, `TimelineEvent`.
Endpoints (`Program.cs`): `POST /api/cases`, `GET /api/cases`, `GET /api/cases/{id}`, `PATCH /api/cases/{id}/status`, `POST /api/cases/{id}/timeline`, `GET /api/cases/{id}/timeline`.

## Decision Priority

Ante contradicciones, prevalece en este orden:
1. ADR-001 (`docs/01-architecture/ADR/ADR-001-Modular-Monolith.md`)
2. `docs/01-architecture/Architecture.md`
3. `docs/03-implementation/PoC-Implementation-Plan.md`
4. `docs/02-development/DomainModel.md`
5. `docs/02-development/MVP.md`
6. `docs/00-product/PDD.md`
7. `docs/00-product/UX-Guidelines.md`
8. `README.md`

Excepción documentada en `Architecture.md`: ante conflicto con la arquitectura, el PDD prevalece sobre `Architecture.md` (este último debe corregirse).

## Coding Rules

- Mantener **Vertical Slice Architecture**: cada operación es una carpeta con `Command/Query`, `Handler` y (si modifica estado) `Validator`. Sin carpetas transversales tipo `Controllers`/`Services`/`Repositories`.
- Patrón de nombres: `CreateXxxCommand`, `ListXxxQuery`, `GetXxxQuery`, `UpdateXxxCommand`, `XxxHandler`, `XxxValidator`.
- **Una responsabilidad por clase**. Un handler = un caso de uso.
- **Constructor injection**; registrar handlers como `Scoped` en `Program.cs` (estilo ya presente).
- Consumir `ICaimmandDbContext` (interfaz en `Domain`), nunca `CaimmandDbContext` concreto desde `Application`.
- **No crear Generic Repository**. EF Core es la abstracción de persistencia suficiente.
- **No usar AutoMapper**. Mapear manualmente en handlers/responses.
- **Usar UTC** para todas las fechas (`DateTime.UtcNow`, como en `CreateCaseHandler.cs`).
- **No introducir abstracciones innecesarias**; preferir soluciones simples y legibles.
- No acceder a la persistencia de otra capacidad; comunicarse por interfaces públicas, servicios de aplicación o eventos de dominio.
- Respetar la organización actual del proyecto y mantener consistencia con el código existente (estilo, namespaces por feature).
- Validación con FluentValidation; logging con Serilog.
- Comandos y queries son `record` o clases simples; las responses son records.

## Before Implementing

Checklist antes de tocar código:
- [ ] Buscar primero una implementación similar (por ejemplo, `Cases/Create/` como referencia para nuevas capacidades).
- [ ] Reutilizar handlers, validators y patterns existentes cuando sea posible.
- [ ] No modificar la arquitectura sin una decisión explícita (requiere ADR o justificación documentada).
- [ ] No refactorizar código no relacionado con la tarea.
- [ ] Actualizar tests en `tests/Caimmand.Tests/` (espejando la carpeta de `Application`) cuando cambie el comportamiento.
- [ ] Verificar que el cambio no rompa las reglas de dependencia entre proyectos.
- [ ] Verificar coherencia con `ADR-001`, `Architecture.md` y `DomainModel.md`.

## Current Scope

**In Scope (PoC actual):**
- Cases (entidad `Case`)
- Case Definitions (entidad `CaseDefinition`)
- Timeline (entidad `TimelineEvent`)
- Dashboard (KPIs por estado y "Requieren Intervención")
- Blazor Server (UI en el mismo proceso)
- Minimal APIs (Command API expuesta por `Program.cs`)
- PostgreSQL + EF Core
- FluentValidation, Serilog, Docker Compose
- Flujo end-to-end del caso `APPOINTMENT_REMINDER` (sembrado al iniciar)

**Out of Scope (excluido del PoC):**
- Task (Tarea)
- Participant (Participante)
- Audit (Registro de Auditoría)
- Multi-tenant
- Autenticación y autorización complejas (Keycloak)
- Mensajería asíncrona (RabbitMQ)
- Bus de eventos / eventos de integración
- Observabilidad enterprise (OpenTelemetry, Redis)
- Integraciones reales (n8n, Meta WhatsApp)
- Motor BPM / Motor de workflows
- Notificaciones
- IA autónoma tomando decisiones críticas
- Marketplace de agentes
- Analítica avanzada, tableros y reportes complejos

## Restricciones (qué NO hacer)

- No acceder a la base de datos directamente; todo por `ICaimmandDbContext` / Command API.
- No crear Casos desde dentro de Caimmand (los crea el Sistema de Origen vía API).
- No implementar reglas de negocio en workflows externos; las reglas viven en Caimmand.
- No ejecutar procesos de negocio, workflows, BPMN, prompt engineering ni automatizaciones dentro de Caimmand.
- No generar modificaciones silenciosas: toda modificación relevante genera un `TimelineEvent` (y, cuando se implemente, un Registro de Auditoria).
- No introducir dependencias con herramientas de automatización específicas (n8n, etc.).
- No crear dependencias circulares ni acceso cruzado a persistencia entre módulos.
- Tasks no es un motor BPM: no contiene lógica de flujo ni ejecuta trabajo.
- No introducir Repository Pattern genérico, AutoMapper, ni capas abstractas no presentes hoy.

## Navegación de la documentación

| Tema | Documento |
|------|-----------|
| Visión, misión, principios y alcance del producto | `docs/00-product/PDD.md` |
| Guía UX y pantallas | `docs/00-product/UX-Guidelines.md` |
| Arquitectura funcional, Command API, límites del sistema | `docs/01-architecture/Architecture.md` |
| Justificación del Modular Monolith y reglas de dependencias | `docs/01-architecture/ADR/ADR-001-Modular-Monolith.md` |
| Entidades del dominio y relaciones | `docs/02-development/DomainModel.md` |
| Alcance y objetivos del MVP | `docs/02-development/MVP.md` |
| Plan técnico del PoC (stack, fases, endpoints, pantallas, decisiones técnicas) | `docs/03-implementation/PoC-Implementation-Plan.md` |

## Inconsistencias encontradas

- **Extensión de la solución**: `PoC-Implementation-Plan.md` describe `src/Caimmand.sln`; en el repositorio existe `src/Caimmand.slnx`.
- **Acceso a DbContext desde Application**: `PoC-Implementation-Plan.md` afirma *"Application consume CaimmandDbContext directamente"* y *"Sin Repository Pattern... sin interfaces"*, pero el código define `ICaimmandDbContext` en `Caimmand.Domain` y los handlers consumen esa interfaz.