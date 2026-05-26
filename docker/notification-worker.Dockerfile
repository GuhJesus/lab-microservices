# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/notification-worker/notification-worker.csproj", "src/notification-worker/"]
RUN dotnet restore "src/notification-worker/notification-worker.csproj"
COPY . .
RUN dotnet publish "src/notification-worker/notification-worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "notification-worker.dll"]
