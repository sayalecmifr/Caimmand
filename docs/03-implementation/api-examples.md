# Caimmand - API Examples (Fase 4)

| Campo    | Valor                |
|----------|----------------------|
| Producto | Caimmand             |
| Version  | 0.1                  |
| Estado   | Draft                |
| Fecha    | 2026-07-21           |

Contrato de la **Command API** expuesto por `Caimmand.Web` (Minimal APIs bajo `/api/...`). Pensado para que sistemas externos (HIS, automatizaciones n8n, otros orquestadores) sea el unico punto de entrada autorizado para crear casos y reportar eventos, sin tocar la base de datos ni el codigo de Caimmand.

- Base URL: `http://localhost:8080` cuando se corre via **Docker Compose** (default). Si se corre localmente con `dotnet run` (Opcion B del README), usar `https://localhost:5001`.
- Content-Type: `application/json` en todos los `POST`/`PATCH`.
- Auth: la Command API exige el header `X-API-Key: <key>` (ver ADR-002). El valor default es `caimmand-poc-key` (override via env var `Auth__ApiKey` en Docker Compose). La UI Blazor requiere login por cookie. Keycloak sigue fuera del MVP. Iteracion B — IMPLEMENTADA 2026-08-11: algunos endpoints requieren roles adicionales (ver seccion "8. Autorizacion por rol" abajo y `ADR-002-Settings-Based-Auth.md`).
- Fechas: UTC en todas las responses (`DateTime.UtcNow`).

> Todos los ejemplos `curl` y n8n de este documento asumen Docker Compose (`http://localhost:8080`). Si corres local, sustitui el host/puerto por `https://localhost:5001`. Todos los endpoints `/api/...` requieren header `X-API-Key: caimmand-poc-key` (o el valor definido en `.env`).

---

## 1. Casos

### 1.1 POST /api/cases

Crea un nuevo Caso. La `CaseDefinition` referenciada debe existir y estar activa. El caso arranca en estado `Creado` y se siembra automaticamente un evento de Timeline de tipo `Creacion` con `Origin = SourceSystem`.

**Request body**

```json
{
  "caseDefinitionCode": "APPOINTMENT_REMINDER",
  "title": "Recordatorio del turno de Juan Perez",
  "sourceSystem": "HIS",
  "context": {
    "externalId": "APT-2026-0718-001",
    "patientId": 12345,
    "patientName": "Juan Perez",
    "appointmentDate": "2026-07-18T10:30",
    "doctor": "Dra. Lopez"
  }
}
```

- `caseDefinitionCode` (string, obligatorio): codigo de la definicion activa.
- `title` (string, obligatorio).
- `sourceSystem` (string, obligatorio): identificador del sistema de origen (ej. `HIS`).
- `context` (JSON object, obligatorio): libre, segun el tipo de caso. Lo que el operador vera en la UI. Se recomienda incluir `externalId` cuando el sistema de origen lo provea (ver 6.6 — idempotencia).

**Response 201 Created**

```json
{
  "id": "52abb42f-1234-5678-9abc-def012345678",
  "status": "Creado",
  "createdAt": "2026-07-21T14:30:00.000Z"
}
```

- `Location` header: `/api/cases/{id}`.

**Errores**

- `404` si la `CaseDefinition` no existe.
- `422` (ValidationProblem) si faltan campos obligatorios o la definicion esta inactiva.

**curl**

```bash
curl -X POST http://localhost:8080/api/cases \
  -H "X-API-Key: caimmand-poc-key" \
  -H "Content-Type: application/json" \
  -d '{
    "caseDefinitionCode": "APPOINTMENT_REMINDER",
    "title": "Recordatorio del turno de Juan Perez",
    "sourceSystem": "HIS",
    "context": {
      "externalId": "APT-2026-0718-001",
      "patientId": 12345,
      "patientName": "Juan Perez",
      "appointmentDate": "2026-07-18T10:30",
      "doctor": "Dra. Lopez"
    }
  }'
```

**n8n HTTP Request Node**

- Method: `POST`
- URL: `http://localhost:8080/api/cases`
- Authentication: none (PoC)
- Headers: `X-API-Key: caimmand-poc-key`, `Content-Type: application/json`
- Body (JSON): el objeto de arriba. Tipicamente se mapea desde el output del nodo que leyo del HIS.

---

### 1.2 GET /api/cases

Lista casos con filtros opcionales y paginación del lado del servidor. La response es un **envelope** con `items`, `total`, `page`, `pageSize` y `totalPages` (no es un array plano).

**Query params**

- `status` (opcional): uno de `Creado`, `EnCurso`, `Suspendido`, `Finalizado`, `Cancelado` (case-insensitive).
- `caseDefinitionCode` (opcional): el codigo exacto de la definicion.
- `externalId` (opcional): filtra por `Context.externalId` en SQL via `EF.Functions.JsonContains` + indice GIN (ver 6.6). Identifica la unidad de trabajo dentro del `sourceSystem`. No confundir con `sourceSystem` mismo.
- `createdFrom` (opcional, ISO 8601 UTC): filtra casos con `CreatedAt >= createdFrom`.
- `createdTo` (opcional, ISO 8601 UTC): filtra casos con `CreatedAt <= createdTo`.
- `updatedFrom` (opcional, ISO 8601 UTC): filtra casos con `UpdatedAt >= updatedFrom`.
- `updatedTo` (opcional, ISO 6087 UTC): filtra casos con `UpdatedAt <= updatedTo`.
- `page` (opcional, default 1): numero de pagina (1-based).
- `pageSize` (opcional, default 50): cantidad de items por pagina.

**curl**

```bash
curl -H "X-API-Key: caimmand-poc-key" "http://localhost:8080/api/cases?status=Suspendido&caseDefinitionCode=APPOINTMENT_REMINDER&page=1&pageSize=50"
```

Con filtro de fecha (casos creados en los ultimos 7 dias):

