# ChatRoom services

## Ejecutar con Docker Compose

1. Copiar `.env.example` a `.env` y cambiar `MSSQL_SA_PASSWORD` por una contraseña segura.
2. Crear y ejecutar las imágenes y contenedores:

   ```powershell
   docker compose up --build
   ```

Compose inicia SQL Server, `Reply.API` y `Thread.API`. La base persistente se guarda en el volumen `sqlserver-data`.

- Reply.API: `http://localhost:5025`
- Thread.API: `http://localhost:5026`
- OpenAPI/Swagger en modo Development: `/openapi/v1.json` y `/swagger`

Los proyectos todavía requieren aplicar las migraciones antes de usar sus endpoints de base de datos. Con Docker Compose activo, configurar la conexión desde PowerShell y aplicar ambas:

```powershell
$env:MSSQL_SA_PASSWORD = "la-contraseña-configurada-en-.env"
$env:ConnectionStrings__DefaultConnection = "Server=localhost,1433;Database=chatReply;User Id=sa;Password=$env:MSSQL_SA_PASSWORD;TrustServerCertificate=True;Encrypt=True"
dotnet ef database update --project Reply.API\Reply.API.csproj --startup-project Reply.API\Reply.API.csproj
dotnet ef database update --project Thread.API\Thread.API.csproj --startup-project Thread.API\Thread.API.csproj
```

Para detener los servicios, ejecutar `docker compose down`. Los datos se conservan en el volumen; `docker compose down -v` también elimina ese volumen y la base de datos.
