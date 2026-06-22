Feature: Bloqueo de Accesos No Autorizados (HTTP 401)
  Como API Gateway de Oryxen
  Quiero que todos los endpoints protegidos validen la presencia y validez del token JWT
  Para bloquear el acceso a clientes no autenticados o con tokens expirados

  Background:
    Given que el sistema Oryxen está desplegado y operativo
    And que los endpoints protegidos requieren autenticación JWT Bearer
    And que el middleware de excepciones produce respuestas RFC 7807 ProblemDetails

  Scenario: Petición a endpoint protegido sin token JWT
    Given que soy un cliente no autenticado
    When envío una petición GET a "/api/v1/auth/me" sin cabecera "Authorization"
    Then la respuesta tiene código HTTP 401 Unauthorized
    And el cuerpo de la respuesta tiene content-type "application/problem+json"
    And el campo "title" del ProblemDetails es "Unauthorized"
    And el campo "status" es 401

  Scenario: Petición a endpoint protegido con token JWT malformado
    Given que soy un cliente con un token JWT malformado "abc.not-a-jwt.token"
    When envío una petición GET a "/api/v1/auth/me" con cabecera "Authorization: Bearer abc.not-a-jwt.token"
    Then la respuesta tiene código HTTP 401 Unauthorized
    And el cuerpo de la respuesta contiene un ProblemDetails RFC 7807

  Scenario: Petición a endpoint protegido con token JWT expirado
    Given que tengo un token JWT emitido hace 2 horas con expiración de 1 hora
    When envío una petición GET a "/api/v1/auth/me" con cabecera "Authorization: Bearer {expiredToken}"
    Then la respuesta tiene código HTTP 401 Unauthorized
    And el cuerpo de la respuesta contiene un ProblemDetails con "title" igual a "Unauthorized"

  Scenario: Petición a endpoint protegido con token JWT con firma inválida
    Given que tengo un token JWT firmado con una clave secreta distinta a la del servidor
    When envío una petición GET a "/api/v1/auth/me" con cabecera "Authorization: Bearer {tamperedToken}"
    Then la respuesta tiene código HTTP 401 Unauthorized
    And el acceso es bloqueado antes de llegar al controller

  Scenario: Petición a endpoint de telemetría sin autenticación de dispositivo
    Given que soy un cliente sin token JWT
    When envío una petición POST a "/api/v1/telemetry" con un payload válido
    Then la respuesta tiene código HTTP 401 Unauthorized
    And la lectura de telemetría NO se persiste en la base de datos

  Scenario: Petición a endpoint de plantas sin autenticación
    Given que soy un cliente no autenticado
    When envío una petición GET a "/api/v1/users/{userId}/plants" sin cabecera "Authorization"
    Then la respuesta tiene código HTTP 401 Unauthorized
    And el cuerpo de la respuesta tiene content-type "application/problem+json"

  Scenario: Petición a endpoint público sin token es permitida
    Given que soy un cliente no autenticado
    When envío una petición POST a "/api/v1/auth/login" con credenciales válidas
    Then la respuesta tiene código HTTP 200 OK
    And el cuerpo de la respuesta contiene accessToken y refreshToken
    And no se requiere cabecera "Authorization" para endpoints de autenticación

  Scenario: Refresh token inválido devuelve 401 al intentar rotar sesión
    Given que tengo un refresh token revocado o expirado "invalid-refresh-token"
    When envío una petición POST a "/api/v1/auth/refresh" con payload:
      """
      { "refreshToken": "invalid-refresh-token" }
      """
    Then la respuesta tiene código HTTP 401 Unauthorized
    And el cuerpo de la respuesta contiene un ProblemDetails con "title" igual a "Unauthorized"
