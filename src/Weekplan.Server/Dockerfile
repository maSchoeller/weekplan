# Server-Image fuer Azure Container Apps. Vom Repo-Wurzelverzeichnis bauen:
#   docker build -f src/Weekplan.Server/Dockerfile -t weekplan-server .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json ./
COPY src/Weekplan.Core.Rechnen.Contracts/ src/Weekplan.Core.Rechnen.Contracts/
COPY src/Weekplan.Core.Rechnen/ src/Weekplan.Core.Rechnen/
COPY src/Weekplan.Server/ src/Weekplan.Server/
RUN dotnet publish src/Weekplan.Server/Weekplan.Server.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
# Container Apps reicht den Port ueber PORT herein; 8080 ist der Standard.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Weekplan.Server.dll"]
