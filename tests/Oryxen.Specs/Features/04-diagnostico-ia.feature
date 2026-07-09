Feature: Diagnóstico Multimodal de Salud Vegetal por IA
  Como agricultor usuario de la plataforma Oryxen
  Quiero que el sistema analice fotografías de mis plantas junto con la telemetría IoT
  Para diagnosticar el estado de salud general, identificar anomalías, deficiencias o plagas
  Y recibir recomendaciones de cuidado o mitigación

  Background:
    Given que el sistema Oryxen está desplegado y operativo
    And que el servicio Gemini Vision API (gemini-2.0-flash) está configurado
    And que existe una planta registrada con ID "11111111-2222-3333-4444-555555555555"
    And que el Sensor Lite ha registrado telemetría reciente para esa planta

  Scenario: Diagnóstico exitoso con detección de anomalía foliar
    Given que me he autenticado como "farmer@oryxen.io" con rol "FARMER"
    And que tengo una fotografía de la planta con signos visibles de anomalía en las hojas
    When envío una petición POST a "/api/v1/ai/diagnoses" con la imagen multipart y plantId
    Then la respuesta tiene código HTTP 201 Created
    And el campo "detectedPest" contiene el nombre de la anomalía detectada
    And el campo "confidenceScore" es mayor o igual a 0.50
    And el campo "recommendation" contiene una recomendación de cuidado o mitigación
    And el campo "status" es "Completed"
    And el diagnóstico se persiste en la tabla plant_diagnoses con el AnalyzedAt timestamp

  Scenario: Planta saludable detectada
    Given que me he autenticado como "farmer@oryxen.io" con rol "FARMER"
    And que tengo una fotografía de la planta sin signos visibles de anomalía
    When envío una petición POST a "/api/v1/ai/diagnoses" con la imagen multipart y plantId
    Then la respuesta tiene código HTTP 201 Created
    And el campo "detectedPest" es "None"
    And el campo "recommendation" contiene una recomendación preventiva basada en la telemetría
    And el campo "status" es "Completed"

  Scenario: Diagnóstico con análisis multimodal enriquecido con telemetría IoT
    Given que me he autenticado como "farmer@oryxen.io" con rol "FARMER"
    And que el Sensor Lite reportó soilMoisture=20%, humidity=35%, temperature=30°C
    When envío una petición POST a "/api/v1/ai/diagnoses" con la imagen multipart y plantId
    Then el servicio Gemini Vision recibe el prompt enriquecido con la telemetría IoT
    And el prompt incluye "Soil moisture: 20.0%", "Air humidity: 35.0%", "Temperature: 30.0°C"
    And la respuesta tiene código HTTP 201 Created

  Scenario: Consulta del historial de diagnósticos de una planta
    Given que la planta "11111111-2222-3333-4444-555555555555" tiene 3 diagnósticos previos
    When envío una petición GET a "/api/v1/ai/plants/11111111-2222-3333-4444-555555555555/diagnoses"
    Then la respuesta tiene código HTTP 200 OK
    And el cuerpo de la respuesta contiene 3 diagnósticos
    And los diagnósticos están ordenados por fecha de creación descendente

  Scenario: FARMER intenta diagnosticar una planta que no le pertenece
    Given que me he autenticado como "farmer@oryxen.io" con rol "FARMER"
    And que la planta "99999999-aaaa-bbbb-cccc-dddddddddddd" pertenece a otro usuario
    When envío una petición POST a "/api/v1/ai/diagnoses" con esa plantId
    Then la respuesta tiene código HTTP 404 Not Found
    And el cuerpo de la respuesta contiene un ProblemDetails RFC 7807

  Scenario: Servicio Gemini Vision no disponible (API key no configurada)
    Given que la variable de entorno GeminiVision__ApiKey no está configurada
    When envío una petición POST a "/api/v1/ai/diagnoses" con una imagen válida
    Then la respuesta tiene código HTTP 502 Bad Gateway
    And el cuerpo de la respuesta contiene un ProblemDetails con mensaje "Gemini Vision API key is not configured"
    And el diagnóstico se persiste con status "Failed"

  Scenario: Petición de diagnóstico sin imagen adjunta
    Given que me he autenticado como "farmer@oryxen.io" con rol "FARMER"
    When envío una petición POST a "/api/v1/ai/diagnoses" sin archivo de imagen
    Then la respuesta tiene código HTTP 400 Bad Request
    And el cuerpo de la respuesta indica que se requiere una imagen

  Scenario: Cliente no autenticado intenta consultar diagnósticos
    Given que soy un cliente no autenticado
    When envío una petición GET a "/api/v1/ai/diagnoses/{id}" sin cabecera "Authorization"
    Then la respuesta tiene código HTTP 401 Unauthorized
    And el cuerpo de la respuesta tiene content-type "application/problem+json"
