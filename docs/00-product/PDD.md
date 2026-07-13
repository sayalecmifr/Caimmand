# Caimmand - Product Definition Document

| Campo    | Valor                |
|----------|----------------------|
| Producto | Caimmand             |
| Version  | 0.1                  |
| Estado   | Draft                |
| Fecha    | 2026-07-13           |
| Autor    | CAI Process Grid Team |

> Caimmand no ejecuta el negocio; hace visible, gobernable y operable su ejecucion.

## Tabla de contenidos

1. [Introduccion](#introduccion)
2. [Vision del producto](#vision-del-producto)
3. [Problema que resuelve](#problema-que-resuelve)
4. [Mision](#mision)
5. [Principios del producto](#principios-del-producto)
6. [Conceptos fundamentales](#conceptos-fundamentales)
7. [Roles del sistema](#roles-del-sistema)
8. [Alcance](#alcance)
9. [Estado del documento](#estado-del-documento)

## Introduccion

Caimmand es la plataforma de operaciones y gobierno del ecosistema CAI Process Grid. Su proposito es proveer un punto unico desde el cual las organizaciones supervisen, controlen e intervengan en la ejecucion de casos impulsados por inteligencia artificial y automatizaciones.

Caimmand no ejecuta el negocio; hace visible, gobernable y operable su ejecucion. El producto se concentra en la operacion de casos ya existentes, aportando trazabilidad, observabilidad y gobierno sobre el trabajo realizado por automatizaciones, agentes de IA y personas.

## Vision del producto

Caimmand es la plataforma de operaciones y gobierno donde las organizaciones supervisan, controlan e intervienen en la ejecucion de casos impulsados por inteligencia artificial y automatizaciones.

## Problema que resuelve

Las organizaciones poseen multiples sistemas, automatizaciones y agentes de IA que ejecutan trabajo distribuido.

Sin embargo, no existe un punto unico desde donde supervisar el estado de los casos, comprender que ocurrio, intervenir cuando es necesario y auditar todo el ciclo de ejecucion.

Esta fragmentacion dificulta la trazabilidad, limita la capacidad de respuesta ante incidentes y obliga a reconstruir el contexto de un caso consultando multiples fuentes dispersas.

## Mision

Centralizar la operacion de los casos de negocio, proporcionando trazabilidad, observabilidad y gobierno sobre el trabajo realizado por automatizaciones, agentes IA y personas.

## Principios del producto

### Principio 1

El eje del producto es el Caso.

No el proceso.

No el agente.

No el workflow.

### Principio 2

Caimmand no disena procesos.

Opera procesos existentes.

### Principio 3

Los casos son creados por sistemas externos.

### Principio 4

La IA ejecuta.

Las personas supervisan y deciden.

### Principio 5

Toda la informacion relevante de un caso debe comprenderse en menos de diez segundos.

Respondiendo siempre:

- Que esta pasando?
- Que paso?
- Que falta?
- Tengo que hacer algo?

## Conceptos fundamentales

### Caso

Unidad central del producto. Representa la instancia de trabajo que se supervisa, controla y gobierna a lo largo de su ciclo de ejecucion.

### Sistema de Origen

Sistema externo responsable de la creacion del caso. Caimmand no crea casos; los recibe y opera.

### Tarea

Unidad de trabajo concreta asociada a un caso, ejecutada por una automatizacion, un agente de IA o una persona.

### Evento

Registro de un cambio o acontecimiento relevante dentro del ciclo de vida de un caso.

### Timeline

Secuencia cronologica de eventos y tareas de un caso, que permite reconstruir su historia y comprender su estado.

### Participante

Actor que interviene en un caso, sea una persona, una automatizacion o un agente de IA.

## Roles del sistema

### Operador

Encargado de ejecutar la operacion diaria de los casos: atender tareas pendientes, registrar eventos y mantener el flujo de cada caso.

### Supervisor

Responsable de supervisar el estado de los casos, intervenir cuando es necesario y asegurar la correcta ejecucion del trabajo, con apoyo en la trazabilidad y la observabilidad.

### Gerente

Responsable del gobierno global de la operacion: analisis de tendencias, auditoria y toma de decisiones de alto nivel sobre el conjunto de casos.

## Alcance

### Dentro (In Scope)

- Supervision
- Casos
- Timeline
- Tareas
- Auditoria
- Observabilidad

### Fuera (Out of Scope)

- Diseno BPM
- Diseno de workflows
- Creacion de agentes
- Prompt engineering
- Modelado BPMN

## Estado del documento

Este documento se encuentra en estado **Draft**, version **0.1**.

El contenido refleja las decisiones tomadas hasta la fecha por el equipo de producto. Las secciones seran ampliadas a medida que se consoliden nuevas definiciones funcionales y de negocio.

Pendiente de revision por el equipo CAI Process Grid.