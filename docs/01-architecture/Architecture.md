# Caimmand - Architecture

| Campo    | Valor                |
|----------|----------------------|
| Producto | Caimmand             |
| Version  | 0.2                  |
| Estado   | Draft                |
| Fecha    | 2026-07-13           |
| Autor    | CAI Process Grid Team |

> Caimmand no ejecuta el negocio; hace visible, gobernable y operable su ejecucion.

## Tabla de contenidos

1. [Objetivo](#objetivo)
2. [Vision General](#vision-general)
3. [Principios Arquitectonicos](#principios-arquitectonicos)
4. [Responsabilidades del Producto](#responsabilidades-del-producto)
5. [Modelo Conceptual](#modelo-conceptual)
6. [Ciclo de Vida del Caso](#ciclo-de-vida-del-caso)
7. [Command API](#command-api)
8. [Integraciones](#integraciones)
9. [Limites del Sistema](#limites-del-sistema)
10. [Evolucion prevista](#evolucion-prevista)
11. [Estado del documento](#estado-del-documento)

## Objetivo

Este documento describe la arquitectura funcional del producto Caimmand correspondiente al MVP. Su proposito es conectar la definicion oficial del producto, contenida en el Product Definition Document (PDD), con el futuro desarrollo, estableciendo la organizacion del dominio, las responsabilidades del sistema y los limites que lo separan de los sistemas externos.

El documento es exclusivamente funcional. No define tecnologias de implementacion, infraestructura, mecanismos de persistencia, frameworks ni componentes tecnicos concretos. Su alcance es describir que hace Caimmand, que informacion administra y como se organiza, no como sera construido.

Este documento acompana la evolucion del producto durante varios anios y opera como referencia fundacional para todas las decisiones arquitectonicas posteriores.

### Relacion con el PDD

El PDD define el producto: su vision, el problema que resuelve, su mision, sus principios y su alcance. Este documento amplifica la dimension arquitectonica de esas definiciones, traduciendo las decisiones de producto en responsabilidades, modelos y fronteras de integracion.

Toda decision arquitectonica debe respetar el PDD. En caso de conflicto, el PDD prevalece y este documento debe corregirse.

### Alcance del documento

| Dimension            | Dentro del alcance                   | Fuera del alcance                          |
|----------------------|--------------------------------------|--------------------------------------------|
| Nivel                | Arquitectura funcional               | Arquitectura tecnica                       |
| Horizonte            | MVP                                  | Evolucion futura detallada                 |
| Tecnologia           | Independiente de tecnologia          | Referencias a stacks o frameworks          |
| Componentes futuros  | No abordados                         | Definicion de componentes Cai*             |
| Detalle de comandos  | Catalogo base propuesto              | Especificacion detallada de cada comando   |

## Vision General

Caimmand es una plataforma de operaciones y gobierno centrada en el Caso. Su rol es centralizar la operacion, la trazabilidad, la observabilidad y la auditoria del trabajo ejecutado por automatizaciones, agentes de IA y personas.

Caimmand no ejecuta el negocio. No disena procesos. No ejecuta workflows. No construye automatizaciones. Su valor esta en hacer visible, gobernable y operable la ejecucion de casos creados por sistemas externos.

### Diagrama de contexto

```
                +-----------------------------+
                |       Sistemas de Origen     |
                |  (crean Casos, no operan)    |
                +--------------+--------------+
                               |
                               | Creacion de Caso
                               | (referencia Case Definition)
                               v
+------------------+   Command API   +-----------------------------------+
|  Automatizaciones| <-------------> |             Caimmand              |
|  (ej. n8n)        |   registros     |    opera el Caso, no lo ejecuta   |
|  son clientes     |   consultas     |                                   |
+------------------+                 |  +-------------+                  |
                                     |  | Case Def.   |                  |
                                     |  +-------------+                  |
                                     |  +-------+  +--------+  +-------+ |
                                     |  | Caso  |  | Tarea  |  |Evento | |
                                     |  +-------+  +--------+  +-------+ |
                                     |             Timeline   Participante|
                                     +----------------+------------------+
                                                      ^
                                                      |
                                            +---------+---------+
                                            |  Usuarios Caimmand |
                                            |  Operador          |
                                            |  Supervisor        |
                                            |  Gerente           |
                                            +-------------------+
```

### Lectura del diagrama

1. Los Sistemas de Origen crean Casos via Command API, referenciando una Case Definition registrada en Caimmand. No operan dentro de Caimmand.
2. Las Automatizaciones (como n8n) son clientes de Caimmand: registran tareas, eventos y consultas via Command API. No son duenas del modelo de datos ni conocen la estructura interna de persistencia.
3. Caimmand administra la Case Definition que tipifica los Casos, y el Caso junto con sus entidades asociadas: Tareas, Eventos, Timeline y Participantes. Tambien administra la trazabilidad, las reglas de gobierno y el estado de cada Caso.
4. Los usuarios (Operador, Supervisor, Gerente) interactuan con Caimmand mediante una interfaz de operacion. El Gerente ademas administra las Case Definitions. Ningun usuario accede directamente a los datos.

### Frontera del sistema

La frontera funcional de Caimmand esta definida por tres elementos:

- La Command API, unico punto de entrada autorizado para sistemas externos y automatizaciones.
- El modelo del Caso, unico dominio administrado internamente por el producto.
- La trazabilidad, propiedad intrinseca del sistema que acompana toda modificacion relevante.

Todo lo que queda fuera de estos tres elementos es responsabilidad de sistemas externos.

## Principios Arquitectonicos

Los siguientes principios son obligatorios y cualquier decision tecnica o funcional que los contradiga debe ser explicitamente justificada y documentada.

### Principio 1. Los Casos siempre son creados por sistemas externos

Caimmand nunca crea Casos. La creacion del Caso es responsabilidad del Sistema de Origen, que invoca la Command API para registrar el Caso en Caimmand asociandolo a una Case Definition previamente registrada.

Implicacion: el producto no posee ninguna funcionalidad de creacion nativa de Casos. Toda solicitud de creacion proviene del exterior y referencia una Case Definition existente y activa.

### Principio 2. El Caso es la entidad operativa central; la Case Definition es la entidad que tipifica la operacion

Todo el modelo de datos, toda la trazabilidad y toda la operacion giran alrededor del Caso. La Case Definition define que tipo de operacion se esta gobernando; el Caso es la instancia concreta que se opera. Ambas son entidades de primer nivel dentro de Caimmand.

Implicacion: el Caso responde a la pregunta "que instancia concreta estamos operando?". La Case Definition responde a "que tipo de operacion estamos gobernando?". Tareas, Eventos, Timeline y Participantes existen en relacion a un Caso. No existe operacion descontextualizada de un Caso.

### Principio 3. Caimmand opera los Casos, no ejecuta el negocio

Caimmand no ejecuta procesos de negocio. No ejecuta workflows. No ejecuta automatizaciones. Su rol es registrar, supervisar, gobernar y auditar el trabajo que otros realizan sobre los Casos.

Implicacion: el flujo de ejecucion real vive fuera de Caimmand; el flujo operativo y de gobierno vive dentro de Caimmand.

### Principio 4. Toda interaccion externa se realiza mediante la Command API

No se permite acceso directo a la base de datos. Ningun sistema externo, automatizacion ni usuario accede directamente al modelo de persistencia. Todas las operaciones se canalizan a traves de la Command API.

Implicacion: la Command API es el unico contrato publico del producto, y su estabilidad es un activo critico de la arquitectura.

### Principio 5. Las automatizaciones son clientes de Caimmand

Las automatizaciones (por ejemplo, n8n) son clientes del producto. No son duenas del modelo de datos. No conocen la estructura interna de persistencia. Interactuan con Caimmand mediante la Command API, como cualquier otro cliente externo.

Implicacion: el producto esta desacoplado de cualquier herramienta de automatizacion especifica. Reemplazar n8n por otra herramienta no debe alterar el modelo de datos ni las reglas de negocio de Caimmand.

### Principio 6. Las reglas de negocio pertenecen a Caimmand

Las reglas de negocio nunca deben implementarse dentro de los workflows. Los workflows ejecutan; Caimmand gobierna. Toda regla que determine validez, transicion de estado, asignacion, priorizacion o gobierno pertenece al producto.

Implicacion: los workflows deben mantenerse libres de logica de negocio. Caimmand valida, autoriza y registra; el workflow cumple lo que Caimmand determina.

### Principio 7. Toda modificacion relevante genera trazabilidad

El historial operativo es parte esencial del producto. Toda modificacion relevante sobre un Caso, una Tarea o un Evento genera un registro de trazabilidad que alimenta la Timeline.

Implicacion: no existe modificacion silenciosa. Toda accion deja rastro, y la Timeline es la fuente unica para reconstruir el ciclo de ejecucion de un Caso.

## Responsabilidades del Producto

Caimmand concentra un conjunto acotado de responsabilidades funcionales. La claridad sobre lo que el producto administra, y lo que no administra, es la base de su arquitectura.

### Que administra Caimmand

| Responsabilidad              | Descripcion                                                       |
|------------------------------|-------------------------------------------------------------------|
| Case Definition              | Entidad que tipifica la operacion. Define el tipo de Caso, sus valores por defecto y su presentacion. |
| Caso                         | Entidad operativa central. Almacena su identidad, origen, estado y contexto. |
| Tarea                        | Unidad de trabajo asociada a un Caso. Administra su estado, asignacion y resultado. |
| Evento                       | Registro de cambios y acontecimientos relevantes del Caso.        |
| Timeline                     | Secuencia cronologica de tareas y eventos de un Caso.             |
| Participante                 | Actor que interviene en un Caso (persona, automatizacion o agente IA). |
| Estado del Caso             | Maquina de estados funcional del Caso, con transiciones gobernadas. |
| Reglas de gobierno          | Reglas de negocio que rigen validez, transiciones, asignaciones y restricciones. |
| Trazabilidad                 | Historial operativo inmutable de toda modificacion relevante.      |
| Observabilidad              | Vista agregada del estado de Casos para supervisores y gerentes. |
| Auditoria                   | Capacidad de reconstruir el ciclo completo de un Caso.            |
| Identidad de Participantes   | Registro de quienes intervienen en cada Caso y en que rol.       |

### Que NO administra Caimmand

| Dimension                         | Responsabilidad externa                                |
|-----------------------------------|--------------------------------------------------------|
| Diseno de procesos BPM            | Herramientas externas de BPMN / modelado de procesos. |
| Diseno de workflows               | Herramientas externas de orquestacion de workflows.   |
| Creacion de agentes de IA        | Plataformas externas de agentes.                       |
| Prompt engineering               | Plataformas externas de IA.                            |
| Ejecucion de automatizaciones    | Herramientas externas como n8n.                       |
| Ejecucion de procesos de negocio | Sistemas transaccionales externos.                    |
| Creacion de Casos                 | Sistemas de Origen.                                    |

### Tabla resumen de frontera

| Pregunta                              | Respuesta                                            |
|---------------------------------------|------------------------------------------------------|
| Quien crea el Caso?                   | El Sistema de Origen, via Command API.              |
| Quien ejecuta el trabajo?             | Automatizaciones, agentes IA y personas.             |
| Quien gobierna las reglas?            | Caimmand.                                            |
| Quien registra la trazabilidad?       | Caimmand, sobre toda modificacion relevante.         |
| Quien conoce el modelo de datos?      | Caimmand. Los clientes solo ven la Command API.    |

## Modelo Conceptual

El modelo conceptual describe las entidades del dominio funcional de Caimmand y sus relaciones. No define estructuras de persistencia ni esquemas de base de datos; define conceptos y responsabilidades.

### Diagrama de entidades

```
+--------------------+
|  Categoria (opc.)  |
+---------+----------+
          |
          | agrupa
          v
+--------------------+
|  Case Definition   |
|  (que tipo de      |
|   operacion se     |
|   gobierna)        |
+---------+----------+
          |
          | tipifica
          v
+--------------------+        +-------------------+
|  Sistema de Origen |        |    Participante   |
+---------+----------+        +-------------------+
          |                          ^
          | crea                     | interviene
          v                          |
+---------+----------+               |
|       Caso         |<--------------+
|  (instancia concreta|
|   que se opera)    |
+---------+----------+
          |
          | tiene
          +------------------------+
          |                        |
          v                        v
+---------+---------+    +---------+---------+
|       Tarea       |    |      Evento       |
+---------+---------+    +---------+---------+
          |                        |
          +------------+-----------+
                       |
                       v
              +---------+---------+
              |     Timeline      |
              | (cronologia del   |
              |  Caso: tareas +   |
              |     eventos)      |
              +-------------------+
```

### Entidades del dominio

#### Case Definition

Entidad que tipifica la operacion. Define que tipo de operacion se esta gobernando. Cada Caso se asocia a una unica Case Definition al ser creado. Las Case Definitions son administradas dentro de Caimmand, tipicamente por el rol Gerente, y permiten agregar nuevos tipos de operacion sin modificar el nucleo del producto.

| Atributo funcional    | Descripcion                                                |
|-----------------------|-------------------------------------------------------------|
| Codigo                | Identificador funcional estable de la definicion (ej. APPOINTMENT_REMINDER, MEDICAL_AUDIT, CLAIM). |
| Nombre                 | Nombre legible de la definicion (ej. "Recordatorio de Turno", "Auditoria Medica"). |
| Descripcion          | Texto que describe el proposito de la operacion.           |
| Categoria             | Agrupacion opcional de la definicion (opcional en el MVP). |
| Estado                | Activa o inactiva. Solo las activas pueden referenciarse al crear Casos. |
| SLA por defecto       | Tiempo maximo objetivo de resolucion del Caso, si aplica.  |
| Prioridad por defecto  | Prioridad asignada por defecto a los Casos de esta definicion. |
| Presentacion          | Color o icono para la interfaz de operacion.               |

Una Case Definition responde a la pregunta "que tipo de operacion estamos gobernando?". Su proposito es agrupar y tipificar Casos que comparten un mismo tipo de operacion, aportando valores por defecto y una presentacion consistente.

#### Caso

Entidad operativa central del producto. Representa la instancia de trabajo concreta que se supervisa, controla y gobierna a lo largo de su ciclo de ejecucion. Cada Caso pertenece a una unica Case Definition.

| Atributo funcional    | Descripcion                                                |
|-----------------------|-------------------------------------------------------------|
| Identidad             | Identificador unico del Caso dentro de Caimmand.            |
| Case Definition       | Definicion que tipifica este Caso (ej. Recordatorio de Turno). |
| Sistema de Origen     | Sistema externo que lo creo.                                |
| Estado actual          | Estado funcional dentro del ciclo de vida del Caso.        |
| Contexto              | Informacion relevante del Caso necesaria para su operacion. |
| Participantes         | Conjunto de actores que intervienen en el Caso.            |
| Timeline              | Cronologia de tareas y eventos asociados al Caso.          |
| Fechas relevantes      | Creacion, ultima modificacion, cierre.                     |

Un Caso responde a la pregunta "que instancia concreta estamos operando?".

#### Sistema de Origen

Sistema externo responsable de la creacion del Caso. Caimmand no crea casos; los recibe y opera.

| Atributo funcional    | Descripcion                                                |
|-----------------------|-------------------------------------------------------------|
| Identidad             | Identificador del sistema externo.                         |
| Tipo                  | Tipo o categoria del sistema de origen.                    |
| Configuracion         | Parametros de integracion definidos en Caimmand.           |

#### Tarea

Unidad de trabajo concreta asociada a un Caso, ejecutada por una automatizacion, un agente de IA o una persona.

| Atributo funcional    | Descripcion                                                |
|-----------------------|-------------------------------------------------------------|
| Identidad             | Identificador unico de la Tarea dentro del Caso.           |
| Caso                 | Caso al que pertenece.                                      |
| Tipo                  | Tipo de trabajo a realizar.                                |
| Participante asignado | Persona, automatizacion o agente responsable de ejecutarla. |
| Estado                | Estado funcional de la Tarea.                              |
| Resultado             | Informacion producida al ejecutarla.                       |
| Fechas relevantes     | Creacion, asignacion, cierre.                              |

#### Evento

Registro de un cambio o acontecimiento relevante dentro del ciclo de vida de un Caso.

| Atributo funcional    | Descripcion                                                |
|-----------------------|-------------------------------------------------------------|
| Identidad             | Identificador unico del Evento.                            |
| Caso                 | Caso al que pertenece.                                      |
| Tipo                  | Tipo de acontecimiento registrado.                         |
| Origen                | Participante o sistema que genero el Evento.              |
| Momento               | Marca temporal de ocurrencia.                              |
| Contenido             | Informacion relevante asociada al Evento.                  |

#### Timeline

Secuencia cronologica de eventos y tareas de un Caso, que permite reconstruir su historia y comprender su estado.

| Atributo funcional    | Descripcion                                                |
|-----------------------|-------------------------------------------------------------|
| Caso                 | Caso al que pertenece.                                      |
| Entradas              | Lista ordenada de Tareas y Eventos en orden cronologico.   |

#### Participante

Actor que interviene en un Caso, sea una persona, una automatizacion o un agente de IA.

| Atributo funcional    | Descripcion                                                |
|-----------------------|-------------------------------------------------------------|
| Identidad             | Identificador del Participante.                            |
| Tipo                  | Persona, automatizacion o agente IA.                       |
| Rol                   | Funcion que cumple dentro del Caso.                        |

### Reglas de relacion

- Cada Caso referencia una unica Case Definition al ser creado.
- Una Case Definition puede agrupar multiples Casos a lo largo del tiempo.
- Una Case Definition puede pertenecer opcionalmente a una Categoria.
- Solo las Case Definitions activas pueden referenciarse al crear Casos.
- Las Case Definitions son administradas dentro de Caimmand, no creadas por sistemas externos.
- Cada Caso tiene un unico Sistema de Origen.
- Cada Tarea pertenece a un unico Caso.
- Cada Evento pertenece a un unico Caso.
- La Timeline es un agregado cronologico de las Tareas y Eventos de un Caso.
- Un Participante puede intervenir en multiples Casos.
- Un Caso puede tener multiples Participantes en distintos roles.

### Distincion conceptual: tipo vs instancia

La separacion entre Case Definition y Caso es un pilar del modelo. Permite incorporar nuevos tipos de operacion registrando una nueva Case Definition, sin modificar el nucleo de Caimmand: Casos, Tareas, Eventos, Timeline, Command API y reglas de gobierno siguen funcionando exactamente igual.

| Concepto          | Pregunta                                                   | Ejemplo                                                          |
|-------------------|-------------------------------------------------------------|-------------------------------------------------------------------|
| Case Definition   | Que tipo de operacion estamos gobernando?                  | Recordatorio de Turno.                                            |
| Caso              | Que instancia concreta estamos operando?                   | Caso #2026-000154: Recordatorio del turno de Juan Perez del 18/07 a las 10:30. |

## Ciclo de Vida del Caso

El ciclo de vida del Caso describe los estados por los que un Caso puede transitar durante su operacion en Caimmand. Esta seccion propone un conjunto base de estados y transiciones para el MVP, sujeto a validacion por el equipo de producto.

### Propuesta de estados

| Estado     | Descripcion                                                          |
|------------|---------------------------------------------------------------------|
| Creado      | El Caso fue registrado en Caimmand por un Sistema de Origen.        |
| En curso    | El Caso esta siendo operado: existen tareas activas o en ejecucion. |
| Suspendido   | El Caso fue pausado manualmente o por una regla de gobierno.        |
| Finalizado  | El Caso completo su ciclo de ejecucion con exito.                    |
| Cancelado    | El Caso fue interrumpido sin completar su ciclo.                    |

### Diagrama de transiciones

```
       +----------+
       |  Creado  |
       +-----+----+
             |
             | iniciar operacion
             v
       +-----+----+
   +-->| En curso |
   |   +-----+----+
   |         |
   | suspender|   finalizar
   |         |   cancelar
   |         v
   |   +-----+-----+
   +---| Suspendido |
   |   +-----+-----+
   |         |
   | reanudar|
   +---------+
             |
             | finalizar
             v
       +-----+-------+       +-----------+
       | Finalizado  |       | Cancelado |
       +-------------+       +-----------+
```

### Transiciones validas

| Estado origen | Estado destino | Disparador                                |
|---------------|----------------|--------------------------------------------|
| Creado         | En curso        | Inicio de operacion (primera tarea/evento). |
| En curso       | Suspendido      | Suspension manual o regla de gobierno.     |
| Suspendido     | En curso        | Reactivacion manual o regla de gobierno.   |
| En curso       | Finalizado      | Cumplimiento del ciclo del Caso.           |
| En curso       | Cancelado       | Cancelacion del Caso.                      |
| Suspendido     | Cancelado       | Cancelacion del Caso.                      |

### Notas

- No existen transiciones desde los estados terminales `Finalizado` ni `Cancelado`.
- Toda transicion genera un Evento en la Timeline del Caso.
- Las transiciones estan sujetas a las reglas de gobierno definidas en Caimmand, no a la logica de los workflows externos.
- Este conjunto base es una propuesta para el MVP. Estados adicionales (por ejemplo, `En revision`, `Escalado`) podran incorporarse en futuras iteraciones.

## Command API

La Command API es el unico punto de entrada autorizado para interactuar con Caimmand. Todo sistema externo, automatizacion y usuario opera exclusivamente a traves de ella. No se permite acceso directo a la base de datos.

### Principios de la Command API

| Principio                         | Descripcion                                                  |
|-----------------------------------|-------------------------------------------------------------|
| Unico punto de entrada            | No existe ningun otro canal autorizado para modificar el estado del Caso. |
| Contratos estables               | Los comandos son contratos publicos del producto y su estabilidad es un activo critico. |
| Desacoplamiento de persistencia   | Ningun cliente conoce la estructura interna de persistencia. |
| Automatizaciones como clientes    | Las automatizaciones (ej. n8n) usan la Command API como cualquier otro cliente. |
| Reglas de negocio en Caimmand    | Toda validacion, autorizacion o restriccion se evalua dentro de Caimmand. |
| Trazabilidad inherente           | Toda ejecucion de comando relevante genera un Evento en la Timeline. |

### Catalogo base propuesto

El siguiente catalogo es una propuesta base para el MVP. Define los tipos de operaciones que la Command API debe soportar, sin entrar en detalle de especificacion de cada comando.

#### Comandos sobre Casos

| Comando                  | Descripcion                                          | Origen permitido              |
|--------------------------|-----------------------------------------------------|-------------------------------|
| Registrar Caso           | Recibe un Caso desde un Sistema de Origen, asociandolo a una Case Definition activa. | Sistema de Origen |
| Cambiar estado del Caso  | Transita el estado del Caso.                        | Caimmand / Supervisor         |
| Suspender Caso           | Pausa la operacion del Caso.                        | Supervisor                    |
| Reactivar Caso           | Reanuda la operacion de un Caso suspendido.        | Supervisor                    |
| Cancelar Caso            | Interrumpe el Caso sin completar su ciclo.         | Supervisor / Gerente          |
| Finalizar Caso           | Marca el Caso como completado.                     | Supervisor / Sistema          |
| Consultar Caso           | Devuelve el estado y contexto de un Caso.         | Operador / Supervisor / Gerente / Automatizacion |

#### Comandos sobre Tareas

| Comando                  | Descripcion                                          | Origen permitido              |
|--------------------------|-----------------------------------------------------|-------------------------------|
| Registrar Tarea          | Crea una Tarea asociada a un Caso.                  | Sistema de Origen / Automatizacion |
| Asignar Tarea            | Asigna una Tarea a un Participante.                 | Caimmand / Supervisor         |
| Iniciar Tarea             | Marca una Tarea como en ejecucion.                  | Operador / Automatizacion     |
| Completar Tarea          | Marca una Tarea como finalizada, registra resultado. | Operador / Automatizacion     |
| Cancelar Tarea           | Cancela una Tarea pendiente.                        | Supervisor / Operador         |
| Consultar Tareas          | Devuelve las Tareas de un Caso, filtradas por estado. | Operador / Supervisor / Gerente / Automatizacion |

#### Comandos sobre Eventos

| Comando                  | Descripcion                                          | Origen permitido              |
|--------------------------|-----------------------------------------------------|-------------------------------|
| Registrar Evento         | Anade un Evento a la Timeline de un Caso.          | Sistema de Origen / Automatizacion / Operador / Supervisor |
| Consultar Timeline        | Devuelve la cronologia de un Caso.                  | Operador / Supervisor / Gerente / Automatizacion |

#### Comandos de Participantes

| Comando                  | Descripcion                                          | Origen permitido              |
|--------------------------|-----------------------------------------------------|-------------------------------|
| Registrar Participante    | Anade un Participante a un Caso.                    | Caimmand / Sistema de Origen  |
| Consultar Participantes   | Devuelve los Participantes de un Caso.              | Operador / Supervisor / Gerente |

#### Comandos sobre Case Definitions

| Comando                  | Descripcion                                          | Origen permitido              |
|--------------------------|-----------------------------------------------------|-------------------------------|
| Registrar Case Definition | Crea una nueva definicion de tipo de operacion.     | Gerente                       |
| Actualizar Case Definition | Modifica los atributos de una definicion existente. | Gerente                       |
| Activar Case Definition  | Marca una definicion como activa para crear Casos. | Gerente                       |
| Desactivar Case Definition | Marca una definicion como inactiva.                  | Gerente                       |
| Consultar Case Definitions | Devuelve las definiciones registradas, filtrables por estado o categoria. | Operador / Supervisor / Gerente / Automatizacion |

#### Comandos de intervencion

| Comando                  | Descripcion                                          | Origen permitido              |
|--------------------------|-----------------------------------------------------|-------------------------------|
| Intervenir en Caso       | Registra una accion manual sobre un Caso.          | Supervisor / Gerente          |
| Escalar Caso              | Eleva la prioridad o visibilidad de un Caso.       | Supervisor / Gerente          |

### Notas

- Toda ejecucion de un comando de modificacion genera trazabilidad en la Timeline del Caso correspondiente.
- Los comandos de consulta no modifican estado; solo devuelven informacion.
- Las reglas de negocio que validan cada comando viven dentro de Caimmand, no en los workflows externos.
- Este catalogo es una propuesta base para el MVP. Comandos adicionales se incorporaran a medida que se consoliden nuevos requerimientos funcionales.

## Integraciones

Caimmand se integra con tres tipos de actores externos:

1. Sistemas de Origen
2. Automatizaciones
3. Usuarios

### Diagrama de integraciones

```
+---------------------+        +----------------------------------+
|  Sistema de Origen  |------->|            Caimmand              |
|  - registra Caso    |        |  Command API                     |
|  - registra eventos |        |  - valida                        |
+---------------------+        |  - aplica reglas                 |
                               |  - genera trazabilidad           |
+---------------------+        |  - actualiza estado              |
|  Automatizacion     |<------>|                                  |
|  (ej. n8n)          |        |  Command API                     |
|  - consulta Caso    |        |  - devuelve estado               |
|  - registra Tarea   |        |  - acepta registros              |
|  - registra Evento  |        +----------------------------------+
|  - completa Tarea    |                   ^
+---------------------+                   |
                                          | interactua
                                          v
                               +-------------------+
                               |  Usuarios         |
                               |  Operador         |
                               |  Supervisor      |
                               |  Gerente         |
                               +-------------------+
```

### 1. Sistemas de Origen

Los Sistemas de Origen son los responsables de la creacion de Casos. No operan dentro de Caimmand: su unico rol es invocar la Command API para registrar un Caso y, opcionalmente, registrar eventos relacionados con su creacion.

| Aspecto                | Decision                                                |
|------------------------|--------------------------------------------------------|
| Crean Casos?            | Si, via Command API.                                  |
| Operan Tareas?         | No.                                                    |
| Conocen el modelo interno? | No. Ven exclusivamente el contrato de la Command API. |
| Pueden modificar Casos ya creados? | Solo mediante comandos autorizados, si las reglas lo permiten. |

### 2. Automatizaciones

Las automatizaciones (por ejemplo, n8n) son clientes de Caimmand. No son duenas del modelo de datos. No conocen la estructura interna de persistencia. Ejecutan trabajo sobre los Casos y registran el avance en Caimmand mediante la Command API.

| Aspecto                | Decision                                                |
|------------------------|--------------------------------------------------------|
| Disenan workflows?     | No. Eso es responsabilidad de la herramienta externa. |
| Ejecutan trabajo?      | Si, sobre Tareas y Eventos asociados a Casos.          |
| Conocen el modelo interno? | No. Ven exclusivamente el contrato de la Command API. |
| Definen reglas de negocio? | No. Las reglas viven en Caimmand.                    |
| Generan trazabilidad?  | Toda modificacion relevante genera trazabilidad en Caimmand. |

Interaccion tipica de una automatizacion:

1. Consulta el estado de un Caso mediante la Command API.
2. Registra una Tarea asociada al Caso.
3. Inicia la Tarea.
4. Ejecuta el trabajo correspondiente fuera de Caimmand.
5. Completa la Tarea mediante la Command API, registrando el resultado.
6. Caimmand genera automaticamente los Eventos de trazabilidad correspondientes.

### 3. Usuarios

Los usuarios de Caimmand interactuan con el producto mediante una interfaz de operacion. No acceden directamente a la base de datos ni a la Command API de forma cruda; la interfaz encapsula los comandos segun el rol del usuario.

| Rol         | Tipo de interaccion principal                                              |
|-------------|---------------------------------------------------------------------------|
| Operador    | Atender Tareas pendientes, registrar Eventos, mantener el flujo del Caso. |
| Supervisor  | Supervisar estado de Casos, intervenir, suspender, reactivar, escalar.   |
| Gerente     | Gobernar la operacion global, administrar Case Definitions, auditar, analizar tendencias. |

### Reglas comunes a todas las integraciones

- Toda interaccion se canaliza a traves de la Command API, directa o indirectamente.
- Toda modificacion relevante genera trazabilidad en la Timeline del Caso.
- Las reglas de negocio se evaluan dentro de Caimmand, sin importar el origen del comando.
- Ningun cliente, sea sistema, automatizacion o usuario, conoce la estructura interna de persistencia.

## Limites del Sistema

Esta seccion consolida explicitamente lo que Caimmand NO hace. Los limites del sistema son tan importantes como sus responsabilidades: definen el contorno preciso del producto y evitan la deriva de alcance.

### Limites funcionales

| Limite                                   | Razon                                                 | Responsabilidad alternativa            |
|------------------------------------------|-------------------------------------------------------|----------------------------------------|
| No crea Casos                            | Los Casos provienen del negocio, no del gobierno.    | Sistemas de Origen                     |
| No disena procesos BPM                   | El diseno de procesos pertenece a herramientas BPM.  | Herramientas externas de modelado      |
| No disena workflows                      | Los workflows son un mecanismo de ejecucion.         | Herramientas externas de orquestacion  |
| No crea agentes de IA                    | Los agentes viven en plataformas externas.           | Plataformas externas                   |
| No realiza prompt engineering            | Esa responsabilidad es de las plataformas de IA.    | Plataformas externas                   |
| No ejecuta automatizaciones              | Caimmand opera, no ejecuta.                          | Herramientas externas como n8n         |
| No ejecuta procesos de negocio           | El negocio se ejecuta en sistemas transaccionales.  | Sistemas transaccionales externos      |
| No modela BPMN                          | El modelado es una actividad de diseno.              | Herramientas externas de modelado      |
| No permite acceso directo a la base de datos | La Command API es el unico canal autorizado.    | Ninguna: es una restriccion absoluta   |
| No delega reglas de negocio a workflows  | Las reglas son activo del producto.                  | Ninguna: es una restriccion absoluta   |

### Antipatrones a evitar

- Implementar reglas de negocio dentro de workflows externos.
- Permitir acceso directo a la base de datos para eludir la Command API.
- Tratar a las automatizaciones como duenas del modelo de datos.
- Crear Casos desde dentro de Caimmand.
- Modificar el estado de un Caso sin generar trazabilidad.
- Introducir dependencias ontologicas con una herramienta de automatizacion especifica.

## Evolucion prevista

Este documento describe exclusivamente la arquitectura funcional del MVP. La evolucion futura del producto incorporara nuevas responsabilidades y capacidades, pero esas decisiones se tomaran en su momento y se documentaran mediante actualizaciones de este documento y, cuando corresponda, mediante Architecture Decision Records (ADR) en la carpeta `ADR/`.

### Lineas de evolucion funcionales

| Dimension              | Evolucion posible                                         |
|------------------------|-----------------------------------------------------------|
| Estados del Caso      | Nuevos estados y subestados (revision, escalado, bloqueo). |
| Tipos de Participante | Nuevos tipos de participantes y roles.                     |
| Reglas de gobierno    | Reglas mas sofisticadas, parametrizables por organizacion. |
| Observabilidad        | Vistas agregadas, metricas, tableros de supervision.      |
| Auditoria              | Capacidad de reconstruccion avanzada, exportes, reportes. |
| Catalogo de comandos  | Nuevos comandos y operaciones.                            |
| Categorias             | Categorias como entidad de primer nivel (en el MVP son un atributo opcional de Case Definition). |
| Case Definitions       | Atributos avanzados, plantillas, reglas por definicion.   |

### Lineas de evolucion estructurales

| Dimension              | Evolucion posible                                         |
|------------------------|-----------------------------------------------------------|
| Componentes internos   | Posible especializacion de responsabilidades internas.    |
| Modos de integracion   | Nuevos modos de integracion con sistemas externos.        |
| Extension del dominio  | Nuevas entidades del dominio funcional.                    |

### Reserva de decision

Cualquier decision sobre componentes internos, especializacion o modos de integracion queda reservada para futuras iteraciones. Este documento no anticipa ni compromete ninguna arquitectura futura especifica.

## Estado del documento

Este documento se encuentra en estado **Draft**, version **0.2**.

El contenido refleja las decisiones tomadas hasta la fecha por el equipo de producto y las decisiones arquitectonicas obligatorias establecidas para el MVP. La version 0.2 incorpora la entidad Case Definition como entidad principal del dominio, tipificando los Casos y permitiendo incorporar nuevos tipos de operacion sin modificar el nucleo del producto. Las propuestas de ciclo de vida del Caso y el catalogo base de la Command API estan sujetas a validacion por el equipo de producto.

Pendiente de revision por el equipo CAI Process Grid.