```bash
curl -H "X-API-Key: caimmand-poc-key" "http://localhost:8080/api/cases?createdFrom=2026-08-05T00:00:00Z&page=1&pageSize=50"
```

Lookup por `externalId` (tipico para idempotencia en n8n):

```bash
curl -H "X-API-Key: caimmand-poc-key" "http://localhost:8080/api/cases?caseDefinitionCode=APPOINTMENT_REMINDER&externalId=APT-2026-0718-001"
```

Si response tiene `total: 0` → no existe caso para ese turno, se puede crear. Si trae un elemento → skip.

**Response 200 OK**

```json
{
  "items": [
    {
      "id": "52abb42f-...",
      "title": "Recordatorio del turno de Juan Perez",
      "caseDefinitionCode": "APPOINTMENT_REMINDER",
      "caseDefinitionName": "Recordatorio de Turno",
      "status": "Suspendido",
      "sourceSystem": "HIS",
      "createdAt": "2026-07-21T14:30:00.000Z"
    }
  ],
  "total": 1,
  "page": 1,
  "pageSize": 50,
  "totalPages": 1
}
```

> **Breaking change (B.4, 2026-08-12)**: la response solia ser un array plano `[...]`. Ahora es un envelope `{ items, total, page, pageSize, totalPages }`. Los clientes n8n existentes deben adaptarse (no hay workflows en produccion). El filter por `externalId` antes era runtime en memoria; ahora es SQL via `EF.Functions.JsonContains` con indice GIN sobre `Cases.Context`.

---

### 1.3 GET /api/cases/{id}

Detalle de un caso. Devuelve `404` si no existe.

**curl**

```bash
curl -H "X-API-Key: caimmand-poc-key" http://localhost:8080/api/cases/52abb42f-1234-5678-9abc-def012345678
```

**Response 200 OK**

```json
{
  "id": "52abb42f-...",
  "title": "Recordatorio del turno de Juan Perez",
  "caseDefinitionCode": "APPOINTMENT_REMINDER",
  "caseDefinitionName": "Recordatorio de Turno",
  "status": "EnCurso",
  "sourceSystem": "HIS",
  "context": {
    "patientId": 12345,
    "patientName": "Juan Perez"
  },
  "createdAt": "2026-07-21T14:30:00.000Z",
  "updatedAt": "2026-07-21T15:00:00.000Z"
}
```

---

### 1.4 PATCH /api/cases/{id}/status

Cambia el estado del caso segun la maquina de transiciones definida en `Domain/Enums/CaseStatusTransitions.cs`. Registra un evento de Timeline tipado (`Inicio de operacion`, `Suspension`, `Reactivacion`, `Finalizacion`, `Cancelacion`).

**Transiciones validas**

- `Creado` → `EnCurso`
- `EnCurso` → `Suspendido`, `Finalizado`, `Cancelado`
- `Suspendido` → `EnCurso`, `Cancelado`
- `Finalizado` / `Cancelado`: estados terminales, sin transiciones.

> Nota: la transicion `Creado → Cancelado` esta pendiente de revision con el equipo (ver `docs/02-development/Backlog.md`).

**Request body**

```json
{ "newStatus": "Finalizado" }
```

**curl**

```bash
curl -X PATCH http://localhost:8080/api/cases/52abb42f-.../status \
  -H "X-API-Key: caimmand-poc-key" \
  -H "Content-Type: application/json" \
  -d '{ "newStatus": "Finalizado" }'
```

**Response 200 OK**

```json
{
  "id": "52abb42f-...",
  "status": "Finalizado",
  "updatedAt": "2026-07-21T16:00:00.000Z"
}
```

---

## 2. Timeline

### 2.1 POST /api/cases/{id}/timeline

Agrega un evento a la timeline del caso. El handler calcula automaticamente el siguiente `Sequence` (maximo + 1). Es como n8n reporta pasos de la automatizacion (SMS enviado, confirmacion recibida, error, etc.) para que queden visibles al operador en la UI.

**Request body**

```json
{
  "type": "Aviso",
  "origin": "n8n",
  "content": "SMS enviado al paciente Juan Perez al +541112345678."
}
```

- `type` (string, obligatorio): libre pero recomendado canonicalo (ver glosario abajo).
- `origin` (string, obligatorio): quien genera el evento (`HIS`, `n8n`, `Operador`, `Sistema`).
- `content` (string, obligatorio).

**Glosario de tipos sugeridos para APPOINTMENT_REMINDER**

| Type          | Origin tipico | Uso                                            |
|---------------|---------------|------------------------------------------------|
| Creacion      | HIS           | Siembra automatica al crear el caso.           |
| Aviso         | n8n           | SMS/WhatsApp enviado.                          |
| Recordatorio  | n8n           | Reenvio / recordatorio secundario.             |
| Confirmacion  | n8n           | Paciente confirmo asistencia.                  |
| Cancelacion   | n8n / Operador| Paciente cancelo el turno.                     |
| Comentario    | Operador      | Nota manual del operador (desde UI).           |
| Llamado       | Operador      | Llamada telefonica manual.                     |

**Response 201 Created**

```json
{
  "id": "abc12345-...",
  "caseId": "52abb42f-...",
  "sequence": 2,
  "occurredAt": "2026-07-21T15:00:00.000Z"
}
```

**curl**

```bash
curl -X POST http://localhost:8080/api/cases/52abb42f-.../timeline \
  -H "X-API-Key: caimmand-poc-key" \
  -H "Content-Type: application/json" \
  -d '{
    "type": "Aviso",
    "origin": "n8n",
    "content": "SMS enviado al paciente Juan Perez al +541112345678."
  }'
```

**n8n HTTP Request Node**

- Method: `POST`
- URL: `http://localhost:8080/api/cases/{{ $json.caseId }}/timeline`
- Body (JSON):

```json
{
  "type": "Aviso",
  "origin": "n8n",
  "content": "SMS enviado al paciente {{ $json.patientName }}."
}
```

---

