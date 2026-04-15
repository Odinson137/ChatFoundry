FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["services/BillingService/src/BillingService/BillingService.csproj", "services/BillingService/src/BillingService/"]
COPY ["shared/src/Shared.Infrastructure/Shared.Infrastructure.csproj", "shared/src/Shared.Infrastructure/"]
COPY ["shared/src/Shared.Application/Shared.Application.csproj", "shared/src/Shared.Application/"]
COPY ["shared/src/Shared.Domain/Shared.Domain.csproj", "shared/src/Shared.Domain/"]
COPY ["shared/src/Shared.Grpc/Shared.Grpc.csproj", "shared/src/Shared.Grpc/"]
RUN dotnet restore "services/BillingService/src/BillingService/BillingService.csproj"
COPY . .
WORKDIR "/src/services/BillingService/src/BillingService"
RUN dotnet build "./BillingService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BillingService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BillingService.dll"]
