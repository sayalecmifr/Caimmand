# Caimmand - UX Guidelines

| Campo    | Valor                |
|----------|----------------------|
| Producto | Caimmand             |
| Version  | 0.1                  |
| Estado   | Draft                |
| Fecha    | 2026-07-15           |
| Autor    | CAI Process Grid Team |

> Caimmand no ejecuta el negocio; hace visible, gobernable y operable su ejecucion.

## Tabla de contenidos

1. [Introduccion](#introduccion)
2. [Objetivo principal](#objetivo-principal)
3. [Principios de diseno](#principios-de-diseno)
4. [Personalidad del producto](#personalidad-del-producto)
5. [Dashboard](#dashboard)
6. [Listado de casos](#listado-de-casos)
7. [Detalle del caso](#detalle-del-caso)
8. [Timeline](#timeline)
9. [Estados](#estados)
10. [Prioridad visual](#prioridad-visual)
11. [Navegacion](#navegacion)
12. [Uso de colores](#uso-de-colores)
13. [Iconografia](#iconografia)
14. [Tipografia](#tipografia)
15. [Estados vacios](#estados-vacios)
16. [Mensajes](#mensajes)
17. [Responsive](#responsive)
18. [Que evitar](#que-evitar)
19. [Evolucion futura](#evolucion-futura)
20. [Estado del documento](#estado-del-documento)

## Introduccion

Caimmand es un Centro de Operaciones. La experiencia de uso no es un accesorio del producto: es parte de la propuesta de valor. Un operador que no comprende el estado de un caso en segundos no esta gobernando; esta buscando informacion. Y un operador que busca informacion no esta operando.

La interfaz de Caimmand existe para reducir la carga cognitiva del operador. Cada pantalla, cada elemento, cada decision visual debe quitar trabajo mental, no sumarlo. El operador abre Caimmand para comprender que esta pasando, decidir que hacer y actuar. Todo lo que no sirve a ese proposito es ruido.

Este documento define la identidad visual y la experiencia de uso de Caimmand. No describe implementacion tecnica. No describe componentes. No describe estilos. Define principios, layout, jerarquia y tono. Sirve como referencia para cualquier persona que disene o implemente una pantalla del producto, hoy o en el futuro.

### Caimmand no es un sistema administrativo

Caimmand no es un CRUD. No es un ERP. No es un BPM. No es un gestor de formularios.

Caimmand es la sala de control desde donde un operador monitorea y gobierna la ejecucion del negocio en tiempo real. La interfaz debe transmitir esa naturaleza: control, visibilidad, decision. No transmite administracion, captura de datos ni cumplimiento burocratico.

Toda decision de UX debe responder a esa filosofia. Si una pantalla se parece a un formulario administrativo, esta mal. Si una pantalla se parece a un panel de control operativo, esta bien.

## Objetivo principal

> Un operador debe comprender un caso en menos de 10 segundos.

Este es el objetivo rector de toda la experiencia de uso de Caimmand. Todo el documento se construye alrededor de esta idea. Toda pantalla, toda decision de layout, toda eleccion visual se mide contra esta pregunta: ayuda al operador a comprender un caso en menos de diez segundos?

Comprension significa que el operador, al abrir un caso, puede responder sin esfuerzo:

- Que esta pasando con este caso ahora mismo.
- Que paso hasta llegar a este punto.
- Que falta por hacer.
- Si tiene que hacer algo el o si la operacion sigue sola.

Si el operador necesita scrollear, abrir pestaas, leer un log o consultar otra herramienta para responder esas preguntas, la pantalla no cumple su proposito. Diez segundos no es una metrica arbitraria: es el tiempo que tarda una persona en perder atencion y buscar informacion en otro lado.

### Que significa en la practica

- La informacion mas importante se ve primero, sin accion del operador.
- El estado del caso es visible de un vistazo, sin necesidad de leer texto.
- La timeline se lee de arriba hacia abajo como una historia, no como una tabla.
- El contexto del caso se presenta estructurado, no como una masa de texto.
- Las acciones disponibles estan visibles, no escondidas en menus.

## Principios de diseno

### Claridad antes que complejidad

Una pantalla clara vale mas que una pantalla completa. Si una informacion no es esencial para la pregunta que la pantalla responde, no debe estar ahi. La complejidad visual no agrega valor: lo resta. Un operador con demasiada informacion no tiene mas informacion; tiene menos capacidad de decidir.

### Informacion antes que decoracion

La decoracion sirve a la informacion, no al reves. Un color, un icono o un espaciado existen para hacer la informacion mas comprensible. Si un elemento visual no ayuda a comprender, decora por decorar y debe eliminarse. Caimmand no es un producto que se mire; es un producto que se usa.

### Acciones visibles

Las acciones que el operador puede realizar sobre un caso estan visibles en el contexto donde tienen sentido. No escondidas en menus desplegables. No enterradas en modales. No separadas de la informacion sobre la que operan. Si el operador ve un caso suspendido, la accion para reactivarlo esta al lado del estado, no en otra pantalla.

### Navegacion simple

El operador nunca debe preguntarse donde esta ni como volver. La navegacion tiene pocas opciones, claramente etiquetadas y siempre presentes. No hay rutas alternativas, no hay flujos ocultos. El operador llega a un caso desde el listado, y vuelve al listado desde el detalle. No hay laberinto.

### Consistencia

Pantallas que responden preguntas similares se ven similares. El estado de un caso se representa igual en el listado, en el detalle y en el dashboard. El icono de una Case Definition es el mismo en todos los lugares donde aparece. La consistencia reduce el aprendizaje y la confusion. Un operador que aprendio a leer una pantalla aprendio a leer todas.

### Poco ruido visual

Caimmand transmite calma operativa. Sin bordes innecesarios. Sin separadores donde el espaciado basta. Sin elementos decorativos que compitan con la informacion. El espacio en blanco es contenido: separa, jerarquiza y respira. Una pantalla densa no transmite seriedad; transmite desorden.

### Una pantalla responde una pregunta concreta

Cada pantalla de Caimmand responde una unica pregunta operativa:

- El Dashboard responde: que esta pasando ahora?
- El Listado responde: que necesito revisar?
- El Detalle responde: que paso, que esta pasando y que debo hacer?

Si una pantalla intenta responder mas de una pregunta, se divide. Si una pantalla no responde ninguna pregunta, no deberia existir. Esta regla previene la acumulacion de funciones que convierte un Centro de Operaciones en un panel administrativo.

## Personalidad del producto

Caimmand transmite una personalidad especifica. La interfaz, el tono, los mensajes y el flujo deben ser coherentes con ella.

### Que transmite

| Rasgo       | Significado                                                          |
|-------------|----------------------------------------------------------------------|
| Profesional | Habla el lenguaje de la operacion. Sin frivolidades, sin decoracion innecesaria. |
| Confiable   | Muestra la verdad del caso. No oculta, no maquilla. Si algo fallo, lo dice claro. |
| Operativo   | Orientado a la accion. Cada pantalla invita a decidir, no a contemplar. |
| Moderno     | Lenguaje visual contemporaneo. Limpio, espacioso, sin retorica de sistema legacy. |
| Tranquilo   | No genera ansiedad visual. Los colores, el ritmo y el espaciado respiran. |
| Enfocado    | Una cosa a la vez. La pantalla no compite por la atencion del operador. |

### Que no transmite

| Rasgo                          | Por que evitarlo                                              |
|--------------------------------|---------------------------------------------------------------|
| Caos                           | Un Centro de Operaciones no se ve desordenado. El desorden genera incertidumbre y la incertidumbre genera errores. |
| Sobrecarga                     | Demasiada informacion visible no es transparencia; es ruido.  |
| Interfaz administrativa tradicional | Caimmand no es un formulario. Las pantallas administrativas transmiten burocracia, no operacion. |
| Formularios infinitos          | La captura de datos masiva no es parte de la experiencia. Caimmand recibe contexto de sistemas externos; no lo pide campo por campo. |
| Urgencia permanente            | El rojo, las animaciones y las alertas son excepcionales. Si todo es urgente, nada lo es. |

## Dashboard

### Que responde

> Que esta pasando ahora?

El Dashboard es la pantalla de inicio. El operador llega y, sin hacer nada, comprende el estado general de la operacion. No necesita navegar, ni filtrar, ni buscar. La informacion esta esperandolo.

### Elementos

| Elemento                | Descripcion                                                                 |
|-------------------------|-----------------------------------------------------------------------------|
| Total de casos          | Volumen total de casos bajo gobierno de Caimmand.                           |
| Casos creados           | Casos recien llegados, aun sin operar activamente.                          |
| Casos finalizados       | Casos que completaron su ciclo con exito.                                   |
| Requieren intervencion  | Casos que necesitan accion humana para avanzar. Principal KPI operativo.   |
| Acceso al listado       | Entrada directa al Listado de Casos desde el Dashboard.                    |

### Reglas

- Los KPIs se presentan como tarjetas numericas grandes, no como tablas.
- "Requieren intervencion" tiene enfasis visual distintivo: es el KPI que el operador mira primero.
- Si no hay casos, el Dashboard muestra un estado vacio orientador, no una pantalla en blanco.
- No se incluyen graficos complejos en el MVP. Los numeros son suficientes para gobernar.

### Wireframe

```
+---------------------------------------------------------------+
|  Dashboard                                            [Ver casos] |
|  Estado operativo de los casos gobernados por Caimmand.       |
+---------------------------------------------------------------+

+-----------------+ +-----------------+ +-----------------+ +-----------------+
|  Total de Casos | |  Creados        | |  Finalizados    | |  Requieren      |
|                 | |                 | |                 | |  Intervencion   |
|       42        | |       12        | |       25        | |        5        |
+-----------------+ +-----------------+ +-----------------+ +-----------------+
                                                                    ^
                                                              enfasis visual

+---------------------------------------------------------------+
|  Caimmand PoC · v0.1                                          |
|  Caimmand no ejecuta el negocio; hace visible, gobernable y   |
|  operable su ejecucion.                                       |
+---------------------------------------------------------------+
```

### Estado vacio

```
+---------------------------------------------------------------+
|  Dashboard                                                    |
+---------------------------------------------------------------+
                                                               |
  No hay casos registrados.
  Crea el primero con POST /api/cases.

+---------------------------------------------------------------+
```

## Listado de casos

### Que responde

> Que necesito revisar?

El Listado es la pantalla de operacion diaria. El operador entra para encontrar los casos que requieren su atencion. No entra a navegar; entra a decidir.

### Elementos

| Elemento          | Descripcion                                                            |
|-------------------|------------------------------------------------------------------------|
| Tabla de casos    | Una fila por caso. Columnas esenciales, no exhaustivas.                |
| Filtro por estado | Acota la lista al estado que interesa operar.                         |
| Filtro por tipo   | Acota por Case Definition cuando el operador trabaja un tipo concreto.|
| Navegacion        | Click en una fila abre el Detalle del Caso.                           |

### Reglas

- Pocas columnas: Titulo, Case Definition, Estado, Sistema origen, Fecha de creacion. Nada mas.
- El estado se representa con un badge visual, no con texto plano.
- Orden por defecto: fecha de creacion descendente. Lo mas reciente arriba.
- Cada fila es seleccionable. El cursor indica que es clickeable.
- Filtros simples: dos desplegables, nada mas. No busqueda libre en el MVP.
- Si no hay casos que mostrar, se muestra un estado vacio orientador.

### Wireframe

```
+---------------------------------------------------------------+
|  Casos                                        [Volver al Dashboard] |
|  Listado de casos gobernados por Caimmand.                    |
+---------------------------------------------------------------+

+---------------------------------------------------------------+
|  Estado: [Todos      v]   Case Definition: [Todas      v]      |
|                                              [Limpiar]         |
+---------------------------------------------------------------+

+---------------------------------------------------------------+
|  Titulo              | Case Definition  | Estado  | Origen | Creado  |
|---------------------+------------------+---------+--------+--------|
|  Recordatorio de...  | Recordatorio...  | Creado  | HIS    | 10:30  |
|  Auditoria medica... | Auditoria Medica | En curso| HIS    | 09:15  |
|  Reclamo de...       | Reclamo          | Suspend.| CRM    | 08:02  |
+---------------------------------------------------------------+

  3 caso(s) encontrado(s).
```

### Estado vacio

```
+---------------------------------------------------------------+
|  Casos                                                        |
+---------------------------------------------------------------+
                                                               |
  No existen casos para mostrar.
```

## Detalle del caso

### Que responde

> Que paso? Que esta pasando? Que debo hacer?

El Detalle es la pantalla principal del producto. Es donde el operador gobierna un caso concreto. Todo el resto de Caimmand conduce a esta pantalla. Si el Detalle no funciona, el producto no funciona.

### Jerarquia visual

1. **Identidad del caso**: titulo, estado y tipo. El operador sabe que caso esta viendo antes de leer nada.
2. **Metadata operativa**: sistema origen, fechas de creacion y actualizacion, identificador. Contexto administrativo, no protagonista.
3. **Contexto**: la informacion especifica del caso, estructurada y legible. No como JSON crudo.
4. **Timeline**: la historia funcional del caso. Cronologica, legible, orientada al operador.
5. **Acciones**: lo que el operador puede hacer ahora. Visibles, junto al contexto, no en otra pantalla.

### Reglas

- El titulo del caso es el elemento mas grande de la pantalla. El operador lo ve primero.
- El estado es un badge junto al titulo, no una linea de texto.
- El contexto se presenta formateado y legible, nunca como una cadena JSON en una linea.
- La timeline ocupa el espacio que merece: es la herramienta principal de comprension.
- Las acciones estan al alcance, en el contexto del detalle, no en una barra separada.
- Un caso inexistente muestra un estado vacio orientador, no un error tecnico.

### Wireframe

```
+---------------------------------------------------------------+
|  <- Volver al listado                                          |
|  Detalle del Caso                                             |
+---------------------------------------------------------------+

+---------------------------------------------------------------+
|  Recordatorio del turno de Juan Perez - 18/07 10:30  [Creado] |
|  Recordatorio de Turno · APPOINTMENT_REMINDER                 |
|  -----------------------------------------------------------  |
|  Sistema origen: HIS        Creacion: 14/07 10:30            |
|  Actualizacion: 14/07 10:30      Id: 52abb42f-...            |
+---------------------------------------------------------------+

+---------------------------------------------------------------+
|  Contexto                                                     |
|  +---------------------------------------------------------+ |
|  |  {                                                       | |
|  |    "patientId": 12345,                                  | |
|  |    "patientName": "Juan Perez",                         | |
|  |    "appointmentDate": "2026-07-18T10:30",               | |
|  |    "doctor": "Dra. Lopez"                                | |
|  |  }                                                       | |
|  +---------------------------------------------------------+ |
+---------------------------------------------------------------+

+-------------------------------+ +-------------------------------+
|  Timeline                     | |  Acciones                     |
|  [Creacion] HIS - 14/07 10:30 | |  [Suspender]  [Finalizar]     |
|  Caso creado por HIS.         | |  [Cancelar]                   |
|                               | |                               |
|  [Aviso] n8n - 14/07 11:00     | |  Agregar evento (cuando el    |
|  SMS enviado al paciente.      | |  caso no esta finalizado/)    |
|                               | |  cancelado.                   |
|  [+ Agregar evento] (form     | |                               |
|  Tipo/Content/Registrar)      | |                               |
|  visible si no esta terminal  | |                               |
+-------------------------------+ +-------------------------------+
```

### Estado vacio (caso inexistente)

```
+---------------------------------------------------------------+
|  Detalle del Caso                                             |
+---------------------------------------------------------------+
                                                               |
  No se encontro el caso solicitado.
  [Volver al listado]
```

## Timeline

### Filosofia

La Timeline no es un log tecnico. No es una tabla de auditoria. No es una lista de eventos del sistema.

La Timeline es la historia funcional del caso. Es lo que un operador o un supervisor lee para comprender que paso, que esta pasando y que falta. Se escribe en lenguaje humano. Se lee de arriba hacia abajo como un relato. Cada evento responde a una pregunta: que ocurrio, quien lo genero y cuando.

### Reglas

- Orden cronologico descendente: lo mas reciente arriba.
- Cada evento muestra tipo, origen, contenido y momento.
- El lenguaje es humano: "SMS enviado al paciente", no "evento type=aviso origin=agente".
- Iconografia simple y consistente: un icono por tipo de evento, no mas.
- Colores discretos: la Timeline no compite con el estado del caso.
- El contenido del evento es descriptivo, no tecnico.
- La Timeline crece: los eventos no se modifican, pero la lista se extiende.

### Wireframe

```
+---------------------------------------------------------------+
|  Timeline                                                     |
+---------------------------------------------------------------+

  18/07 09:30  Confirmacion
  Paciente                     Paciente confirma asistencia via
                               respuesta al SMS.

  17/07 10:00  Recordatorio
  Agente de Recordatorio       Segundo SMS enviado al paciente.

  16/07 08:06  Aviso
  Agente de Recordatorio       SMS enviado al paciente.

  16/07 08:00  Creacion
  Sistema de Turnos            Caso creado para el turno de Juan
                               Perez del 18/07 a las 10:30.
```

## Estados

Los estados del caso se representan visualmente para que el operador los identifique de un vistazo, sin leer texto. Cada estado tiene una personalidad visual coherente en todas las pantallas: listado, detalle y dashboard.

### Estados del MVP

| Estado     | Significado operativo                              | Personalidad visual              |
|------------|----------------------------------------------------|----------------------------------|
| Creado     | Caso registrado, sin operar aun.                  | Activo, reciente, neutro.        |
| En curso   | Caso en operacion activa.                          | En movimiento, dinamico.        |
| Suspendido | Caso pausado, requiere decision para reanudar.     | Atencion, alerta discreta.      |
| Finalizado | Caso completo, ciclo cerrado con exito.            | Resuelto, sereno.               |
| Cancelado  | Caso interrumpido, ciclo cerrado sin exito.        | Cerrado, sin ambiguedad.        |

### Principios de representacion

- El estado se representa con un badge visual: color de fondo + texto corto.
- El color comunica la naturaleza del estado, no decora.
- Un estado terminal (Finalizado, Cancelado) se ve distinto de un estado activo.
- El mismo badge aparece igual en el listado, en el detalle y en el dashboard.
- No se mezclan colores de estado con colores de prioridad ni de alerta.
- El rojo es excepcional. Reservado para situaciones que requieren accion inmediata.

### Transiciones

Las transiciones entre estados no se muestran en la interfaz como un diagrama. El operador actua sobre el caso y el estado cambia. La Timeline registra el cambio como un evento funcional. La interfaz muestra el estado actual, no el historial de transiciones; eso vive en la Timeline.

## Prioridad visual

La prioridad visual de un caso no siempre coincide con su estado. Un caso en estado Suspendido puede requerir intervencion urgente. Un caso en estado Creado puede ser rutinario. La prioridad operativa la define la necesidad de accion humana, no el estado por si solo.

### Requiere intervencion

"Requiere intervencion" es el principal indicador operativo de Caimmand. Un caso requiere intervencion cuando necesita accion humana para avanzar. En el MVP actual, esto se materializa con los casos en estado Suspendido: fueron pausados y esperan una decision del operador para reanudarse.

La definicion es deliberadamente operativa, no tecnica. A medida que el producto evolucione, "requiere intervencion" puede enriquecerse con otros criterios: tareas vencidas, SLA excedido, eventos de escalado. Lo que no cambia es el principio: el operador abre Caimmand y lo primero que ve es cuanto trabajo lo esta esperando.

### Reglas

- "Requiere intervencion" tiene enfasis visual distintivo en el Dashboard.
- Un caso que requiere intervencion se senala en el listado, no solo en el detalle.
- El indicador no es rojo por defecto: es destacado, no alarmante.
- La prioridad visual nunca oculta el estado. Ambos son visibles simultaneamente.

## Navegacion

La navegacion de Caimmand es minima. El operador no explora el producto: opera casos. La navegacion refleja eso.

### Estructura

| Opcion    | Destino                   |
|----------|---------------------------|
| Dashboard | Pantalla de inicio.      |
| Casos     | Listado de casos.        |

No hay mas opciones en el MVP. No se agregan entradas a secciones que no existen. No se anticipan pantallas futuras en el menu. Si una capacidad no esta implementada, no aparece en la navegacion.

### Reglas

- El menu lateral es simple, con dos opciones claramente etiquetadas.
- La opcion activa se senala visualmente.
- "Casos" se resalta tanto en el listado como en el detalle: el operador sabe donde esta.
- No hay breadcrumbs en el MVP. El boton "Volver" basta.
- La navegacion es client-side: cambiar de pantalla no recarga la aplicacion.

## Uso de colores

Caimmand no define una paleta exacta. Define principios. Los colores se eligen y aplican segun estos principios, no segun preferencias esteticas.

### Principios

- Los colores comunican. Cada color que aparece en la interfaz significa algo operativo.
- Los colores no decoran. Un elemento sin significado no lleva color.
- El color de fondo principal es neutro y claro. El contenido es el protagonista.
- El color de acento principal es unico y consistente en toda la aplicacion.
- Los estados del caso tienen colores coherentes con su naturaleza operativa.
- El rojo es excepcional. Reservado para situaciones que requieren accion inmediata o que representan un error.
- El verde indica resolucion o exito, no decoracion generica.
- El amarillo senala atencion: algo requiere revision, pero no es critico.
- No se usan mas de tres colores de estado simultaneamente en una pantalla.

## Iconografia

Los iconos en Caimmand son funcionales, no decorativos. Cada icono significa algo. Un icono sin significado es ruido.

### Principios

- Pocos iconos. Solo donde aportan comprension.
- Siempre con significado operativo: tipo de evento, tipo de participante, accion disponible.
- Un icono se acompaña de texto. El icono acelera; el texto confirma.
- Nunca un icono solo, sin etiqueta, en una accion critica.
- Los iconos son consistentes: el mismo concepto usa el mismo icono en toda la aplicacion.
- Los iconos de Case Definition provienen de su configuracion. Son parte de la identidad del tipo de operacion.

## Tipografia

La tipografia de Caimmand prioriza legibilidad y jerarquia. No es decorativa.

### Principios

- Una sola familia tipografica. Sin mezclar fuentes.
- Jerarquia clara por tamano y peso, no por color ni por decoracion.
- Los titulos son grandes y definidos. El operador identifica la pantalla por el titulo.
- El cuerpo de texto es legible, con tamano confortable y espaciado generoso.
- Los datos operativos (numeros, fechas, identificadores) usan tipografia monoespaciada para alinear y comparar.
- Se evita la densidad. El espaciado entre elementos es generoso: la pantalla respira.
- No se usan mas de cuatro niveles de jerarquia tipografica en una pantalla.

## Estados vacios

Las pantallas vacias orientan al usuario. Nunca muestran una tabla sin filas sin contexto. Nunca muestran una pantalla en blanco. Un estado vacio es una oportunidad para guiar al operador, no un hueco.

### Principios

- Un estado vacio explica por que esta vacio y que hacer a continuacion.
- El tono es orientador, no de error.
- Si la accion esperada es tecnica (como crear un caso via API), se indica sin ambages.
- Los estados vacios son consistentes: mismo estilo, mismo tono, en toda la aplicacion.
- Un estado vacio nunca es un dead end. Siempre ofrece una salida.

### Ejemplos

| Pantalla  | Estado vacio                                              |
|-----------|-----------------------------------------------------------|
| Dashboard | No hay casos registrados. Crea el primero.                 |
| Listado  | No existen casos para mostrar.                            |
| Detalle  | No se encontro el caso solicitado. Volver al listado.     |

## Mensajes

El tono de Caimmand es profesional, claro y humano. Los mensajes hablan al operador como un colega, no como un sistema.

### Principios

- Profesional: sin coloquialismos, sin humor innecesario, sin alarma.
- Claro: una idea por mensaje. Sin ambiguedad.
- Humano: escribe como una persona, no como un log. "No se encontro el caso", no "Error 404: entity not found".
- Sin tecnicismos innecesarios. Si el operador no necesita saber el nombre interno de la entidad, no se lo decimos.
- Los mensajes de error explican que paso y que hacer. No solo dicen que fallo.
- Los mensajes de exito son discretos. No festejan.

### Ejemplos

| Situacion              | Mensaje correcto                              | Mensaje incorrecto                  |
|------------------------|-----------------------------------------------|-------------------------------------|
| Caso inexistente       | No se encontro el caso solicitado.            | Entity not found.                   |
| Sin casos              | No hay casos registrados.                     | No records to display.              |
| Validacion fallida     | Title es obligatorio.                         | ValidationException: Title required.|
| Operacion exitosa      | (discreto, sin mensaje si no es necesario)    | Success! Operation completed!       |

## Responsive

Caimmand se disea para escritorio primero. El operador trabaja desde una estacion de operacion, no desde un telefono.

### Prioridades

| Dispositivo | Prioridad | Comportamiento esperado                                    |
|-------------|-----------|------------------------------------------------------------|
| Desktop     | Primaria  | Experiencia completa. Layout optimo, todas las capacidades.|
| Tablet      | Aceptable | Funcional. Layout adaptado, sin perdida de capacidad.     |
| Mobile      | Funcional | Consulta basica. No prioridad del MVP.                    |

### Reglas

- El layout se adapta: las tarjetas se apilan, las tablas se comprimen, los filtros se reordenan.
- En mobile, el listado de casos prioriza titulo y estado. Las demas columnas se ocultan.
- En mobile, el detalle prioriza identidad y timeline. El contexto se colapsa.
- No se diseñan funcionalidades exclusivas de mobile en el MVP.

## Que evitar

Esta seccion enumera explicitamente los anti-patrones de UX que Caimmand no tolera.

| Anti-patron                      | Por que evitarlo                                              |
|----------------------------------|---------------------------------------------------------------|
| Demasiadas columnas en tablas    | El operador no compara veinte atributos a la vez. Cinco es el limite. |
| Demasiados botones por pantalla  | Cada boton compite por atencion. Si hay mas de tres acciones primarias, la pantalla hace demasiado. |
| Modales innecesarios             | Rompen el flujo. Se usan solo para confirmar acciones irreversibles. |
| Pestaas excesivas                | Si una pantalla necesita pestaas para caber, probablemente necesita dividirse en dos pantallas. |
| Dashboards llenos de graficos    | Los numeros gobernaban antes que los graficos. Un grafico que no ayuda a decidir es decoracion. |
| Tablas interminables             | Si la lista no cabe en una pantalla sin scrollear mucho, necesita filtros o paginacion. |
| Colores excesivos                | Cada color adicional reduce el impacto de los que ya estan.  |
| Formularios gigantes             | Caimmand recibe contexto de sistemas externos. No pide veinte campos al operador. |
| Animaciones decorativas          | La animacion sirve para orientar, no para entretener.        |
| Jerarquia tipografica profunda   | Mas de cuatro niveles de jerarquia confunde al operador.    |
| Iconos sin etiqueta              | Un icono sin texto es adivinanza. El operador no debe adivinar. |
| Estados vacios sin contexto      | Una tabla sin filas no es informacion; es un dead end.      |

## Evolucion futura

Estos principios no son provisionales. Son la identidad del producto. Cuando Caimmand crezca, cuando se agregen pantallas, cuando se incorporen capacidades nuevas, la experiencia de uso debe seguir respondiendo a los mismos principios.

### Reglas de evolucion

- Toda pantalla nueva responde una pregunta concreta, como las existentes.
- Toda pantalla nueva se mide contra el objetivo de los diez segundos.
- La navegacion crece solo cuando una nueva capacidad justifica una entrada en el menu.
- Los principios de color, tipografia e iconografia se mantienen.
- La personalidad del producto se preserva: profesional, confiable, operativo, moderno, tranquilo, enfocado.
- Lo que se avoid hoy se avoid manana. El crecimiento no justifica la degradacion.

Si una futura pantalla no puede construirse respetando estos principios, el problema no es la pantalla: es la concepcion de la capacidad. Se repiensa antes de romper la identidad del producto.

## Estado del documento

Este documento se encuentra en estado **Draft**, version **0.1**.

El contenido define la guia oficial de experiencia de usuario de Caimmand y es coherente con el PDD, el documento de arquitectura, el modelo de dominio y el MVP. Los principios, la personalidad del producto, los wireframes y las reglas de evolucion sirven como referencia para cualquier persona que disene o implemente una pantalla del producto.

Pendiente de revision por el equipo CAI Process Grid.
