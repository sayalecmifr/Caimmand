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
        Entities/  Enums/  Exceptions/
    Caimmand.Application/               # casos de uso; consume ICaimmandDbContext, IAuditRecorder, IAuthorizationContext
        Cases/{Create,List,GetDetail,UpdateStatus}/
        Timeline/{AddEvent,GetTimeline}/
        Dashboard/GetDashboardKpis/
        Audit/{IAuditRecorder, AuditRecorder, GetAudit}/
        Participants/{Register,List}/
        Tasks/{Create,Assign,Start,Complete,Cancel,List,GetDetail}/
        CaseDefinitions/{Create,List,Update,SetActive}/
        Authorization/{IAuthorizationContext, Roles, UnauthorizedOperationException}
    Caimmand.Infrastructure/            # EF Core: CaimmandDbContext, migraciones, DI
        Migrations/
    Caimmand.Web/                       # host único: Blazor Server + Minimal APIs + DI
        Components/Pages/               # Home, Cases, CaseView, Tasks, CaseDefinitions, Login, Error, NotFound
        Components/Layout/
        Auth/
        Authorization/                  # HttpAuthorizationContext (implementa IAuthorizationContext)
    tests/Caimmand.Tests/                   # xUnit; espeja Application (un *HandlerTests.cs por operación)
        Infrastructure/{TestDbContext.cs, TestAuthorizationContext.cs}
