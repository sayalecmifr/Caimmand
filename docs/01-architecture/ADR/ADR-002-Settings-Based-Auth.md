# ADR-002 - Settings-Based Auth (Interim)

| Campo    | Valor                 |
|----------|-----------------------|
| Producto | Caimmand              |
| Version  | 0.1                   |
| Estado   | Accepted              |
| Fecha    | 2026-07-23            |
| Autor    | CAI Process Grid Team |

> Login basico por settings: cubre la puerta de entrada mientras se decide la auth de produccion.

## Contexto

El PoC (Fase 4 cerrada) mantiene UI Blazor y Command API expuestas sin autenticacion. `MVP.md` y `Architecture.md` declaran Keycloak / Identity fuera del alcance del MVP, y ADR-001 enumera el modulo `Identity` como "previsto". Sin embargo, antes de exponer el contenedor a una red interna o salir del localhost de desarrollo, se necesita una puerta de entrada simple.

Este ADR documenta un esquema **interim**: cuker auth UI + API key por settings. No reemplaza la futura auth OIDC (Keycloak); la precede como medida minima y reemplazable.

## Decisiones

1. **UI Blazor** protegida con **cookie auth** (`Microsoft.AspNetCore.Authentication.Cookies`).
   - Lista de usuarios en `appsettings.json` seccion `Auth:Users[]` con `Username`, `Password`, `Role`.
   - Passwords en **plaintext** en settings (documentado como no production-grade).
   - Roles declarados como claims `ClaimTypes.Role` con valores `Operador` / `Supervisor` / `Gerente` (alineados al PDD y a `MVP.md` seccion "Roles y permisos MVP").
   - Sesion cookie 8h con sliding expiration.

2. **Command API** (`/api/...`) protegida con header **`X-API-Key`** validado contra `Auth:ApiKey` en settings.
   - n8n (y otros orquestadores) agregan el header en todos los HTTP Request nodes.
   - El valor default `caimmand-poc-key` esta en `appsettings.json` y **debe sobreescribirse** en todo deployment que no sea dev local (via env var `Auth__ApiKey` en Docker Compose / `appsettings.Production.json`).

3. **Politica "ApiAuth"** unifica ambos esquemas: requiere usuario autenticado y acepta indistintamente cookie (browser) o API key (n8n). Configurada via `AddAuthenticationSchemes(Cookie, ApiKey)` en la policy.

4. **Endpoints de cuenta**:
   - `POST /account/login` recibe form (`username`, `password`, `returnUrl`), valida contra `LoginService.TryValidate`, emite cookie via `HttpContext.SignInAsync`, redirige a `returnUrl` o `/`.
   - `POST /account/logout` limpia la cookie, redirige a `/login`.
   - Pagina `/login` Blazor con `EmptyLayout` y `[AllowAnonymous]`.

5. **Roles con autorizacion fina (Iteracion B)**:
    - Se aplican restricciones por rol en endpoints y handlers (ver seccion "Autorizacion por rol" abajo).
    - Los claims `Role` se emiten para los 3 roles del PDD: `Operador` / `Supervisor` / `Gerente`.
    - Adicionalmente, la auth API key emite el claim `Role = Api` para que los orquestadores externos (n8n) puedan operar tareas y transiciones permitidas (Finalizar Caso, Start/Complete Task) sin permisos de administracion.

### Autorizacion por rol (Iteracion B)

Implementada en `Application/Authorization/IAuthorizationContext.cs` + `Web/Authorization/HttpAuthorizationContext.cs`. Los handlers consumen `IAuthorizationContext.IsInRole(...)` y lanzan `UnauthorizedOperationException` si el rol no esta autorizado. Los endpoints de Minimal API aplican policies `RequireGerente` / `RequireSupervisorGerente` configuradas en `Program.cs`.

