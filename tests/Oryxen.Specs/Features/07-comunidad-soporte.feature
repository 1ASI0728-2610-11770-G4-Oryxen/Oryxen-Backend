Feature: Comunidad de Soporte Agrícola
  Como agricultor o administrador de la plataforma Oryxen
  Quiero interactuar en un feed comunitario donde pueda publicar consejos, comentar
  publicaciones de otros usuarios y reaccionar con "me gusta"
  Para fortalecer la red de soporte colaborativo agrícola y compartir experiencias de cultivo

  Asimismo, como negocio Oryxen
  Quiero que el acceso a la comunidad esté restringido exclusivamente a suscriptores Premium
  Para que la comunidad funcione como diferenciador de retención en el modelo Freemium
  Y para que las imágenes publicadas sean sanitizadas de metadatos EXIF/GPS
  Protegiendo la privacidad de geolocalización de las fincas agrícolas

  Background:
    Given que el sistema Oryxen está desplegado y operativo
    And que existe un usuario "farmer@oryxen.io" con rol "FARMER"
    And que el usuario "farmer@oryxen.io" tiene una suscripción activa "Premium"
    And que existe un usuario "freemium@oryxen.io" con rol "FARMER"
    And que el usuario "freemium@oryxen.io" tiene una suscripción activa "Freemium"
    And que "farmer@oryxen.io" se ha autenticado exitosamente

  Scenario: Usuario Premium publica un consejo en el feed comunitario
    Given que me he autenticado como "farmer@oryxen.io" con rol "FARMER"
    And mi suscripción actual es "Premium"
    When envío una petición POST a "/api/v1/community/posts"
    And el cuerpo incluye title "Control de humedad en suelos arcillosos"
    And el cuerpo incluye content "La clave está en el drenaje. Recomiendo mezclar con arena gruesa en proporción 3:1 para evitar encharcamiento."
    Then la respuesta tiene código HTTP 201 Created
    And el cuerpo de la respuesta contiene un objeto "PostResponse" con title "Control de humedad en suelos arcillosos"
    And el atributo "authorName" es "farmer@oryxen.io"
    And el atributo "likesCount" es 0
    And el atributo "likedByCurrentUser" es false

  Scenario: Usuario Premium publica con imagen y el sistema sanitiza metadatos EXIF
    Given que me he autenticado como "farmer@oryxen.io" con rol "FARMER"
    And adjunto una imagen JPEG con metadatos EXIF que contienen coordenadas GPS
    When envío una petición POST multipart a "/api/v1/community/posts"
    And el formulario incluye title "Plaga detectada en cultivo de hortalizas"
    And el formulario incluye content "Encontré manchas foliares. Adjunto foto para referencia de la comunidad."
    And el formulario incluye el archivo "plaga.jpg" como campo "image"
    Then la respuesta tiene código HTTP 201 Created
    And el atributo "imageUrl" no es nulo
    And el archivo almacenado en el servidor NO contiene el segmento EXIF APP1 original
    And el archivo almacenado NO contiene coordenadas GPS

  Scenario: Usuario obtiene el feed comunitario cronológico
    Given que existen 5 publicaciones en la comunidad
    And las publicaciones están ordenadas por fecha de creación descendente
    When envío una petición GET a "/api/v1/community/feed?page=1&pageSize=20"
    Then la respuesta tiene código HTTP 200 OK
    And el cuerpo contiene un array de 5 elementos
    And cada elemento incluye los atributos "id", "title", "content", "authorName", "likesCount", "likedByCurrentUser"
    And el primer elemento del array es la publicación más reciente

  Scenario: Usuario Premium añade un comentario a una publicación
    Given que existe una publicación con id "abc-123"
    When envío una petición POST a "/api/v1/community/posts/abc-123/comments"
    And el cuerpo incluye content "Muy buen consejo, lo aplicaré en mi cultivo de tomates"
    Then la respuesta tiene código HTTP 200 OK
    And el atributo "postId" es "abc-123"
    And el atributo "authorName" es "farmer@oryxen.io"
    And el atributo "content" es "Muy buen consejo, lo aplicaré en mi cultivo de tomates"

  Scenario: Usuario Premium alterna "me gusta" en una publicación
    Given que existe una publicación con id "abc-123"
    And la publicación tiene 3 "me gusta"
    When envío una petición POST a "/api/v1/community/posts/abc-123/likes"
    Then la respuesta tiene código HTTP 200 OK
    And el atributo "postId" es "abc-123"
    And el atributo "likesCount" es 4
    And el atributo "likedByCurrentUser" es true

  Scenario: Usuario Premium quita "me gusta" de una publicación previamente likeada
    Given que existe una publicación con id "abc-123"
    And el usuario "farmer@oryxen.io" ya dio "me gusta" a esta publicación
    And la publicación tiene 4 "me gusta"
    When envío una petición POST a "/api/v1/community/posts/abc-123/likes"
    Then la respuesta tiene código HTTP 200 OK
    And el atributo "postId" es "abc-123"
    And el atributo "likesCount" es 3
    And el atributo "likedByCurrentUser" es false

  Scenario: Usuario Freemium no puede acceder al feed comunitario
    Given que me he autenticado como "freemium@oryxen.io" con rol "FARMER"
    And mi suscripción actual es "Freemium"
    When envío una petición GET a "/api/v1/community/feed"
    Then la respuesta tiene código HTTP 403 Forbidden
    And el cuerpo contiene un mensaje indicando que el acceso a la comunidad requiere plan Premium

  Scenario: Usuario no autenticado no puede acceder al feed
    Given que no estoy autenticado
    When envío una petición GET a "/api/v1/community/feed"
    Then la respuesta tiene código HTTP 401 Unauthorized

  Scenario: Administrador puede ver y gestionar el contenido de la comunidad
    Given que existe un usuario "admin@oryxen.io" con rol "ADMIN"
    And "admin@oryxen.io" se ha autenticado exitosamente
    When envío una petición GET a "/api/v1/community/feed"
    Then la respuesta tiene código HTTP 200 OK
    And el cuerpo contiene publicaciones de todos los usuarios
