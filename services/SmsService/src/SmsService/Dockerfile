FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["services/SmsService/src/SmsService/SmsService.csproj", "services/SmsService/src/SmsService/"]
COPY ["shared/src/Shared.Infrastructure/Shared.Infrastructure.csproj", "shared/src/Shared.Infrastructure/"]
COPY ["shared/src/Shared.Application/Shared.Application.csproj", "shared/src/Shared.Application/"]
COPY ["shared/src/Shared.Domain/Shared.Domain.csproj", "shared/src/Shared.Domain/"]
COPY ["shared/src/Shared.Grpc/Shared.Grpc.csproj", "shared/src/Shared.Grpc/"]
RUN dotnet restore "services/SmsService/src/SmsService/SmsService.csproj"
COPY . .
WORKDIR "/src/services/SmsService/src/SmsService"
RUN dotnet build "./SmsService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SmsService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmsService.dll"]
