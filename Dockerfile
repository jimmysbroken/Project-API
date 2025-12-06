# Imagen base para runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Imagen para build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["WebAPI/Minimalapi.JWT.csproj", "WebAPI/"]
RUN dotnet restore "WebAPI/Minimalapi.JWT.csproj"
COPY WebAPI/ WebAPI/
WORKDIR /src/WebAPI
RUN dotnet build "Minimalapi.JWT.csproj" -c Release -o /app/build

# Publicar la aplicación
FROM build AS publish
RUN dotnet publish "Minimalapi.JWT.csproj" -c Release -o /app/publish

# Imagen final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Minimalapi.JWT.dll"]