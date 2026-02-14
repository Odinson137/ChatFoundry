using FileService.Data;
using FileService.GraphQL;
using FileService.GraphQL.Mutations;
using FileService.Grpc;
using FileService.Interfaces;
using FileService.Options;
using FileService.Repositories;
using FileService.Services;
using Shared.Infrastructure.DependencyInjection;
using Shared.Infrastructure.GraphQl;
using HotChocolate.Types;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GcsStorageOptions>(
    builder.Configuration.GetSection(GcsStorageOptions.SectionName));

builder.Services.AddPostgreSql<FileDbContext>(builder.Configuration);

builder.Services.AddScoped<IStorageService, GcsStorageService>();
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<IFileUrlBuilder, FileUrlBuilder>();
builder.Services.AddGrpc();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddTypeExtension<FileMutation>()
    .AddType<UploadType>()
    .AddProjections()
    .AddFiltering()
    .AddSorting()
    .ModifyRequestOptions(o => o.IncludeExceptionDetails = builder.Environment.IsDevelopment());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FileDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapGrpcService<FileGrpcService>();
app.MapControllers();
app.MapGraphQL();
app.MapGet("/", () => "File Service is running");

app.Run();