### 2.2 GET /api/cases/{id}/timeline

Devuelve los eventos ordenados por `Sequence` descendente.

**curl**

```bash
curl -H "X-API-Key: caimmand-poc-key" http://localhost:8080/api/cases/52abb42f-.../timeline
```

**Response 200 OK**

```json
[
  {
    "id": "abc12345-...",
    "sequence": 2,
    "type": "Aviso",
    "origin": "n8n",
    "content": "SMS enviado.",
    "occurredAt": "2026-07-21T15:00:00.000Z"
  },
  {
    "id": "def67890-...",
    "sequence": 1,
    "type": "Creacion",
    "origin": "HIS",
    "content": "Caso creado por HIS.",
    "occurredAt": "2026-07-21T14:30:00.000Z"
  }
]
```

---

## 3. Case Definitions

### 3.1 GET /api/case-definitions

Lista las definiciones de caso registradas, ordenadas por `Name`. Incluye activas e inactivas.

**curl**

```bash
curl -H "X-API-Key: caimmand-poc-key" http://localhost:8080/api/case-definitions
```

**Response 200 OK**

```json
[
  {
    "id": "11111111-...",
    "code": "APPOINTMENT_REMINDER",
    "name": "Recordatorio de Turno",
    "description": "Recordatorio automatico de turnos medicos",
    "category": "Appointments",
    "isActive": true,
    "defaultSla": null,
    "defaultPriority": "Media",
    "displayColor": "#3b82f6",
    "displayIcon": "calendar"
  }
]
```

---

### 3.2 POST /api/case-definitions

Registra una nueva `CaseDefinition`. Permite incorporar nuevos tipos de caso (ej. `MEDICAL_AUDIT`) sin editar el seed de `Program.cs` ni redeployar codigo; basta un POST desde un cliente autorizado.

**Request body**

```json
{
  "code": "MEDICAL_AUDIT",
  "name": "Auditoria Medica",
  "description": "Auditoria de historias clinicas en batch",
  "category": "Audit",
  "defaultPriority": "Alta",
  "displayColor": "#dc3545",
  "displayIcon": "clipboard-check"
}
```

- `code` (string, obligatorio, unico): no debe existir otra definicion con ese codigo.
- `name` (string, obligatorio).
- `description` (string, obligatorio).
- `category` (string, opcional).
- `defaultPriority` (string, obligatorio): uno de `Baja`, `Media`, `Alta`, `Urgente`.
- `displayColor` (string, obligatorio): color hex `#RRGGBB` (ej. `#3b82f6`).
- `displayIcon` (string, obligatorio): nombre de icono (ej. `calendar`, `clipboard-check`).

La nueva definicion arranca siempre con `IsActive = true` (en el PoC no hay una operacion de desactivacion expuesta; es cambio directo en DB o eliminacion logica futura).

**Response 201 Created**

```json
{
  "id": "22222222-...",
  "code": "MEDICAL_AUDIT"
}
```

- `Location` header: `/api/case-definitions/{id}`.

**Errores**

- `422` (ValidationProblem) si algun campo es invalido o `code` ya existe.

**curl**

```bash
curl -X POST http://localhost:8080/api/case-definitions \
  -H "X-API-Key: caimmand-poc-key" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "MEDICAL_AUDIT",
    "name": "Auditoria Medica",
    "description": "Auditoria de historias clinicas en batch",
    "category": "Audit",
    "defaultPriority": "Alta",
    "displayColor": "#dc3545",
    "displayIcon": "clipboard-check"
  }'
```

---

## 4. Flujo end-to-end APPOINTMENT_REMINDER (n8n)

Referencia del flujo completo que un sistema externo (HIS + n8n) ejecuta contra la Command API. Caimmand no envia el SMS; solo registra y hace visible el caso.

1. **HIS (via n8n) lee turnos** del sistema de turnos y, para cada turno, hace un `POST /api/cases` con `caseDefinitionCode = APPOINTMENT_REMINDER`, `sourceSystem = HIS` y `context` con `patientId`, `patientName`, `appointmentDate`, `doctor`.
2. **Caimmand** crea el caso en `Creado`, siembra evento de Timeline `Creacion` (origin `HIS`) y devuelve el `id`.
3. **n8n** toma el `id` devuelto y, luego de enviar el SMS via el proveedor externo, hace `POST /api/cases/{id}/timeline` con `Type = Aviso`, `Origin = n8n` y `Content` describiendo el envio.
4. **n8n** (opcional) programa un reenvio y reporta `Type = Recordatorio` con su `Content`.
5. Si el paciente responde (webhook de WhatsApp/SMS gateway), **n8n** reporta `Type = Confirmacion` o `Type = Cancelacion` via timeline.
6. El **operador** abre el detalle del caso en Blazor (`/cases/{id}`) y ve toda la timeline. Puede agregar manualmente eventos (`Type = Llamado`, `Type = Comentario`) desde la UI si intervino por telefono u otra va.
7. El caso avanza de estado: `Creado → EnCurso` (al primer evento n8n que arranca operacion; en el PoC se hace manual o desde UI), y finalmente `→ Finalizado` o `→ Cancelado` por el operador, lo cual siembra eventos tipados `Finalizacion` / `Cancelacion`.

> Nota: Iteracion B — IMPLEMENTADA 2026-08-11. Las "tareas" (`Enviar SMS`, `Esperar confirmacion`) ahora son entidades estructuradas (`Task`) con su propio estado (`Pendiente` / `EnProgreso` / `Completada` / `Cancelada`) y `Result` al cerrarse. Los KPIs de "tareas vencidas" estan activos en el Dashboard (`TasksOverdue`). Ver seccion "7. Tasks" abajo.

---

## 5. Como correr la API

### Opcion A - Docker Compose (default)

```bash
# 1) Copiar variables de entorno (solo la primera vez)
cp .env.example .env

# 2) Levantar PostgreSQL + Web
docker compose up --build
```

