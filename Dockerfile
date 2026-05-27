FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY backend/src/Prodea.Api/Prodea.Api.csproj Prodea.Api/
RUN dotnet restore Prodea.Api/Prodea.Api.csproj

COPY backend/src/Prodea.Api/ Prodea.Api/
WORKDIR /src/Prodea.Api
RUN dotnet publish Prodea.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
EXPOSE 8080

ENTRYPOINT ["dotnet", "Prodea.Api.dll"]
