FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ChatRoom.csproj ./
RUN dotnet restore ChatRoom.csproj
COPY . ./
RUN dotnet publish ChatRoom.csproj --configuration Release --output /app/publish --no-restore

FROM nginx:alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
EXPOSE 80
