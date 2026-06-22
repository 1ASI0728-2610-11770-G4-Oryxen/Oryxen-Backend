Feature: Notificaciones y Alertas Automáticas
  Como agricultor usuario de la plataforma Oryxen
  Quiero recibir notificaciones automáticas sobre eventos críticos de salud de mis plantas
  Para mantenerme informado y tomar medidas correctivas a tiempo

  Background:
    Given que el sistema Oryxen está desplegado y operativo
    And que existe una planta registrada con ID "11111111-2222-3333-4444-555555555555"
    And que la planta pertenece al usuario "farmer@oryxen.io"

  Scenario: Notificación automática por HealthScore crítico
    Given que me he autenticado como "farmer@oryxen.io" con rol "FARMER"
    And que el Sensor Lite envía una lectura con soilMoisture=10%, humidity=20%, temperature=35°C
    When el sistema calcula un HealthScore menor a 40
    Then se crea una notificación de tipo "CriticalHealth" para el usuario propietario de la planta
    And la notificación contiene el nombre de la planta y el puntaje de salud actual
    And la notificación queda persistida en la tabla notifications con IsRead=false

  Scenario: Consulta de bandeja de notificaciones
    Given que me he autenticado como "farmer@oryxen.io" con rol "FARMER"
    And que existen notificaciones previas para el usuario
    When envío una petición GET a "/api/v1/notifications"
    Then la respuesta tiene código HTTP 200 OK
    And el cuerpo contiene un arreglo de notificaciones ordenadas por fecha descendente

  Scenario: Conteo de notificaciones no leídas
    Given que me he autenticado como "farmer@oryxen.io" con rol "FARMER"
    And que el usuario tiene notificaciones sin leer
    When envío una petición GET a "/api/v1/notifications/unread/count"
    Then la respuesta tiene código HTTP 200 OK
    And el campo "count" es mayor a 0

  Scenario: Marcar notificación como leída
    Given que me he autenticado como "farmer@oryxen.io" con rol "FARMER"
    And que existe una notificación no leída con ID "22222222-3333-4444-5555-666666666666"
    When envío una petición POST a "/api/v1/notifications/22222222-3333-4444-5555-666666666666/read"
    Then la respuesta tiene código HTTP 204 No Content
    And la notificación ahora tiene IsRead=true en la base de datos

  Scenario: Autenticación requerida para acceder a notificaciones
    Given que no he iniciado sesión
    When envío una petición GET a "/api/v1/notifications"
    Then la respuesta tiene código HTTP 401 Unauthorized

  Scenario: Vista de notificaciones en la aplicación web
    Given que me he autenticado como "farmer@oryxen.io" con rol "FARMER"
    And que navego a la ruta "/notifications"
    Then puedo ver el listado completo de mis notificaciones
    And cada notificación muestra título, mensaje, tipo, fecha y estado de lectura
    And las no leídas tienen un borde verde distintivo
    And puedo marcarlas como leídas mediante un botón

  Scenario: Badge de notificaciones en el Header
    Given que me he autenticado como "farmer@oryxen.io" con rol "FARMER"
    And que el usuario tiene notificaciones sin leer
    When se carga la aplicación web
    Then el Header muestra un ícono de campana con un badge numérico
    And el badge muestra la cantidad de notificaciones no leídas
