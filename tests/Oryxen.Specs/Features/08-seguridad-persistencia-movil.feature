Feature: Seguridad y Persistencia Segura en Dispositivos Móviles
  Como usuario de la aplicación móvil Oryxen
  Quiero que mis credenciales y tokens de sesión estén protegidos mediante cifrado local
  Y que el sistema renueve automáticamente mi sesión en segundo plano sin interrumpir mi experiencia
  Para mantener la seguridad de mis datos incluso en dispositivos potencialmente comprometidos

  Background:
    Given que la aplicación móvil Oryxen está instalada en un dispositivo Android (API 24+)
    And que el backend .NET 9 de Oryxen está desplegado y operativo
    And que el endpoint "POST /api/v1/auth/refresh" está disponible para rotación de tokens

  Scenario: Renovación transparente de sesión en segundo plano
    Given que el usuario "farmer@oryxen.io" ha iniciado sesión exitosamente en la aplicación móvil
    And que el accessToken JWT almacenado en EncryptedSharedPreferences ha expirado
    And que el refreshToken almacenado en EncryptedSharedPreferences sigue siendo válido
    When la aplicación realiza una petición GET a "/api/v1/telemetry/{plantId}" con el accessToken expirado
    Then el interceptor OkHttp TokenAuthenticator detecta la respuesta HTTP 401 Unauthorized
    And el interceptor bloquea temporalmente las peticiones encoladas
    And el interceptor realiza una llamada síncrona a "POST /api/v1/auth/refresh" con el refreshToken almacenado
    And el backend .NET 9 valida el hash SHA-256 del refreshToken y emite un nuevo par de tokens JWT
    And el interceptor persiste el nuevo accessToken y refreshToken en EncryptedSharedPreferences (AES256_GCM)
    And el interceptor re-intenta la petición original "GET /api/v1/telemetry/{plantId}" con el nuevo accessToken
    And la petición original retorna HTTP 200 OK con los datos de telemetría solicitados
    And el usuario no percibe ninguna interrupción en la visualización del dashboard

  Scenario: Cierre forzado de sesión por token inválido
    Given que el usuario "farmer@oryxen.io" tiene una sesión activa en la aplicación móvil
    And que tanto el accessToken como el refreshToken han expirado o han sido revocados en el backend
    When la aplicación realiza una petición GET a "/api/v1/ai/plants/{plantId}/diagnoses" con el accessToken expirado
    Then el interceptor OkHttp TokenAuthenticator detecta la respuesta HTTP 401 Unauthorized
    And el interceptor intenta renovar llamando a "POST /api/v1/auth/refresh"
    And el backend responde con HTTP 401 Unauthorized porque el refreshToken es inválido
    And el interceptor limpia completamente el EncryptedSharedPreferences (borra accessToken, refreshToken, userId, roles)
    And el interceptor emite el evento "SessionEvent.Expired" a través del SessionEventBus
    And la capa de navegación Compose (OryxenNavHost) recibe el evento SessionEvent.Expired
    And la aplicación redirige automáticamente al usuario a la pantalla de LoginScreen
    And el dashboard y todas las pantallas protegidas quedan inaccesibles hasta un nuevo inicio de sesión

  Scenario: Almacenamiento cifrado de tokens y datos de sesión
    Given que el usuario "admin@oryxen.io" inicia sesión en la aplicación móvil
    When el AuthRepository persiste los datos de sesión a través de SecureStorage
    Then el accessToken se almacena cifrado con AES256-GCM en EncryptedSharedPreferences
    And el refreshToken se almacena cifrado con AES256-GCM en EncryptedSharedPreferences
    And el userId (extraído del claim "sub" del JWT) se almacena cifrado en EncryptedSharedPreferences
    And los roles del usuario ("ADMIN") se almacenan cifrados en EncryptedSharedPreferences
    And ningún token ni credencial se almacena en SharedPreferences plano o en memoria sin cifrar
    And la clave maestra (MasterKey) reside exclusivamente en el Android Keystore respaldado por hardware (TEE/StrongBox)

  Scenario: Concurrencia segura durante la renovación de token
    Given que el TokenAuthenticator está procesando una renovación de token activa
    When múltiples peticiones concurrentes reciben HTTP 401 y activan el Authenticator
    Then solo la primera petición ejecuta la llamada a "POST /api/v1/auth/refresh"
    And las peticiones subsecuentes esperan bloqueadas por el bloque synchronized
    And una vez completada la renovación, todas las peticiones encoladas se re-intentan con el nuevo accessToken
    And no se generan múltiples llamadas concurrentes al endpoint de refresh
