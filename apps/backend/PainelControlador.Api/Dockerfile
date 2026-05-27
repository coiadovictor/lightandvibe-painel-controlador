# =============================================================================
# Build stage
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restaurar primeiro só os csproj para aproveitar cache de layers
COPY PainelControlador.sln .
COPY PainelControlador.Api/PainelControlador.Api.csproj PainelControlador.Api/
COPY PainelControlador.Tests/PainelControlador.Tests.csproj PainelControlador.Tests/
RUN dotnet restore PainelControlador.Api/PainelControlador.Api.csproj

# Copia o restante e publica
COPY . .
RUN dotnet publish PainelControlador.Api/PainelControlador.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# =============================================================================
# Runtime stage
# =============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "PainelControlador.Api.dll"]
