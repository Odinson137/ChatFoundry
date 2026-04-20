FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["services/MessengerHubService/src/MessengerHubService/MessengerHubService.csproj", "services/MessengerHubService/src/MessengerHubService/"]
COPY ["shared/src/Shared.Infrastructure/Shared.Infrastructure.csproj", "shared/src/Shared.Infrastructure/"]
COPY ["shared/src/Shared.Application/Shared.Application.csproj", "shared/src/Shared.Application/"]
COPY ["shared/src/Shared.Domain/Shared.Domain.csproj", "shared/src/Shared.Domain/"]
RUN dotnet restore "services/MessengerHubService/src/MessengerHubService/MessengerHubService.csproj"
COPY . .
WORKDIR "/src/services/MessengerHubService/src/MessengerHubService"
RUN dotnet build "./MessengerHubService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MessengerHubService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MessengerHubService.dll"]
