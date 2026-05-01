# CLAUDE.md

## Stack
.NET 10.0, PostgreSQL 16, EF Core 10 (Npgsql), Kafka (MassTransit Rider), HotChocolate 15

## Services
Gateway (5000), WorkflowService (5010 HTTP / 5011 gRPC), ClientService (5030), IdentityServer (5020), TelegramService (5040)

## Communication
- Client -> Service: GraphQL via Gateway
- Service -> Service: gRPC
- Async: Kafka — topics: `bot.message.incoming/outgoing`, `workflow.action.execute/completed`, `telegram.set-webhook/send-message`

## Workflow Engine
BotMessageConsumer -> SessionResolver -> ExecuteActionConsumer -> IActionExecutor -> ActionCompletedConsumer
- Executors: `WorkflowService/Actions/Executors/`, implement `IActionExecutor`
- New node type: enum in `WorkflowNodeType.cs` + executor + DI registration

## Key Conventions
- JWT authority: `http://identity-service:8080` (hard-coded)
- Schema auto-applied on startup, no migration files
- GraphQL mutations: `[ExtendObjectType(typeof(Mutation))]`, register via `AddTypeExtension<>`
- Entity configs: `Data/Configurations/` using `IEntityTypeConfiguration<T>`
- Secrets: `deployment/.env`
