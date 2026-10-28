# ChatRoom services

## Ejecutar con Docker Compose

1. Copiar `.env.example` a `.env` y cambiar `MSSQL_SA_PASSWORD` y `JWT_SIGNING_KEY` por secretos propios y seguros. Generar la clave JWT con al menos 32 bytes aleatorios; no reutilizar la contraseña de SQL Server.
2. Crear y ejecutar las imágenes y contenedores:

   ```powershell
   docker compose up --build
   ```

Compose inicia `Users.API`, `Reply.API`, `Thread.API` y la aplicación `ChatRoom`. Las APIs se conectan a la instancia de SQL Server ya disponible en el host; la aplicación web consume `Users.API` y `Thread.API`.

- Reply.API: `http://localhost:5025`
- Thread.API: `http://localhost:5026`
- Users.API: `http://localhost:5027`
- ChatRoom WebAssembly: `http://localhost:5017`
- OpenAPI/Swagger en modo Development: `/openapi/v1.json` y `/swagger`

La página principal consume la API paginada de `Thread.API`. Cada hilo abre su vista de conversación (`/threads/{id}`), que muestra sus respuestas y permite agregar nuevas. El registro y el inicio de sesión están disponibles desde ChatRoom. El JWT de acceso (15 minutos) se conserva sólo en memoria; al abrir la aplicación, Users.API restaura la sesión mediante una cookie `HttpOnly` con refresh token. El refresh token rota al renovarse, con expiración deslizante de 30 días y máxima de 90 días. Cada dispositivo tiene una sesión independiente; cerrar sesión revoca la sesión de ese dispositivo. Las APIs deben servirse por HTTPS fuera de Development. Los endpoints de escritura requieren autenticación, y sólo el autor puede editar o eliminar sus hilos y respuestas. Si se cambia `CHATROOM_PORT`, Compose configura el origen correspondiente en las políticas CORS.

Los proyectos todavía requieren aplicar las migraciones antes de usar sus endpoints de base de datos. Con Docker Compose activo, configurar la conexión desde PowerShell y aplicar ambas:

```powershell
$env:MSSQL_SA_PASSWORD = "la-contraseña-configurada-en-.env"
$env:ConnectionStrings__DefaultConnection = "Server=localhost,1433;Database=chatReply;User Id=sa;Password=$env:MSSQL_SA_PASSWORD;TrustServerCertificate=True;Encrypt=True"
dotnet ef database update --project Reply.API\Reply.API.csproj --startup-project Reply.API\Reply.API.csproj
dotnet ef database update --project Thread.API\Thread.API.csproj --startup-project Thread.API\Thread.API.csproj
```

Para detener los servicios, ejecutar `docker compose down`. Los datos se conservan en el volumen; `docker compose down -v` también elimina ese volumen y la base de datos.

Users.API usa ASP.NET Core Identity y requiere una base de datos `chatUsers`. Configurar su cadena de conexión y la clave JWT con User Secrets para desarrollo local; la misma clave debe configurarse para las tres APIs:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=chatUsers;User Id=sa;Password=<SQL_PASSWORD>;TrustServerCertificate=True;Encrypt=True" --project Users.API\Users.API.csproj
dotnet user-secrets set "Jwt:SigningKey" "<JWT_SIGNING_KEY_DE_AL_MENOS_32_BYTES>" --project Users.API\Users.API.csproj
dotnet user-secrets set "Jwt:SigningKey" "<JWT_SIGNING_KEY_DE_AL_MENOS_32_BYTES>" --project Reply.API\Reply.API.csproj
dotnet user-secrets set "Jwt:SigningKey" "<JWT_SIGNING_KEY_DE_AL_MENOS_32_BYTES>" --project Thread.API\Thread.API.csproj
dotnet ef database update --project Users.API\Users.API.csproj --startup-project Users.API\Users.API.csproj
```

En Compose, establecer `JWT_SIGNING_KEY` en `.env`; nunca guardar una clave real en el repositorio. La política CORS de Users.API permite credenciales sólo para los orígenes configurados y los endpoints que emiten, renuevan o revocan cookies validan el encabezado `Origin`. Web y APIs deben compartir el mismo sitio para que `SameSite=Lax` funcione; la cookie es host-only, HttpOnly y Secure fuera de Development.
