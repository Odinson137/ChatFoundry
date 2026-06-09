# Stage 1: Build the Blazor WASM client
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

# Copy csproj files for restoring
COPY ["webApps/src/BlazorClient/BlazorClient.csproj", "webApps/src/BlazorClient/"]
COPY ["services/WorkflowService/src/WorkflowValidation/WorkflowValidation.csproj", "services/WorkflowService/src/WorkflowValidation/"]

RUN dotnet restore "webApps/src/BlazorClient/BlazorClient.csproj"

# Copy the entire codebase
COPY . .

# Build and publish Blazor WASM
RUN dotnet publish "webApps/src/BlazorClient/BlazorClient.csproj" -c Release -o /app/publish

# Stage 2: Serve using Nginx
FROM nginx:alpine
WORKDIR /usr/share/nginx/html
COPY --from=build-env /app/publish/wwwroot .
COPY webApps/src/BlazorClient/nginx.conf /etc/nginx/nginx.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
