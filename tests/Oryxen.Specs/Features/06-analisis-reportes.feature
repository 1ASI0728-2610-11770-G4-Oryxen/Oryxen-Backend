# language: es

Feature: Análisis de Telemetría y Generación de Reportes Comerciales
  Como agricultor registrado en la plataforma Oryxen
  Quiero visualizar tendencias históricas de salud de mis cultivos y generar reportes exportables
  Para tomar decisiones informadas sobre riego, fertilización y manejo agronómico

  Background:
    Given que el sistema Oryxen está desplegado y operativo
    And que el usuario autenticado posee el rol FARMER
    And que existen plantas registradas con telemetría histórica de al menos 30 días

  Scenario: Consulta del dashboard analítico del fundo
    Given que el agricultor ha iniciado sesión exitosamente
    When envía una petición GET a "/api/v1/analytics/dashboard"
    Then la respuesta tiene código HTTP 200 OK
    And el campo "totalPlants" es mayor que 0
    And el campo "healthyPlants" contiene el conteo de plantas saludables
    And el campo "avgHealthScore" está entre 0 y 100
    And el array "plantSummaries" contiene un resumen por cada planta activa
    And cada resumen incluye "plantId", "plantName", "avgHealthScore", "avgSoilMoisture" y "readingCount"

  Scenario: Consulta de tendencias de salud de una planta específica
    Given que existe una planta con ID "11111111-2222-3333-4444-555555555555"
    When envía una petición GET a "/api/v1/analytics/plants/11111111-2222-3333-4444-555555555555/trends"
    Then la respuesta tiene código HTTP 200 OK
    And el campo "plantName" contiene el nombre de la planta
    And los arrays "daily", "weekly" y "monthly" contienen puntos de tendencia agregados
    And cada punto de tendencia incluye "label", "avgHealthScore", "avgSoilMoisture", "avgTemperature" y "avgHumidity"

  Scenario: Tendencia diaria de salud con datos reales de telemetría
    Given que la planta ha recibido 20 lecturas de telemetría en los últimos 7 días
    When se consultan las tendencias diarias de la planta
    Then el array "daily" contiene hasta 7 puntos de agregación
    And cada punto "avgHealthScore" es el promedio de las lecturas de ese día
    And cada punto "avgSoilMoisture" es el promedio de humedad del suelo de ese día
    And la suma de "readingCount" en todos los puntos diarios es igual a 20

  Scenario: Planta sin telemetría devuelve tendencias vacías
    Given que existe una planta sin lecturas de telemetría registradas
    When se consultan las tendencias de esa planta
    Then la respuesta tiene código HTTP 200 OK
    And los arrays "daily", "weekly" y "monthly" están vacíos

  Scenario: Generación de reporte CSV bajo demanda
    Given que el agricultor selecciona una planta y un rango de fechas de 7 días
    When envía una petición POST a "/api/v1/analytics/reports" con:
      """
      {
        "plantId": "11111111-2222-3333-4444-555555555555",
        "rangeStart": "2026-06-01T00:00:00Z",
        "rangeEnd": "2026-06-08T00:00:00Z",
        "type": "HealthSummary",
        "format": "Csv"
      }
      """
    Then la respuesta tiene código HTTP 201 Created
    And el campo "status" es "Completed"
    And el campo "format" es "Csv"
    And el campo "fileContent" contiene datos en formato CSV con encabezados

  Scenario: Listado paginado de reportes generados
    Given que el agricultor ha generado 3 reportes previamente
    When envía una petición GET a "/api/v1/analytics/reports?page=1&size=10"
    Then la respuesta tiene código HTTP 200 OK
    And el campo "totalCount" es 3
    And el array "items" contiene 3 elementos
    And cada item incluye "id", "plantId", "plantName", "type", "status" y "format"

  Scenario: Acceso no autorizado al dashboard de analíticas
    Given que un usuario no autenticado intenta acceder al dashboard
    When envía una petición GET a "/api/v1/analytics/dashboard" sin token JWT
    Then la respuesta tiene código HTTP 401 Unauthorized

  Scenario: Validación de rango de fechas inválido en generación de reporte
    Given que el agricultor envía una solicitud de reporte con "rangeStart" posterior a "rangeEnd"
    When envía una petición POST a "/api/v1/analytics/reports"
    Then la respuesta tiene código HTTP 400 Bad Request
    And el mensaje de error indica que el rango de fechas es inválido

  Scenario: Renderizado de gráficos de tendencia en la aplicación web
    Given que el dashboard analítico ha cargado exitosamente en el frontend Vue 3
    When el agricultor selecciona una planta del selector de tendencias
    Then el gráfico SVG de Health Score se renderiza con datos reales del endpoint "/api/v1/analytics/plants/{plantId}/trends"
    And el gráfico de humedad muestra la serie temporal de "avgHumidity" diaria
    And el selector de rango temporal permite alternar entre "daily", "weekly" y "monthly"
    And todos los gráficos tienen atributos ARIA "role=img" y "aria-label"

  Scenario: Visualización de tendencias en la aplicación móvil Android
    Given que el agricultor ha iniciado sesión en la app Android nativa
    When navega a la pantalla de "Crop Analytics"
    Then se muestra el dashboard con total de plantas, plantas saludables y críticas
    And al seleccionar una planta se despliegan las tendencias diarias de salud y humedad del suelo
    And los datos provienen del endpoint "/api/v1/analytics/dashboard" y "/api/v1/analytics/plants/{plantId}/trends"

  Scenario: Internacionalización bilingüe del dashboard de analíticas
    Given que el usuario tiene configurado el idioma español en la aplicación web
    When se renderiza la vista de analíticas
    Then todas las etiquetas, títulos y mensajes se muestran en español
    And el selector de rango temporal muestra "Diario", "Semanal" y "Mensual"
    And al cambiar el idioma a inglés las etiquetas cambian a "Daily", "Weekly" y "Monthly"
