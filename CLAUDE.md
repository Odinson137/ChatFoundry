# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Build entire solution
dotnet build ChatFoundry.sln

# Build a single service
dotnet build services/WorkflowService/src/WorkflowService/WorkflowService.csproj

# Run tests (NUnit + Moq)
dotnet test services/WorkflowService/tests/WorkflowService.IntegrationTests/

# Run full stack via Docker Compose
cd deployment && docker-compose up -d

# Rebuild and restart a single service
cd deployment && docker-compose up -d --build workflow-service
```

## Architecture Overview

ChatFoundry is a bot/workflow automation platform built as event-driven microservices on .NET 10.0.

### Services

| Service | Port (host) | Role |
|---------|-------------|------|
| **Gateway** | 5000 (HTTPS) | YARP reverse proxy, JWT validation, route authorization |
| **WorkflowService** | 5010 (HTTP), 5011 (gRPC) | Workflow engine, action execution, GraphQL API |
| **ClientService** | 5030 | Client/team/attribute management, GraphQL API |
| **IdentityServer** | 5020 | OAuth2/OpenID Connect via OpenIddict |
| **TelegramService** | 5040 | Telegram webhook receiver/sender, stateless |
| **BlazorClient** | (served via Gateway) | Blazor WASM frontend with workflow designer |

### Communication Patterns

- **Client-to-Service**: GraphQL through Gateway (HotChocolate 15.x)
- **Service-to-Service**: gRPC (WorkflowService <-> ClientService, TelegramService -> WorkflowService)
- **Async Events**: Kafka via MassTransit Rider. Topics:
  - `bot.message.incoming` / `bot.message.outgoing` - message routing
  - `workflow.action.execute` / `workflow.action.completed` - workflow execution pipeline
  - `telegram.set-webhook` / `telegram.send-message` - Telegram operations

### Workflow Engine (WorkflowService)

The workflow engine processes bot conversations through a node graph. Each node type maps to an `IActionExecutor` implementation registered in DI:

- **Node types** defined in `WorkflowService/Enums/WorkflowNodeType.cs`: Start, Message, Ask, Input, Condition, SetVariable, HttpRequest, AIGenerate, AIFilter, etc.
- **Execution flow**: `BotMessageConsumer` -> `SessionResolver` -> `ExecuteActionConsumer` -> `IActionExecutor` -> `ActionCompletedConsumer` (loops to next node)
- **Action executors** live in `WorkflowService/Actions/Executors/` and implement `IActionExecutor`
- New node types require: enum value + executor class + DI registration in `Program.cs`

### Shared Libraries (`shared/src/`)

- **Shared.Domain**: `EntityBase` base class, domain enums (`ActionStatus`, `MessageDirection`, `SessionStatus`, etc.)
- **Shared.Application**: Kafka event contracts (`BotIncomingMessage`, `BotOutgoingMessage`, `ActionCompletedEvent`, etc.)
- **Shared.Infrastructure**: `AddPostgreSql<T>()` extension for EF Core setup, `BaseGraphQl` base class (extracts `UserId` from JWT claims), `Mutation` base type for GraphQL
- **Shared.Grpc**: Protobuf definitions (`bot_token.proto`, `client.proto`)

### GraphQL Pattern

Each service uses HotChocolate with type extensions:
- Base `Mutation` type in `Shared.Infrastructure.GraphQl`
- Query/mutation classes extend from `BaseGraphQl` to access `UserId`
- Feature mutations added as `[ExtendObjectType(typeof(Mutation))]`
- Registered in `Program.cs` via `.AddTypeExtension<XxxMutation>()`
- Projections, filtering, and sorting enabled globally

### Database

- PostgreSQL 16 per service (separate containers, each named `foundry_db`)
- EF Core 10.0 with Npgsql provider
- Entity configurations in `Data/Configurations/` using `IEntityTypeConfiguration<T>`
- Schema auto-applied on startup (no explicit migration files)

### Configuration

- Connection strings in `appsettings.json` (`DefaultConnection` for Postgres, `Kafka` for Kafka)
- Secrets via `deployment/.env` file (OpenAI key, Telegram webhook URL/secret)
- JWT authority hard-coded to `http://identity-service:8080` in each service's `Program.cs`
- Typed options pattern (e.g., `OpenAiOptions`) bound from `IConfiguration`

### Frontend (BlazorClient)

- Blazor WebAssembly with MudBlazor UI components
- StrawberryShake for generated GraphQL client
- Z.Blazor.Diagrams for the visual workflow editor
