# Atalho para o EasyPanel: caminho raiz do build = apps/backend, Dockerfile = apps/backend/Dockerfile.
# O Dockerfile real fica em PainelControlador.Api/Dockerfile mas precisa do contexto de toda a solução,
# então este wrapper espelha o build a partir daqui.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY PainelControlador.sln .
COPY PainelControlador.Api/PainelControlador.Api.csproj PainelControlador.Api/
COPY PainelControlador.Tests/PainelControlador.Tests.csproj PainelControlador.Tests/
RUN dotnet restore PainelControlador.Api/PainelControlador.Api.csproj

COPY . .
RUN dotnet publish PainelControlador.Api/PainelControlador.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "PainelControlador.Api.dll"]