| Operacion | Roles permitidos | Donde se aplica |
|-----------|------------------|-----------------|
| `POST /api/cases`, `GET /api/cases[/{id}]` | Cualquiera autenticado (UI + API) | Endpoint `ApiAuth` |
| `POST /api/cases/{id}/timeline` | Cualquiera autenticado (UI + API) | Endpoint `ApiAuth` |
| `PATCH /api/cases/{id}/status` (a `EnCurso`) | Cualquiera autenticado | Handler (post-endpoint) |
| `PATCH /api/cases/{id}/status` (a `Suspendido`/`Cancelado`) | `Supervisor` / `Gerente` | Handler (post-endpoint) |
| `PATCH /api/cases/{id}/status` (a `Finalizado`) | `Supervisor` / `Gerente` / `Api` | Handler (post-endpoint) |
| `POST /api/cases/{id}/participants` | Cualquiera autenticado (UI + API) | Endpoint `ApiAuth` |
| `POST /api/cases/{id}/tasks`, `GET /api/cases/{id}/tasks[/{tid}]` | Cualquiera autenticado (UI + API) | Endpoint `ApiAuth` |
| `PATCH /api/cases/{id}/tasks/{tid}/start` | `Operador` / `Supervisor` / `Api` | Handler (post-endpoint) |
| `PATCH /api/cases/{id}/tasks/{tid}/complete` | `Operador` / `Supervisor` / `Api` | Handler (post-endpoint) |
| `PATCH /api/cases/{id}/tasks/{tid}/cancel` | `Operador` / `Supervisor` | Handler (post-endpoint) |
| `PATCH /api/cases/{id}/tasks/{tid}/assign` | `Supervisor` / `Gerente` | Endpoint `RequireSupervisorGerente` |
| `POST /api/case-definitions`, `PATCH /api/case-definitions/{id}*` | `Gerente` | Endpoint `RequireGerente` + handler |
| `GET /api/cases/{id}/audit` | `Gerente` | Endpoint `RequireGerente` + handler |

UI Blazor: `CaseView.razor` envuelve los botones de transicion de estado en `<AuthorizeView Roles="Supervisor,Gerente">`; `NavMenu.razor` muestra "Case Definitions" solo a `Gerente`; `CaseDefinitions.razor` (`/admin/case-definitions`) tiene `@attribute [Authorize(Roles = "Gerente")]`.

## Consecuencias

**Positivas**:
- La UI no es accesible anonimamente. Si abris `http://localhost:8080/` te redirige a `/login`.
- La API no es accedida sin credencial. `curl http://localhost:8080/api/cases` (sin header) devuelve `401`.
- n8n sigue funcionando — solo agrega `X-API-Key: <key>` a sus HTTP Request nodes.
- Migracion a Keycloak futura reemplaza `AuthOptions` por esquema OIDC; los claim names (`Name`, `Role`) se mantienen compatibles, asi los handlers authorize-by-role no cambian.

**Negativas / riesgos**:
- Passwords en plaintext en `appsettings.json` — no production-grade. Si el archivo leaks o se commitea por error, las credenciales se comprometen. **Backlog (iteracion futura)**: hashear con PBKDF2/Argon2; requiere tooling para generarlos offline.
- API key compartida (un solo string) — n8n la usa para todo. No hay diferenciacion por sistema de origen. **Backlog**: rotacion + multi-key (por sistema externo) cuando el volumen lo justifique.
- `Auth:ApiKey` default declarado en `appsettings.json` — conviene **sobre escribir siempre** en cualquier ambiente que no sea dev local del autor.

## Alternativas consideradas

- **Sin auth hasta Keycloak**: rechazada porque deja el PoC expuesto antes de la integracion real.
- **API_key global unica para UI y API**: rechazada porque pierde la nocion de 3 roles del PDD/UX, que el PoC ya documenta como futura autorizacion por rol.
- **Identity integrado (ASP.NET Core Identity con DB de usuarios)**: rechazada por scope — requiere migracion, entidades de dominio DB y mas codigo del que esta iteracion pide. Reservado para la auth de produccion.

## Backlog explicito

- **Iteracion B — autorizacion por rol (IMPLEMENTADA 2026-08-11)**: las policies `RequireGerente` y `RequireSupervisorGerente` y los checks `IAuthorizationContext.IsInRole(...)` en handlers estan activos. Originalmente pendiente en la Fase 4 del PoC; materializada junto con Tasks, Participants y Audit (ver `MVP.md` y `Architecture.md`).
- **Iteracion futura (post-PoC)** — hashing de passwords en settings.
- **Iteracion futura (post-PoC)** — API key por sistema externo + rotacion.
- **Iteracion futura (post-PoC, fuera de ADR-002)** — migracion a Keycloak/IdentityServer: sustituye `AuthOptions` por esquema OIDC; ADR-003 cubrira ese cambio.

## Estado

Aceptado e implementado en `src/Caimmand.Web/Auth/` + `Program.cs`. Sera sustituido por una ADR de auth de produccion cuando se decida el mecanismo OIDC.