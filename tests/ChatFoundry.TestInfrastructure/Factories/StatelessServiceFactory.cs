using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ChatFoundry.TestInfrastructure.Containers;
using ChatFoundry.TestInfrastructure.Database;
using ChatFoundry.TestInfrastructure.Auth;
using ChatFoundry.TestInfrastructure.Interfaces;
using Xunit;

namespace ChatFoundry.TestInfrastructure.Factories;

public abstract class StatelessServiceFactory<TProgram>
    : WebApplicationFactory<TProgram>, IAsyncLifetime, ITestFixture
    where TProgram : class
{
    public DatabaseRespawner? Respawner { get; private set; }

    protected virtual bool NeedsPostgres => false;
    protected virtual bool NeedsKafka => false;
    protected virtual bool NeedsRedis => false;

    public async Task InitializeAsync()
    {
        var tasks = new List<Task>();
        if (NeedsPostgres) tasks.Add(PostgresFixture.StartAsync());
        if (NeedsKafka) tasks.Add(KafkaFixture.StartAsync());
        if (NeedsRedis) tasks.Add(RedisFixture.StartAsync());

        if (tasks.Any())
        {
            await Task.WhenAll(tasks);
        }

        if (NeedsPostgres)
        {
            Respawner = new DatabaseRespawner(PostgresFixture.ConnectionString);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("OpenIddict:EncryptionKey", "cGFzc3dvcmQxMjM0NTY3ODkwMTIzNDU2Nzg5MDEyMzQ=");

        if (NeedsPostgres)
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", PostgresFixture.ConnectionString);
        }

        if (NeedsKafka)
        {
            builder.UseSetting("ConnectionStrings:Kafka", KafkaFixture.BootstrapServers);
        }

        if (NeedsRedis)
        {
            builder.UseSetting("CacheSettings:ConnectionString", RedisFixture.ConnectionString);
        }

        builder.ConfigureAppConfiguration((context, config) =>
        {
            var testConfig = new Dictionary<string, string?>();

            if (NeedsPostgres)
            {
                testConfig["ConnectionStrings:DefaultConnection"] = PostgresFixture.ConnectionString;
            }

            if (NeedsKafka)
            {
                testConfig["ConnectionStrings:Kafka"] = KafkaFixture.BootstrapServers;
            }

            if (NeedsRedis)
            {
                testConfig["CacheSettings:ConnectionString"] = RedisFixture.ConnectionString;
            }

            if (testConfig.Any())
            {
                config.AddInMemoryCollection(testConfig);
            }
        });

        builder.ConfigureServices(services =>
        {
            services.AddTransient<TestAuthHandler>();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            });

            services.Configure<AuthenticationSchemeOptions>(JwtBearerDefaults.AuthenticationScheme, options => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                var fields = typeof(AuthenticationOptions).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                foreach (var field in fields)
                {
                    var value = field.GetValue(options);
                    if (value is System.Collections.IDictionary dict)
                    {
                        if (dict.Contains(JwtBearerDefaults.AuthenticationScheme))
                        {
                            dict.Remove(JwtBearerDefaults.AuthenticationScheme);
                        }
                    }
                    else if (value is System.Collections.IList list)
                    {
                        for (var i = 0; i < list.Count; i++)
                        {
                            var item = list[i];
                            if (item != null)
                            {
                                var nameProp = item.GetType().GetProperty("Name");
                                var name = nameProp?.GetValue(item) as string;
                                if (name == JwtBearerDefaults.AuthenticationScheme)
                                {
                                    list.RemoveAt(i);
                                    i--;
                                }
                            }
                        }
                    }
                }
                options.AddScheme<TestAuthHandler>(JwtBearerDefaults.AuthenticationScheme, "Test Auth");
            });

            ConfigureGrpcMocks(services);
        });
    }

    protected abstract void ConfigureGrpcMocks(IServiceCollection services);

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }
}