- API: `http://localhost:8080/api/...`
- UI: `http://localhost:8080/` (Dashboard / Casos / Detalle del Caso)

PostgreSQL levanta con healthcheck y la Web espera a que este listo. Al arrancar, Caimmand aplica migraciones de EF Core y siembra la definicion `APPOINTMENT_REMINDER` si la tabla `CaseDefinitions` esta vacia.

Variables configurables en `.env` (ver `.env.example`): `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`, `POSTGRES_HOST`, `POSTGRES_PORT`, `WEB_PORT`.

### Opcion B - Local (dev / debugger)

```bash
# 1) Levantar PostgreSQL (Docker)
./scripts/run-postgres.ps1

# 2) Restaurar y compilar
dotnet build src/Caimmand.slnx

# 3) Arrancar la Web (Blazor Server + Minimal API juntos)
dotnet run --project src/Caimmand.Web
```

- API: `https://localhost:5001/api/...`
- UI: `https://localhost:5001/`

Conexion PostgreSQL por defecto (en `appsettings.Development.json`):

```
Host=localhost;Port=5432;Database=caimmand;Username=postgres;Password=postgres
```

Al arrancar, Caimmand aplica migraciones de EF Core y siembra la definicion `APPOINTMENT_REMINDER` si la tabla `CaseDefinitions` esta vacia.

---

## 6. Guia de integracion n8n

Referencia operativa para configurar los workflows de n8n contra la Command API de Caimmand. Cubre el caso `APPOINTMENT_REMINDER` (Recordatorio de Turno). La seccion 4 describe el flujo a nivel funcional; esta seccion entra en detalle de implementacion por workflow.

### 6.1 Principios

- **`sourceSystem` ≠ `Origin`**: el HIS es el sistema de origen del caso (`sourceSystem: "HIS"`); n8n es el transporte/orquestador y firma como `Origin: "n8n"` en los eventos de timeline que reporta.
- **Operator oversight**: n8n nunca mueve un caso a `Cancelado`. n8n si finaliza (`Finalizado`) cuando el paciente confirma explicitamente.
- **Auth API**: todo HTTP Request Node de n8n debe llevar el header `X-API-Key: caimmand-poc-key` (o el valor definido en `.env` → `AUTH_API_KEY`). Sin ese header, todos los endpoints `/api/...` devuelven `401 Unauthorized`. Ver ADR-002.
- **Idempotencia convencional**: `Context.externalId` es la **clave Caimmand-side** para idempotencia. n8n mapea el id que el HIS le dé (turnoId, appointmentId, codigo de turno, etc.) a esa clave al crear el caso. Si el HIS no provee id estable, skip idempotencia: POST directo y aceptar duplicados en re-runs (ver 6.6).
- **Toda accion relevante genera TimelineEvent**: envio, reenvio, confirmacion, error. Si hiciste algo, postealo.
- **Content legible y especifico**: nunca "OK" ni "fallo". Incluir proveedor, MSID, phone mascarado, texto de error crudo.

### 6.2 Distribucion de estados

| Transicion | La dispara | Trigger |
|---|---|---|
| `Creado → EnCurso` | n8n (Workflow 2) | Inicio del envio del SMS |
| `EnCurso → Suspendido` | n8n (Workflow 2) | Falla del proveedor SMS o timeout sin respuesta |
| `Suspendido → EnCurso` | Operador | Tras resolver el problema manualmente |
| `EnCurso → Finalizado` | n8n (Workflow 3) | Paciente confirma via webhook |
| `EnCurso → Cancelado` | Operador | Paciente cancela (n8n solo reporta `Cancelacion` en timeline) |

> La transicion `Creado → Cancelado` esta pendiente de revision con el equipo; no se usa en este flujo.

### 6.3 Workflow 1: Ingesta HIS → Caimmand

Crea un caso por cada turno del dia leido del HIS. Idempotente por `externalId` **cuando el HIS provee un id estable** (ver 6.6 — fallback si no lo hay).

```
+-----------------------------------------------------------+
|  WF1: Ingesta HIS → Caimmand                              |
|  Trigger: schedule cada N min (o webhook del HIS)        |
+-----------------------------------------------------------+
        |
        v
   [GET turnos del dia desde HIS]
        |
        v
   [Loop por turno]
        |
        +-- Tiene id estable (HIS-side, label cual sea)
        |       |
        |       v
        |   [GET /api/cases?caseDefinitionCode=APPOINTMENT_REMINDER&externalId={{ id }}]
        |       |-- []   --> no existe --> POST (Branch A)
        |       |-- [1]  --> ya existe --> skip
        |
        +-- No tiene id estable
                |
                v
            [Skip GET, POST directo] (Branch B, acepta dupes)
        |
        v
   [POST /api/cases  (Creado, sourceSystem=HIS)]
        |
        v
   [Guardar caseId <-> externalId en Static Data (solo Branch A)]
```

**Paso 1 — GET turnos desde el HIS**

Configuracion del HIS fuera de Caimmand (HTTP Request o nodo SQL/DB). Output: lista de turnos con los campos que el HIS exponga — tipicamente `patientId`, `patientName`, `patientPhone`, `appointmentDate`, `doctor`, `doctorSpecialty`, y opcionalmente un id estable (turnoId, appointmentId, codigo de turno, etc.).

**Paso 2 — Branch condicional sobre idempotencia**

El `externalId` es la **clave Caimmand-side** (convencion fija en el handler). n8n decide segun el HIS:

- **Branch A — el HIS provee un id estable** (con cualquier label HIS-side, ej. `turnoId`):
  - n8n lo publica como `Context.externalId` al crear (ver paso 3).
  - n8n HTTP Request Node (lookup idempotencia):
    - Method: `GET`
    - URL: `http://localhost:8080/api/cases?caseDefinitionCode=APPOINTMENT_REMINDER&externalId={{ $json.turnoId }}`
  - Si la response es `[]` → no existe caso, se continua con el POST. Si trae un elemento → skip. El filtro por `externalId` se resuelve en Caimmand (ver 6.6); no hace falta filtrar client-side.

