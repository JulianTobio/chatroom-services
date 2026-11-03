# ChatRoom services

## Ejecutar con Docker Compose

1. Copiar `.env.example` a `.env` y cambiar `MSSQL_SA_PASSWORD`, `JWT_SIGNING_KEY` y `CHATBOT_INTERNAL_API_KEY` por secretos propios y seguros. Generar las claves JWT e interna con al menos 32 bytes aleatorios; no reutilizar la contraseña de SQL Server.
2. Crear y ejecutar las imágenes y contenedores:

   ```powershell
   docker compose up --build
   ```

Compose inicia `Users.API`, `Reply.API`, `Thread.API`, `ChatBot.API` y la aplicación `ChatRoom`. Las APIs de datos se conectan a la instancia de SQL Server ya disponible en el host; la aplicación web consume `Users.API` y `Thread.API`.

- Reply.API: `http://localhost:5025`
- Thread.API: `http://localhost:5026`
- Users.API: `http://localhost:5027`
- ChatBot.API: `http://localhost:5028`
- ChatRoom WebAssembly: `http://localhost:5017`
- OpenAPI/Swagger en modo Development: `/openapi/v1.json` y `/swagger`

La página principal consume la API paginada de `Thread.API`. Cada hilo abre su vista de conversación (`/threads/{id}`), que muestra sus respuestas y permite agregar nuevas. Threads.API y Reply.API enriquecen sus respuestas con `authorProfile` o `userProfile` (id, username e imagen) consultando perfiles actuales por lote en Users.API; la interfaz muestra el username actual en threads y replies. El registro y el inicio de sesión están disponibles desde ChatRoom. El JWT de acceso (15 minutos) se conserva sólo en memoria; al abrir la aplicación, Users.API restaura la sesión mediante una cookie `HttpOnly` con refresh token. El refresh token rota al renovarse, con expiración deslizante de 30 días y máxima de 90 días. Cada dispositivo tiene una sesión independiente; cerrar sesión revoca la sesión de ese dispositivo. Las APIs deben servirse por HTTPS fuera de Development. Los endpoints de escritura requieren autenticación, y sólo el autor puede editar o eliminar sus hilos y respuestas. Si se cambia `CHATROOM_PORT`, Compose configura el origen correspondiente en las políticas CORS.

Users.API expone `POST /api/users/profiles`, que recibe `{ "userIds": [1, 2] }` y devuelve únicamente `id`, `username` e `image` para los perfiles existentes (máximo 100 por solicitud). La ruta es pública porque sólo expone información pública de perfil.

ChatBot.API expone `POST /api/chat` y requiere un JWT de Users.API. Cada petición se procesa sin historial ni contexto conversacional. Thread.API utiliza el endpoint interno `POST /api/chat/internal`, protegido con `CHATBOT_INTERNAL_API_KEY`.

ChatBot.API usa Microsoft.Extensions.AI (`IChatClient`) con OllamaSharp como proveedor. Instalar y arrancar Ollama en el host, descargar el modelo configurado (por defecto `llama3.2:3b`) con `ollama pull llama3.2:3b` y ejecutar `docker compose up --build`. En Compose, la API se conecta por defecto a `http://host.docker.internal:11434`; se puede cambiar con `OLLAMA_BASE_ADDRESS` y `OLLAMA_MODEL`. Para ejecutarla localmente fuera de Docker, la dirección por defecto es `http://localhost:11434`. El endpoint devuelve HTTP 503 si Ollama no está disponible.

Para asignar respuestas automáticas a una cuenta dedicada, registrar primero desde ChatRoom una cuenta para el bot (por ejemplo, `chatbot`) y configurar en `.env` el ID entero devuelto por Users.API como `CHATBOT_USER_ID`. Reply.API asigna ese ID en el servidor al guardar una respuesta interna del bot; el cliente no puede elegirlo. `CHATBOT_INTERNAL_API_KEY` se comparte sólo entre Thread.API, Reply.API y ChatBot.API, y protege las llamadas internas.

Al crear una reply cuyo cuerpo menciona `@chatbot` como palabra completa y contiene texto adicional, Thread.API guarda y devuelve la reply original sin esperar al modelo. Luego, su cola en memoria procesa el texto sin la mención, solicita una respuesta independiente a ChatBot.API y publica una segunda reply atribuida al usuario `CHATBOT_USER_ID`. Al guardarse la respuesta del bot, Thread.API emite el evento SignalR existente y ChatRoom recarga la conversación automáticamente. La cola tiene capacidad máxima de 1000 trabajos, no es durable y no reintenta: un reinicio o una cola llena puede dejar una mención sin respuesta, y los fallos se registran en el log.

