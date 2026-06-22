Feature: Ingesta Automatizada de Telemetría IoT
  Como Sensor Lite instalado en un fundo agrícola
  Quiero enviar ráfagas de lecturas de suelo y ambiente al backend Oryxen
  Para que el sistema calcule el Health Score de la planta y persista el histórico

  Background:
    Given que el sistema Oryxen está desplegado y operativo
    And que existe una planta con ID "11111111-2222-3333-4444-555555555555"
    And que el Sensor Lite con deviceId "SL-SIM-001" está registrado

  Scenario: Ingesta de una lectura válida del Sensor Lite
    Given que el Sensor Lite "SL-SIM-001" ha tomado una lectura con:
      | soilMoisture | humidity | temperature | lightLevel |
      | 62           | 52       | 22          | 850        |
    When envío una petición POST a "/api/v1/telemetry" con el payload:
      """
      {
        "deviceId": "SL-SIM-001",
        "plantId": "11111111-2222-3333-4444-555555555555",
        "soilMoisture": 62,
        "humidity": 52,
        "temperature": 22,
        "lightLevel": 850
      }
      """
    Then la respuesta tiene código HTTP 201 Created
    And el campo "healthScore" es calculado por el PlantHealthCalculator
    And el "healthScore" es igual a 100 porque todas las métricas están en el rango ideal
    And el registro persiste en la base de datos con un timestamp de grabación

  Scenario: Ingesta de una lectura con métricas degradadas
    Given que el Sensor Lite "SL-SIM-001" ha tomado una lectura con:
      | soilMoisture | humidity | temperature | lightLevel |
      | 15           | 20       | 35          | 200        |
    When envío una petición POST a "/api/v1/telemetry" con el payload:
      """
      {
        "deviceId": "SL-SIM-001",
        "plantId": "11111111-2222-3333-4444-555555555555",
        "soilMoisture": 15,
        "humidity": 20,
        "temperature": 35,
        "lightLevel": 200
      }
      """
    Then la respuesta tiene código HTTP 201 Created
    And el "healthScore" es menor que 40
    And el estado de la planta se actualiza a "Critical"

  Scenario: Ingesta de una ráfaga de 5 lecturas consecutivas
    Given que el Sensor Lite "SL-SIM-001" envía 5 lecturas en un intervalo de 60 segundos
    When envío 5 peticiones POST a "/api/v1/telemetry" con métricas válidas
    Then cada respuesta tiene código HTTP 201 Created
    And todas las lecturas se persisten con su timestamp individual
    And al consultar GET "/api/v1/telemetry/{plantId}" devuelve las 5 lecturas ordenadas por timestamp descendente

  Scenario: Consulta del historial de telemetría de una planta
    Given que la planta "11111111-2222-3333-4444-555555555555" tiene 10 lecturas persistidas
    When envío una petición GET a "/api/v1/telemetry/11111111-2222-3333-4444-555555555555"
    Then la respuesta tiene código HTTP 200 OK
    And el cuerpo de la respuesta contiene una lista de registros de telemetría
    And cada registro incluye los campos: deviceId, plantId, soilMoisture, humidity, temperature, lightLevel, healthScore, recordedAt

  Scenario Outline: Cálculo determinístico del Health Score para distintos niveles de humedad del suelo
    Given que el Sensor Lite envía una lectura con soilMoisture = <soilMoisture>
    When el PlantHealthCalculator procesa la lectura con humidity = 50 y temperature = 22
    Then el healthScore es <expectedScore>
    And la banda de clasificación es <band>

    Examples:
      | soilMoisture | expectedScore | band      |
      | 55           | 100           | Healthy   |
      | 40           | 100           | Healthy   |
      | 70           | 100           | Healthy   |
      | 25           | 50            | Warning   |
      | 10           | 25            | Critical  |
      | 0            | 0             | Critical  |