- **Branch B — el HIS no provee id estable**:
  - Skip lookup. Post directo en paso 3 (sin `externalId` en `Context`).
  - Aceptar que re-runs del WF1 pueden crear duplicados en el Dashboard. Trade-off conscious del PoC; el operador reconcilia manualmente si detecta dupes.

**Paso 3 — POST /api/cases**

n8n HTTP Request Node:
- Method: `POST`
- URL: `http://localhost:8080/api/cases`
- Headers: `X-API-Key: caimmand-poc-key`, `Content-Type: application/json`

Body Branch A (con idempotency key):

```json
{
  "caseDefinitionCode": "APPOINTMENT_REMINDER",
  "title": "Recordatorio - {{ $json.patientName }} - {{ $json.appointmentDate }}",
  "sourceSystem": "HIS",
  "context": {
    "externalId": "{{ $json.turnoId }}",
    "patientId": {{ $json.patientId }},
    "patientName": "{{ $json.patientName }}",
    "patientPhone": "{{ $json.patientPhone }}",
    "appointmentDate": "{{ $json.appointmentDate }}",
    "doctor": "{{ $json.doctor }}",
    "doctorSpecialty": "{{ $json.doctorSpecialty }}"
  }
}
```

Body Branch B (sin idempotency key — omitir `externalId`):

```json
{
  "caseDefinitionCode": "APPOINTMENT_REMINDER",
  "title": "Recordatorio - {{ $json.patientName }} - {{ $json.appointmentDate }}",
  "sourceSystem": "HIS",
  "context": {
    "patientId": {{ $json.patientId }},
    "patientName": "{{ $json.patientName }}",
    "patientPhone": "{{ $json.patientPhone }}",
    "appointmentDate": "{{ $json.appointmentDate }}",
    "doctor": "{{ $json.doctor }}",
    "doctorSpecialty": "{{ $json.doctorSpecialty }}"
  }
}
```

**Paso 4 — Mapeo caso ↔ externalId** (Branch A solo)

Guardar `caseId` (devuelto por el `POST`) junto a `externalId` (el valor HIS-side) en n8n Static Data (o Redis / tabla simple, ver 6.7). En Branch B no hay mapeo idempotente, pero se puede guardar `caseId` por `appointmentDate + patientName` para correlacionar en WF2.

### 6.4 Workflow 2: Envio SMS (24h antes del turno, por caso)

Envia el SMS y registra el evento. Ante falla, suspende automaticamente para que el caso caiga en "Requieren Intervencion" del Dashboard.

```
+-----------------------------------------------------------+
|  WF2: SMS Recordatorio (24h antes del turno)              |
|  Trigger: Schedule                                        |
+-----------------------------------------------------------+
        |
        v
   [Lookup caseId <- Static Data]
        |
        v
   [PATCH /status -> EnCurso]
        |
        v
   [Enviar SMS (proveedor externo)]
        |
        +-- OK ----> [POST timeline Aviso] ---> WF3 escucha
        |
        +-- Fail --> [POST timeline Error]
                       |
                       v
                    [PATCH /status -> Suspendido]
                       |
                       v
                    Dashboard "Requieren Intervencion"
```

**Paso 1 — Lookup caseId**

Leer del Static Data (o equivalente) el `caseId` asociado al `externalId` del turno.

**Paso 2 — PATCH /status (Creado → EnCurso)**

n8n HTTP Request Node:
- Method: `PATCH`
- URL: `http://localhost:8080/api/cases/{{ $json.caseId }}/status`
- Headers: `X-API-Key: caimmand-poc-key`, `Content-Type: application/json`
- Body:

```json
{ "newStatus": "EnCurso" }
```

**Paso 3 — Enviar SMS**

nodo del proveedor (Twilio, Meta WhatsApp, etc.). Fuera del alcance de esta guia; lo importante es que el resultado defina la rama OK/Fail.

**Paso 4a — Rama OK: POST timeline Aviso**

n8n HTTP Request Node:
- Method: `POST`
- URL: `http://localhost:8080/api/cases/{{ $json.caseId }}/timeline`
- Headers: `X-API-Key: caimmand-poc-key`, `Content-Type: application/json`
- Body:

```json
{
  "type": "Aviso",
  "origin": "n8n",
  "content": "SMS enviado a {{ $json.patientPhoneMasked }} vía {{ $json.provider }}. MSID: {{ $json.msid }}."
}
```

Despues de este paso, el Workflow 3 (webhook) queda a la escucha de la respuesta del paciente.

**Paso 4b — Rama Fail/Timeout: POST timeline Error + Suspendido**

Primero el evento:

n8n HTTP Request Node:
- Method: `POST`
- URL: `http://localhost:8080/api/cases/{{ $json.caseId }}/timeline`
- Body:

```json
{
  "type": "Error",
  "origin": "n8n",
  "content": "Falla envio SMS vía {{ $json.provider }}: {{ $json.errorMessage }}. Intentos: {{ $json.attempts }}."
}
```

Despues el cambio de estado:

n8n HTTP Request Node:
- Method: `PATCH`
- URL: `http://localhost:8080/api/cases/{{ $json.caseId }}/status`
- Body:

```json
{ "newStatus": "Suspendido" }
```

El caso pasa a `Suspendido` y aparece en la tarjeta "Requieren Intervencion" del Dashboard. El operador lo retoma con `Suspendido → EnCurso` desde la UI tras resolver el problema (ej. cambiar el numero, reenviar manualmente).

### 6.5 Workflow 3: Respuesta paciente (webhook)

Recibe la respuesta del paciente via el gateway SMS/WhatsApp y registra el evento. Si confirma, finaliza el caso. Si cancela, solo reporta — el operador valida antes de cerrar.

