FROM registry.alza.cz/hub.docker.com/library/node:22-alpine AS frontend-build
WORKDIR /app/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

FROM registry.alza.cz/mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /src
COPY backend/Backend.csproj backend/packages.lock.json ./backend/
RUN dotnet restore backend/Backend.csproj --locked-mode
COPY backend/ ./backend/
RUN dotnet publish backend/Backend.csproj -c Release -o /app/publish --no-restore

FROM registry.alza.cz/mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=backend-build /app/publish ./
COPY --from=frontend-build /app/frontend/dist ./wwwroot/
COPY config.env ./
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Backend.dll"]