scripts/run-postgres.ps1
```

Entidades persistidas: `Case`, `CaseDefinition`, `TimelineEvent`, `Participant`, `CaseParticipant` (join con `Rol`), `AuditRecord`, `Task`. Atributos no-PoC: `Case.Priority`/`Case.Sla` (columnas propias, heredadas de `CaseDefinition` al crear), `CaseDefinition.AllowedStatuses` (JSONB; vacío = global), `TimelineEvent.ParticipantId?` (vincula a Participant estructurado; `Origin` se mantiene como snapshot string).

Endpoints (`Program.cs`):
- Cases: `POST /api/cases`, `GET /api/cases`, `GET /api/cases/{id}`, `PATCH /api/cases/{id}/status`
- Timeline: `POST /api/cases/{id}/timeline`, `GET /api/cases/{id}/timeline`
- Participants: `POST /api/cases/{id}/participants`, `GET /api/cases/{id}/participants`
- Tasks: `POST /api/cases/{id}/tasks`, `GET /api/cases/{id}/tasks`, `GET /api/cases/{id}/tasks/{taskId}`, `PATCH /api/cases/{id}/tasks/{taskId}/{assign,start,complete,cancel}`
- CaseDefinitions: `POST /api/case-definitions`, `GET /api/case-definitions`, `PATCH /api/case-definitions/{id}`, `PATCH /api/case-definitions/{id}/active`
- Audit: `GET /api/cases/{id}/audit` (solo `Gerente`)

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

**In Scope (PoC + Iteracion B — IMPLEMENTADA 2026-08-11):**
- Cases (entidad `Case`, con `Priority`/`Sla` como columnas propias)
- Case Definitions (entidad `CaseDefinition`, con `AllowedStatuses` JSONB)
- Timeline (entidad `TimelineEvent`, con `ParticipantId?` )
- Participants (entidad `Participant` + join `CaseParticipant`)
- Tasks (entidad `Task`, estados `Pendiente`/`EnProgreso`/`Completada`/`Cancelada`)
- Audit (entidad `AuditRecord`, generation automatica via `IAuditRecorder` on every mutation)
- Dashboard (KPIs por estado, "Requieren Intervención"=Casos Suspendidos, `TasksOverdue`)
- Blazor Server (UI en el mismo proceso) con páginas `/`, `/cases`, `/cases/{id}`, `/tasks`, `/admin/case-definitions`, `/login`
- Minimal APIs (Command API expuesta por `Program.cs`)
- Auth interim (cookie UI + API key) + autorizacion por rol (`Operador`/`Supervisor`/`Gerente`/`Api`)
- PostgreSQL + EF Core
- FluentValidation, Serilog, Docker Compose
- Flujo end-to-end del caso `APPOINTMENT_REMINDER` (sembrado al iniciar, con `AllowedStatuses = [Creado, EnCurso, Finalizado, Cancelado]`)

**Out of Scope (siguen excluidos):**
- Multi-tenant
- Autenticacion de produccion (Keycloak / IdentityServer / OIDC) — la auth interim settings-based sigue como puerta de entrada
- Hashing de passwords en settings (sigue plaintext; backlog)
- API key por sistema externo + rotacion (sigue una sola key)
- Mensajería asíncrona (RabbitMQ)
- Bus de eventos / eventos de integración
- Observabilidad enterprise (OpenTelemetry, Redis)
- Integraciones reales (n8n workflow runner, Meta WhatsApp) — la Command API esta lista para que n8n se integre en el futuro
- Motor BPM / Motor de workflows
- Notificaciones
- IA autónoma tomando decisiones críticas
- Marketplace de agentes
- Analítica avanzada, tableros y reportes complejos
- Backlog Iteracion D: `IdempotencyContextKey` configurable por `CaseDefinition`
- ~~Backlog Iteracion C: indice GIN sobre `Context.externalId` + filtro en SQL~~ — **IMPLEMENTADO en B.4+B.5 (2026-08-12)**: indice funcional `IX_Cases_Context_ExternalId` sobre `Cases.Context` + operador `->>` (text extraction) via `IJsonQueryAdapter` (Npgsql prod / InMemory tests). Type-agnostic (matchea externalId guardado como string o numero). Paginacion offset/limit + filtros por fecha tambien agregados en B.4.

## Restricciones (qué NO hacer)

- No acceder a la base de datos directamente; todo por `ICaimmandDbContext` / Command API.
- No crear Casos desde dentro de Caimmand (los crea el Sistema de Origen vía API).
- No implementar reglas de negocio en workflows externos; las reglas viven en Caimmand.
- No ejecutar procesos de negocio, workflows, BPMN, prompt engineering ni automatizaciones dentro de Caimmand.
- No generar modificaciones silenciosas: toda modificación relevante genera un `TimelineEvent` (vista funcional) **y** un `AuditRecord` (trazabilidad técnica inmutable) via `IAuditRecorder`.
- No generar `AuditRecord` manualmente desde un handler nuevo sin orquestarlo via `IAuditRecorder.RecordAsync(...)` para mantener el formato unico (`Operation`, `Origin`, `OccurredAt`, `ChangeJson`, `ContextRef`).
- No bloquear transiciones en handlers Application sin pasar por `CaseStatusTransitions.IsValid(from, to, allowed)` — el set `allowed` debe respetarse.
- No omitir el check de autorizacion en handlers de mutacion: usar `IAuthorizationContext.IsInRole(...)` y lanzar `UnauthorizedOperationException` si no aplica (ver `ADR-002`).
- No introducir dependencias con herramientas de automatización específicas (n8n, etc.).
- No crear dependencias circulares ni acceso cruzado a persistencia entre módulos.
- Task **no es un motor BPM**: no contiene lógica de flujo ni ejecuta trabajo. Su ejecucion ocurre fuera de Caimmand; solo se registran su estado, asignatario y resultado.
- No introducir Repository Pattern genérico, AutoMapper, ni capas abstractas no presentes hoy.

## Navegación de la documentación

| Tema | Documento |
|------|-----------|
| Visión, misión, principios y alcance del producto | `docs/00-product/PDD.md` |
| Guía UX y pantallas | `docs/00-product/UX-Guidelines.md` |
| Arquitectura funcional, Command API, límites del sistema | `docs/01-architecture/Architecture.md` |
| Justificación del Modular Monolith y reglas de dependencias | `docs/01-architecture/ADR/ADR-001-Modular-Monolith.md` |
| Auth interim settings-based + autorizacion por rol (Iteracion B) | `docs/01-architecture/ADR/ADR-002-Settings-Based-Auth.md` |
| Entidades del dominio y relaciones | `docs/02-development/DomainModel.md` |
| Alcance y objetivos del MVP | `docs/02-development/MVP.md` |
| Plan técnico del PoC (stack, fases, endpoints, pantallas, decisiones técnicas) | `docs/03-implementation/PoC-Implementation-Plan.md` |
| Ejemplos curl + guia de integracion n8n (secciones 7=Tasks, 8=Participants, 9=Audit, 10=Authz por rol) | `docs/03-implementation/api-examples.md` |

## Iteracion B — registro historico

> Implementada el 2026-08-11. Tres sub-iteraciones:

- **B.1 (Participants + Audit + CaseDefinitions extensions + Priority/Sla)**: entidades `Participant`, `CaseParticipant`, `AuditRecord`; `IAuditRecorder` + retrofit en CreateCase/UpdateCaseStatus/AddTimelineEvent; slices `Participants/Register|List`, `Audit/GetAudit`, `CaseDefinitions/Update|SetActive`; herencia `Priority`/`Sla` de CaseDefinition; `TimelineEvent.ParticipantId?` + `Origin` snapshot.
- **B.2 (Tasks + Dashboard `TasksOverdue`)**: entidad `Task` + slices `Tasks/{Create,Assign,Start,Complete,Cancel,List,GetDetail}`; cada mutation genera TimelineEvent + AuditRecord; nuevo KPI `TasksOverdue`; pagina Blazor `/tasks` y seccion Tareas en CaseView.
- **B.3 (AllowedStatuses + Auth por rol)**: `CaseDefinition.AllowedStatuses` JSONB + value converter EF Core; `CaseStatusTransitions.IsValid(from, to, allowed)`; `InvalidStatusTransitionException`; `IAuthorizationContext` + policies `RequireGerente`/`RequireSupervisorGerente` en Program.cs; checks de rol en handlers (CaseDefinition Create/Update/SetActive + Audit Get = `Gerente`; Tasks/assign = `Supervisor`/`Gerente`; Tasks/start/complete = `Operador`/`Supervisor`/`Api`; Tasks/cancel = `Operador`/`Supervisor`); `<AuthorizeView Roles="...">` en Blazor; pagina `/admin/case-definitions`.

Tests: 129 casos en `tests/Caimmand.Tests/` (build limpio, 0 errores).

## Iteracion B.4 — registro historico

> Implementada el 2026-08-12. Paginacion + filtros por fecha en listado de Casos + avance de filtro `externalId` a SQL.

- **Paginacion offset/limit**: `ListCasesQuery` extendida con `Page`/`PageSize` (default 50). `ListCasesResult` envelope `{ Items, Total, Page, PageSize, TotalPages }`. `ListCasesHandler` reescrito con `CountAsync` + `Skip/Take` en SQL. Endpoint `GET /api/cases` extendido con `page`/`pageSize` params (breaking: response ahora es envelope, no array plano).
- **Filtros por fecha**: `createdFrom`/`createdTo`/`updatedFrom`/`updatedTo` (query params + UI). Presets rapidos en Blazor: Hoy / 7d / 30d.
- **ExternalId a SQL (Iteracion C adelantada)**: `IJsonQueryAdapter` (Domain) + `NpgsqlJsonQueryAdapter` (Infrastructure, usa operador `->>` text extraction) + `InMemoryJsonQueryAdapter` (Tests, fallback runtime). Migracion `IteracionB4_ExternalIdIndex` con `CREATE INDEX IX_Cases_Context_GIN`; B.5 reemplazo el GIN por indice funcional `IX_Cases_Context_ExternalId` (`((Context ->> 'externalId'))`) — type-agnostic y mas liviano.
- **UI Blazor `Cases.razor`**: filtros de fecha (`<input type="date">` x4), select de presets, select de page size (20/50/100), navegacion `« Primera / ‹ Anterior / Página X de Y / Siguiente › / Última »`.

Tests: 138 casos en `tests/Caimmand.Tests/` (build limpio, 0 errores).

## Iteracion B.6 — registro historico

> Implementada el 2026-08-14. Abre el rol `Api` para las transiciones de estado de Caso a `Suspendido`/`Cancelado` (antes solo `Supervisor`/`Gerente`), igualando el branch de `Finalizado`.

- **`UpdateCaseStatusHandler.RequireRoleForTransition`**: el branch `Suspendido`/`Cancelado` ahora acepta `Roles.Api` ademas de `Supervisor`/`Gerente`; mensaje de error actualizado. El branch de `Finalizado` ya lo tenia. La UI Blazor (`CaseView.razor`) no se toca: `Api` solo existe para API key (cookie users son Operador/Supervisor/Gerente), asi que los botones de transicion siguen envueltos en `<AuthorizeView Roles="Supervisor,Gerente">`.
- **Rescision de oversight humano**: esto rescinde el principio de "oversight humano en cancelacion" documentado previamente en `api-examples.md` (seccion 6) y el PDD. n8n (rol `Api`) ahora puede suspender y cancelar casos directamente. La maquina de estados (`CaseStatusTransitions`) NO se modifico: `Creado → Cancelado` sigue invalido por transicion (no por rol), y `Creado → Suspendido` tampoco existe. Tarea cancel (`Tasks/cancel`) sigue requiriendo `Operador`/`Supervisor` (sin `Api`) — el cambio aplica solo a estado de Caso.
- **Docs actualizadas**: `ADR-002`, `Architecture.md`, `api-examples.md` (seccion 6: principios, distribucion de estados, workflow 3, reglas de oro, tabla de permisos n8n), `MVP.md`.

Tests: 140 casos en `tests/Caimmand.Tests/` (+2: `Suspendido_AsApi_Succeeds`, `Cancelado_AsApi_Succeeds`) (build limpio, 0 errores).

## Inconsistencias encontradas

> Las inconsistencias documentadas originalmente entre `docs/` y el código fueron resueltas el 2026-07-28 corrigiendo la documentación. Se conservan a continuación como registro histórico:

- **Extensión de la solución** (resuelto): `PoC-Implementation-Plan.md` describía `src/Caimmand.sln`; corregido a `src/Caimmand.slnx`, extensión real del repositorio.
- **Acceso a DbContext desde Application** (resuelto): `PoC-Implementation-Plan.md` afirmaba *"Application consume CaimmandDbContext directamente"* y *"Sin Repository Pattern... sin interfaces"*. Corregido: el PoC usa `ICaimmandDbContext` (definida en `Caimmand.Domain`), los handlers consumen esa interfaz, y `CaimmandDbContext` (Infrastructure) la implementa. EF Core sigue siendo la unica abstraccion de persistencia; no se introducen interfaces de repositorio adicionales.