```
+-----------------------------------------------------------+
|  WF3: Respuesta paciente                                  |
|  Trigger: webhook del gateway SMS/WhatsApp               |
+-----------------------------------------------------------+
        |
        v
   [Parse respuesta (Confirmar | Cancelar)]
        |
        v
   [Lookup caseId por phone o externalId]
        |
        v
   [POST /timeline  type=Confirmacion|Cancelacion]
        |
        +-- Confirmacion --> [PATCH /status -> Finalizado]
        |
        +-- Cancelacion ----> (no mueve estado; operador valida)
```

**Paso 1 — Parse respuesta**

El body del webhook depende del gateway. Tipicamente trae `from` (phone), `body` (texto), y a veces metadata con `externalId` si se envio como parte del SMS original.

**Paso 2 — Lookup caseId**

Si el gateway devuelve `externalId` en metadata → directo desde Static Data.
Si no → lookup por phone, requiriendo traer casos activos y matchear contra `Context.patientPhone`.

**Paso 3 — POST /timeline**

n8n HTTP Request Node:
- Method: `POST`
- URL: `http://localhost:8080/api/cases/{{ $json.caseId }}/timeline`
- Headers: `X-API-Key: caimmand-poc-key`, `Content-Type: application/json`
- Body:

```json
{
  "type": "{{ $json.responseType }}",
  "origin": "n8n",
  "content": "Paciente respondio: \"{{ $json.responseText }}\""
}
```

Donde `responseType` es `"Confirmacion"` o `"Cancelacion"` segun el parseo.

**Paso 4 — Cierre del caso (solo Confirmacion)**

Si `responseType == "Confirmacion"`:

n8n HTTP Request Node:
- Method: `PATCH`
- URL: `http://localhost:8080/api/cases/{{ $json.caseId }}/status`
- Body:

```json
{ "newStatus": "Finalizado" }
```

Si `responseType == "Cancelacion"`: no se mueve estado. El operador abre el caso en Blazor, lee el evento `Cancelacion` en la timeline, valida, y hace manualmente `EnCurso → Cancelado` desde la UI.

### 6.6 Idempotencia

El endpoint `GET /api/cases` soporta el query param `externalId` que filtra por `Context.externalId` (JSONB). n8n lo usa directo: si la response es `[]` → el turno no tiene caso, se POSTea; si trae un elemento → skip. El filtro se resuelve en Caimmand, no client-side.

`externalId` es **convencion Caimmand-side** (hardcodeada en el handler). El nombre que el HIS use (`turnoId`, `appointmentId`, `codigoTurno`, etc.) es irrelevante — n8n lo mapea a `Context.externalId` al crear el caso y lo usa en el `GET` para idempotencia.

**Fallback sin id estable**: si el HIS no provee un id unico por turno (walk-ins, turnos viejos, etc.), n8n skip el `GET` y hace `POST` directo sin `externalId` en `Context`. Trade-off conscious del PoC: re-runs del WF1 pueden crear duplicados en el Dashboard. Si se quiere cerrar esa puerta, ver **backlog (Iteracion D)**.

**Detalle de implementacion (PoC)**: el filtro se aplica en memoria despues de traer los casos del `CaseDefinitionCode` dado. Funciona bien en volumen PoC (decenas de casos por definicion). Si escala:

- **Iteracion C — IMPLEMENTADA 2026-08-12 (B.4)**: el filtro `externalId` ahora se resuelve en SQL via `EF.Functions.JsonContains` con indice GIN sobre `Cases.Context` (`IX_Cases_Context_GIN`). El handler usa `IJsonQueryAdapter` (Npgsql en produccion, `InMemoryJsonQueryAdapter` en tests) para abstraer la implementacion. No hay mas filtro runtime en memoria para este campo.

- **Backlog (Iteracion D)**: hacer la **idempotency key configurable** por `CaseDefinition` (columna `IdempotencyContextKey`, default `"externalId"`) para que el handler lea el nombre de la clave de la definicion en vez de hardcodear `externalId`. Permite que `MEDICAL_AUDIT` use `auditId`, `INVOICE_FOLLOWUP` use `invoiceId`, etc. sin tocar el handler ni recompilar. Migracion EF Core + 1 test + doc update.

### 6.7 Mapeo caso ↔ externalId

Caimmand no guarda ese mapping; `Context.externalId` vive en el JSONB pero no tiene indice. El mapeo lo mantiene n8n en una de estas opciones (eleccion del integrador):

- **n8n Static Data**: simple, sin dependencias. Pierde si se resetea n8n.
- **Tabla en PostgreSQL**: una tabla dedicada `n8n_case_mapping(external_id, case_id)`. Sobrevive restarts.
- **Redis**: si ya hay Redis en el stack. Omitido del PoC.

Si se pierde el mapping, se puede reconstruir desde `GET /api/cases` filtrando client-side por `externalId` (mismo workaround de la idempotencia).

### 6.8 Convencion de tipos de evento

| Type          | Origin tipico | Generado por                              |
|---------------|---------------|-------------------------------------------|
| Creacion      | HIS           | Caimmand (auto-seed al crear el caso)     |
| Aviso         | n8n           | n8n tras envio SMS                        |
| Recordatorio  | n8n           | n8n tras reenvio (opcional)               |
| Error         | n8n           | n8n ante falla de proveedor / envio       |
| Confirmacion  | n8n           | n8n desde webhook del gateway             |
| Cancelacion   | n8n           | n8n desde webhook del gateway             |
| Llamado       | Operador      | UI (intervencion manual por telefono)     |
| Comentario    | Operador      | UI (nota manual del operador)             |

### 6.9 Reglas de oro

1. **n8n nunca usa `sourceSystem: "n8n"`**. El sistema de origen es el HIS; n8n firma `Origin` en timeline events.
2. **n8n nunca mueve a `Cancelado` directamente**. Las cancelaciones quedan a validacion del operador (oversight del PDD).
3. **n8n solo mueve a `Finalizado` tras `Confirmacion` explicita** del paciente via webhook.
4. **Toda falla reportable genera TimelineEvent `Error` + `Suspendido`**. Asi el caso cae en "Requieren Intervencion" del Dashboard.
5. **Content siempre legible y especifico**. Evitar "OK" o "fallo". Incluir proveedor, MSID, phone mascarado, texto de error crudo — el operador tiene que entender el caso en `<10s`.