Thread.API publica notificaciones SignalR en `/hubs/threads` cuando se crea, edita o elimina un hilo o una respuesta mediante sus endpoints. ChatRoom se suscribe a la lista general o al hilo abierto y, al recibir `ThreadChanged`, vuelve a consultar los datos por REST. Tras conectar, SignalR reintenta automáticamente y ChatRoom vuelve a cargar los datos al reconectarse; los eventos no se almacenan, así que los cambios durante una desconexión se recuperan al reconectar. SignalR sólo transporta el identificador del hilo y el tipo de cambio. La configuración actual supone una instancia de Thread.API; para escalar a varias instancias se deberá agregar un backplane de SignalR.

Ejemplo de solicitud autenticada a ChatBot.API:

```json
{
  "message": "¿Qué opinas sobre esta propuesta?"
}
```

Los proyectos todavía requieren aplicar las migraciones antes de usar sus endpoints de base de datos. Con Docker Compose activo, configurar la conexión desde PowerShell y aplicar ambas:

```powershell
$env:MSSQL_SA_PASSWORD = "la-contraseña-configurada-en-.env"
$env:ConnectionStrings__DefaultConnection = "Server=localhost,1433;Database=chatReply;User Id=sa;Password=$env:MSSQL_SA_PASSWORD;TrustServerCertificate=True;Encrypt=True"
dotnet ef database update --project Reply.API\Reply.API.csproj --startup-project Reply.API\Reply.API.csproj
dotnet ef database update --project Thread.API\Thread.API.csproj --startup-project Thread.API\Thread.API.csproj
```

Para detener los servicios, ejecutar `docker compose down`. Los datos se conservan en el volumen; `docker compose down -v` también elimina ese volumen y la base de datos.

Users.API usa ASP.NET Core Identity y requiere una base de datos `chatUsers`. Configurar su cadena de conexión y la clave JWT con User Secrets para desarrollo local; la misma clave debe configurarse en todas las APIs que validan JWT:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=chatUsers;User Id=sa;Password=<SQL_PASSWORD>;TrustServerCertificate=True;Encrypt=True" --project Users.API\Users.API.csproj
dotnet user-secrets set "Jwt:SigningKey" "<JWT_SIGNING_KEY_DE_AL_MENOS_32_BYTES>" --project Users.API\Users.API.csproj
dotnet user-secrets set "Jwt:SigningKey" "<JWT_SIGNING_KEY_DE_AL_MENOS_32_BYTES>" --project Reply.API\Reply.API.csproj
dotnet user-secrets set "Jwt:SigningKey" "<JWT_SIGNING_KEY_DE_AL_MENOS_32_BYTES>" --project Thread.API\Thread.API.csproj
dotnet user-secrets set "Jwt:SigningKey" "<JWT_SIGNING_KEY_DE_AL_MENOS_32_BYTES>" --project ChatBot.API\ChatBot.API.csproj
dotnet user-secrets set "ChatBotApi:InternalApiKey" "<CHATBOT_INTERNAL_API_KEY_DE_AL_MENOS_32_BYTES>" --project Thread.API\Thread.API.csproj
dotnet user-secrets set "ChatBot:InternalApiKey" "<CHATBOT_INTERNAL_API_KEY_DE_AL_MENOS_32_BYTES>" --project Reply.API\Reply.API.csproj
dotnet user-secrets set "ChatBot:InternalApiKey" "<CHATBOT_INTERNAL_API_KEY_DE_AL_MENOS_32_BYTES>" --project ChatBot.API\ChatBot.API.csproj
dotnet user-secrets set "ChatBot:UserId" "<ID_NUMERICO_DE_LA_CUENTA_CHATBOT>" --project Reply.API\Reply.API.csproj
dotnet ef database update --project Users.API\Users.API.csproj --startup-project Users.API\Users.API.csproj
```

El valor de la clave interna debe ser el mismo en Reply.API, Thread.API y ChatBot.API. Para Compose, crear primero desde ChatRoom una cuenta dedicada (por ejemplo, username `chatbot`) y copiar su ID numérico a `CHATBOT_USER_ID` en `.env`; luego establecer `CHATBOT_INTERNAL_API_KEY` con un secreto aleatorio de al menos 32 bytes. Nunca guardar una clave real en el repositorio. La política CORS de Users.API permite credenciales sólo para los orígenes configurados y los endpoints que emiten, renuevan o revocan cookies validan el encabezado `Origin`. Web y APIs deben compartir el mismo sitio para que `SameSite=Lax` funcione; la cookie es host-only, HttpOnly y Secure fuera de Development.
