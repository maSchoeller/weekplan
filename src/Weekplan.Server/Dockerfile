# Server-Image fuer Azure Container Apps. Vom Repo-Wurzelverzeichnis bauen:
#   docker build -f src/Weekplan.Server/Dockerfile -t weekplan-server .
# Genau die SDK-Fassung aus global.json. Das Sammeltag `sdk:10.0` bringt
# 10.0.400, und `rollForward: latestPatch` laesst nur 10.0.1xx zu — der Build
# bricht dann mit "SDK not found" ab. Wird global.json gehoben, muss diese Zeile
# mit.
FROM mcr.microsoft.com/dotnet/sdk:10.0.100 AS build
WORKDIR /src
COPY global.json ./
# Bewusst das ganze src/, nicht die einzelnen Projekte: die frueheren
# Einzelzeilen hatten Anmeldung und Tagebuch nicht mitgenommen, obwohl der
# Server sie referenziert — der Build brach ab, und niemand merkte es, weil
# kein Ausrollen das Image gebaut hat. `dotnet publish` baut ohnehin nur den
# referenzierten Graphen, der Client bleibt also aussen vor. `.dockerignore`
# haelt bin/ und obj/ heraus.
COPY src/ src/
RUN dotnet publish src/Weekplan.Server/Weekplan.Server.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
# Container Apps reicht den Port ueber PORT herein; 8080 ist der Standard.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Weekplan.Server.dll"]