---

## 7. Tasks (Iteracion B — IMPLEMENTADA 2026-08-11)

Las Tasks representan trabajo concreto asociado a un Caso. No son un motor BPM: Caimmand registra la Tarea, su estado, su asignatario y su resultado; la ejecucion real ocurre fuera de Caimmand (HIS, n8n, agente IA, operador humano).

### 7.1 POST /api/cases/{id}/tasks

Crea una Task en estado `Pendiente` asociada al Caso. Opcionalmente la asigna a un `Participant` pr-creado. Cada `Create` genera un `TimelineEvent` (Tipo `Tarea creada` o `Asignacion`) y un `AuditRecord` (`TaskCreated`).

**Request body**

```json
{
  "type": "enviar_sms",
  "assigneeId": null,
  "dueAt": "2026-08-13T10:00:00.000Z"
}
```

- `type` (string, obligatorio): tipo de accion (ej. `enviar_sms`, `confirmar_turno`, `reprogramar`).
- `assigneeId` (Guid?, opcional): id de `Participant` asignado. Si se envia, el handler valida que exista.
- `dueAt` (DateTime?, opcional): vencimiento. Cuando esta seteado y vence, la tarea se cuenta en el KPI `TasksOverdue` del Dashboard.

**Response 201 Created**

```json
{
  "id": "33333333-...",
  "caseId": "11111111-...",
  "type": "enviar_sms",
  "status": "Pendiente",
  "assigneeId": null,
  "createdAt": "2026-08-11T18:00:00.000Z"
}
```

### 7.2 GET /api/cases/{id}/tasks

Lista las Tasks del Caso. Filtros: `status` (Pendiente/EnProgreso/Completada/Cancelada), `assigneeId`.

```bash
curl -H "X-API-Key: caimmand-poc-key" "http://localhost:8080/api/cases/11111111-.../tasks?status=Pendiente"
```

### 7.3 GET /api/cases/{id}/tasks/{taskId}

Devuelve el detalle de una Task concreta (incluye `result`).

### 7.4 PATCH /api/cases/{id}/tasks/{taskId}/assign

Asigna la Task a un Participant. Requiere rol `Supervisor` o `Gerente` (policy `RequireSupervisorGerente` en `Program.cs`).

**Request body**

```json
{ "assigneeId": "44444444-..." }
```

Genera `AuditRecord(TaskAssigned)`.

### 7.5 PATCH /api/cases/{id}/tasks/{taskId}/start

Transiciona la Task de `Pendiente` → `EnProgreso`. Requiere rol `Operador`/`Supervisor`/`Api` (handler). El rol `Api` permite a n8n iniciar tareas automatizadas (envio de SMS, etc.).

Genera `AuditRecord(TaskStarted)` + TimelineEvent.

### 7.6 PATCH /api/cases/{id}/tasks/{taskId}/complete

Transiciona a `Completada` (desde `Pendiente` o `EnProgreso`), registra `Result` y `CompletedAt`. Requiere rol `Operador`/`Supervisor`/`Api`.

**Request body**

```json
{ "result": "SMS enviado correctamente al +54 9 11 ..." }
```

### 7.7 PATCH /api/cases/{id}/tasks/{taskId}/cancel

Transiciona a `Cancelada` (desde `Pendiente` o `EnProgreso`). Requiere rol `Operador`/`Supervisor` (no `Api` — el operador decide cancelar).

### 7.8 Estados y transiciones Tasks

| Estado      | Siguiente                          |
|-------------|------------------------------------|
| Pendiente   | EnProgreso / Completada / Cancelada |
| EnProgreso  | Completada / Cancelada             |
| Completada  | (terminal)                         |
| Cancelada   | (terminal)                         |

### 7.9 n8n HTTP Request Node (Tasks)

n8n tipicamente crea la Task al iniciar el workflow y la completa cuando el proveedor externo responde:

1. **Workflow 1 (HIS lee turnos)**: tras `POST /api/cases` exitoso, hacer `POST /api/cases/{id}/tasks` con `type = enviar_sms`, `assigneeId = <participant id del Agente SMS>`, `dueAt = ahora + 2h`.
2. **n8n envia el SMS via proveedor externo**. Si OK → `PATCH /api/cases/{id}/tasks/{taskId}/start` (Api) y luego `PATCH /api/cases/{id}/tasks/{taskId}/complete` con `result = "enviado a +54 9 11 ..."`. Si error → ver regla 6.4 de oro (Task `start` + `Suspendido` del caso).

> `Tasks/cancel` no esta permitido para rol `Api` — cancelar una tarea es oversight del operador.

---

## 8. Participants (Iteracion B — IMPLEMENTADA 2026-08-11)

Los Participants unifican en una sola entidad a personas externas (pacientes), usuarios internos (operadores), sistemas externos (HIS) y agentes IA.

### 8.1 POST /api/cases/{id}/participants

Crea o reusa un `Participant` (busca primero por `externalId`; si no existe, lo crea) y lo vincula al Caso con un `Rol` concreto via la entidad join `CaseParticipant`. Reutilizable a traves de multiples Casos. Genera `TimelineEvent` mantiene `Origin` snapshot string para la UI + genera `AuditRecord(ParticipantRegistered)` con `ContextRef = ExternalId`.

**Request body**

```json
{
  "type": "PersonaExterna",
  "reference": "Juan Perez",
  "externalId": "PAT-12345",
  "rol": "Paciente"
}
```

