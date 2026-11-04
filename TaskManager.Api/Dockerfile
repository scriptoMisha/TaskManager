FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY TaskManager.Api/TaskManager.Api.csproj TaskManager.Api/
RUN dotnet restore TaskManager.Api/TaskManager.Api.csproj

COPY TaskManager.Api/ TaskManager.Api/
RUN dotnet publish TaskManager.Api/TaskManager.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "TaskManager.Api.dll"]
