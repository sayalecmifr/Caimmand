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
- Auth: en el PoC la API es abierta. En futuras fases se incorporara Keycloak (Out of Scope del PoC).
- Fechas: UTC en todas las responses (`DateTime.UtcNow`).

> Todos los ejemplos `curl` y n8n de este documento asumen Docker Compose (`http://localhost:8080`). Si corres local, sustitui el host/puerto por `https://localhost:5001`.

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
- Headers: `Content-Type: application/json`
- Body (JSON): el objeto de arriba. Tipicamente se mapea desde el output del nodo que leyo del HIS.

---

### 1.2 GET /api/cases

Lista casos filtrando por estado, `CaseDefinitionCode` y/o `externalId` (idempotencia por turno).

**Query params**

- `status` (opcional): uno de `Creado`, `EnCurso`, `Suspendido`, `Finalizado`, `Cancelado` (case-insensitive).
- `caseDefinitionCode` (opcional): el codigo exacto de la definicion.
- `externalId` (opcional): filtra por `Context.externalId` (lookup en JSONB en memoria, ver 6.6). Identifica la unidad de trabajo dentro del `sourceSystem` (ej. id del turno dentro del HIS). No confundir con `sourceSystem` mismo.

**curl**

```bash
curl "http://localhost:8080/api/cases?status=Suspendido&caseDefinitionCode=APPOINTMENT_REMINDER"
```

Lookup por `externalId` (tipico para idempotencia en n8n):

```bash
curl "http://localhost:8080/api/cases?caseDefinitionCode=APPOINTMENT_REMINDER&externalId=APT-2026-0718-001"
```

Si response es `[]` → no existe caso para ese turno, se puede crear. Si trae un elemento → skip.

**Response 200 OK**

```json
[
  {
    "id": "52abb42f-...",
    "title": "Recordatorio del turno de Juan Perez",
    "caseDefinitionCode": "APPOINTMENT_REMINDER",
    "caseDefinitionName": "Recordatorio de Turno",
    "status": "Suspendido",
    "sourceSystem": "HIS",
    "createdAt": "2026-07-21T14:30:00.000Z"
  }
]
```

---

### 1.3 GET /api/cases/{id}

Detalle de un caso. Devuelve `404` si no existe.

**curl**

```bash
curl http://localhost:8080/api/cases/52abb42f-1234-5678-9abc-def012345678
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
curl http://localhost:8080/api/cases/52abb42f-.../timeline
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
curl http://localhost:8080/api/case-definitions
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

> Nota: en la iteracion actual las "tareas" (`Enviar SMS`, `Esperar confirmacion`) se modelan como eventos de timeline libres. La **Iteracion B** (Task + Participant entities) las convertira en entidades estructuradas con su propio estado, desbloqueando KPIs de "tareas vencidas" en el Dashboard.

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
- Headers: `Content-Type: application/json`

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
- Headers: `Content-Type: application/json`
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
- Headers: `Content-Type: application/json`
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
- Headers: `Content-Type: application/json`
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

- **Backlog (Iteracion C)**: empujar el filtro a SQL con un indice GIN sobre `Context` y `EF.Functions.JsonContains` (Npgsql). Hasta entonces, el costo es traer los N casos del `CaseDefinitionCode` + filtrar en runtime — aceptable.

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