- `type` (enum, obligatorio): `PersonaExterna` / `UsuarioInterno` / `SistemaExterno` / `AgenteIA`.
- `reference` (string, obligatorio): nombre o identificador legible (se persiste como snapshot en `TimelineEvent.Origin`).
- `externalId` (string?, opcional): id externo. Si existe un `Participant` con ese `ExternalId`, se reusa en lugar de crear.
- `rol` (string, obligatorio): rol en este Caso (ej. `Paciente`, `Operador`, `SistemaDeOrigen`, `AgenteEjecutor`).

**Response 201 Created**

```json
{
  "participantId": "44444444-...",
  "caseId": "11111111-...",
  "rol": "Paciente"
}
```

### 8.2 GET /api/cases/{id}/participants

```bash
curl -H "X-API-Key: caimmand-poc-key" "http://localhost:8080/api/cases/11111111-.../participants"
```

Devuelve lista con `participantId`, `type`, `reference`, `externalId` y `rol`.

### 8.3 vinculacion en Timeline events

`POST /api/cases/{id}/timeline` ahora acepta el campo opcional `originParticipantId` (Guid). Si se pasa, el handler valida que el Participant exista y persiste `TimelineEvent.ParticipantId` ademas de `Origin` (snapshot). La UI Blazor muestra el badge de participant vinculado en el detalle del caso.

```json
{
  "type": "Aviso",
  "origin": "HIS",
  "originParticipantId": "44444444-...",
  "content": "SMS enviado."
}
```

---

## 9. Audit (Iteracion B — IMPLEMENTADA 2026-08-11)

### 9.1 GET /api/cases/{id}/audit

Devuelve los `AuditRecord` del Caso en orden descendente por `OccurredAt`. Requiere rol `Gerente` (policy `RequireGerente`).

```bash
curl -H "X-API-Key: caimmand-poc-key" "http://localhost:8080/api/cases/11111111-.../audit"
```

**Response 200 OK**

```json
[
  {
    "id": "55555555-...",
    "caseId": "11111111-...",
    "operation": "StatusChange",
    "origin": "Supervisor",
    "occurredAt": "2026-08-11T18:30:00.000Z",
    "changeJson": "{\"from\":\"EnCurso\",\"to\":\"Suspendido\"}",
    "contextRef": null
  },
  {
    "id": "66666666-...",
    "caseId": "11111111-...",
    "operation": "CaseCreation",
    "origin": "HIS",
    "occurredAt": "2026-08-11T18:00:00.000Z",
    "changeJson": "{\"caseDefinitionCode\":\"APPOINTMENT_REMINDER\",\"status\":\"Creado\",\"priority\":\"Media\"}",
    "contextRef": null
  }
]
```

### 9.2 Operaciones registradas (AuditOperation enum)

| `Operation`            | Cuando se genera                                  |
|------------------------|---------------------------------------------------|
| `CaseCreation`         | `POST /api/cases`                                 |
| `StatusChange`         | `PATCH /api/cases/{id}/status`                    |
| `EventAdded`           | `POST /api/cases/{id}/timeline`                  |
| `ParticipantRegistered`| `POST /api/cases/{id}/participants`              |
| `TaskCreated`          | `POST /api/cases/{id}/tasks`                     |
| `TaskAssigned`         | `PATCH /api/cases/{id}/tasks/{tid}/assign`       |
| `TaskStarted`          | `PATCH /api/cases/{id}/tasks/{tid}/start`        |
| `TaskCompleted`        | `PATCH /api/cases/{id}/tasks/{tid}/complete`     |
| `TaskCancelled`        | `PATCH /api/cases/{id}/tasks/{tid}/cancel`       |

El handler `UpdateCaseDefinitionHandler` no audit explicitamente en la Iteracion B (queda como backlog de Iteracion C); los cambios a Case Definitions son detectables via `CaseDefinitions` table (quedan logged en Serilog). `SetActiveCaseDefinition` igual.

---

## 10. Autorizacion por rol (n8n + UI)

> Iteracion B — IMPLEMENTADA 2026-08-11. Detalle completo en `ADR-002-Settings-Based-Auth.md`.

Para n8n, el header sigue siendo el unico requisito. Los endpoints que requieren rol especifico (Create/Update/SetActive CaseDefinition, Audit, Tasks/assign) estan disponibles, pero los que se catalogan como `Gerente` no pueden ser ejecutados desde n8n con el rol `Api` — eso es consciente: n8n no configura definiciones, ni asigna tasks, ni consulta audit. Para Tasks `start`/`complete` y Caso status `Finalizado`, n8n (rol `Api`) si esta autorizado ({to:"Finalizado"} requiere `Api` ademas de `Supervisor`/`Gerente`).

| Endpoint / operacion | Roles permitidos para n8n (`Api`)                                            |
|----------------------|--------------------------------------------------------------------------------|
| `POST /api/cases`                                 | SI     |
| `GET /api/cases[/{id}]`                           | SI     |
| `PATCH /api/cases/{id}/status` (EnCurso)          | SI     |
| `PATCH /api/cases/{id}/status` (Finalizado)        | SI     |
| `PATCH /api/cases/{id}/status` (Suspendido/Cancelado) | NO  |
| `POST /api/cases/{id}/timeline`                   | SI     |
| `POST /api/cases/{id}/tasks`                      | SI     |
| `GET /api/cases/{id}/tasks[/{tid}]`               | SI     |
| `PATCH /api/cases/{id}/tasks/{tid}/start`         | SI     |
| `PATCH /api/cases/{id}/tasks/{tid}/complete`      | SI     |
| `PATCH /api/cases/{id}/tasks/{tid}/cancel`        | NO     |
| `PATCH /api/cases/{id}/tasks/{tid}/assign`        | NO  |
| `POST /api/cases/{id}/participants`              | SI     |
| `GET /api/cases/{id}/participants`               | SI     |
| `POST /api/case-definitions`                      | NO     |
| `PATCH /api/case-definitions/{id}*`               | NO     |
| `GET /api/cases/{id}/audit`                       | NO     |

> n8n debe respetar este mapeo. Si un workflow intenta un endpoint prohibido para rol `Api`, recibira `403 Forbidden`.