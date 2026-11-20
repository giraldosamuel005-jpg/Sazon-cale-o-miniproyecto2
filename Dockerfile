# Imagen para publicar la aplicación web (Render u otro servicio con Docker).
# Solo se compila la web y la biblioteca Core: el escritorio (WinForms) es exclusivo de Windows.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Restaurante.Core/Restaurante.Core.csproj src/Restaurante.Core/
COPY src/Restaurante.Web/Restaurante.Web.csproj src/Restaurante.Web/
RUN dotnet restore src/Restaurante.Web/Restaurante.Web.csproj
COPY src/Restaurante.Core/ src/Restaurante.Core/
COPY src/Restaurante.Web/ src/Restaurante.Web/
RUN dotnet publish src/Restaurante.Web/Restaurante.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# Hora de Colombia para fechas y reportes ("ventas de hoy").
ENV TZ=America/Bogota \
    ASPNETCORE_ENVIRONMENT=Production \
    PORT=8080
EXPOSE 8080
# La cadena de PostgreSQL llega por la variable de entorno SAZON_DB (configurada en Render).
ENTRYPOINT ["sh", "-c", "dotnet Restaurante.Web.dll --urls http://0.0.0.0:${PORT}"]
