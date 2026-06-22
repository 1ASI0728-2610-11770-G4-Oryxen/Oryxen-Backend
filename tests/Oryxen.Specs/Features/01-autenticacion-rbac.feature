Feature: Control de Acceso Basado en Roles (RBAC)
  Como administrador de la plataforma Oryxen
  Quiero que los usuarios con rol FARMER solo accedan a sus propias plantas
  Y que los usuarios con rol ADMIN tengan acceso global
  Para garantizar el aislamiento de datos entre agricultores

  Background:
    Given que el sistema Oryxen está desplegado y operativo
    And que existe un usuario "farmer@oryxen.io" con rol "FARMER"
    And que existe un usuario "admin@oryxen.io" con rol "ADMIN"
    And que el usuario "farmer@oryxen.io" tiene 3 plantas registradas
    And que el usuario "other@oryxen.io" tiene 2 plantas registradas

  Scenario: FARMER consulta sus propias plantas
    Given que me he autenticado como "farmer@oryxen.io" con rol "FARMER"
    When envío una petición GET a "/api/v1/users/{myUserId}/plants"
    Then la respuesta tiene código HTTP 200 OK
    And el cuerpo de la respuesta contiene exactamente 3 plantas
    And todas las plantas devueltas pertenecen a mi usuario

  Scenario: FARMER intenta consultar las plantas de otro usuario
    Given que me he autenticado como "farmer@oryxen.io" con rol "FARMER"
    When envío una petición GET a "/api/v1/users/{otherUserId}/plants"
    Then la respuesta tiene código HTTP 403 Forbidden
    And el cuerpo de la respuesta contiene un ProblemDetails RFC 7807

  Scenario: ADMIN consulta las plantas de cualquier usuario
    Given que me he autenticado como "admin@oryxen.io" con rol "ADMIN"
    When envío una petición GET a "/api/v1/users/{otherUserId}/plants"
    Then la respuesta tiene código HTTP 200 OK
    And el cuerpo de la respuesta contiene exactamente 2 plantas
    And las plantas devueltas pertenecen al usuario consultado

  Scenario: FARMER crea una planta en su propia cuenta
    Given que me he autenticado como "farmer@oryxen.io" con rol "FARMER"
    When envío una petición POST a "/api/v1/plants" con los datos:
      | name     | type      | location           |
      | Tomate   | Tomatera  | Invernadero Norte  |
    Then la respuesta tiene código HTTP 201 Created
    And la planta creada tiene asignado mi userId como propietario
    And el estado inicial de la planta es "Healthy"

  Scenario: FARMER elimina una planta de su propiedad
    Given que me he autenticado como "farmer@oryxen.io" con rol "FARMER"
    And que la planta "11111111-2222-3333-4444-555555555555" pertenece a mi usuario
    When envío una petición DELETE a "/api/v1/plants/{plantId}"
    Then la respuesta tiene código HTTP 204 No Content
    And la planta ya no existe en la base de datos
