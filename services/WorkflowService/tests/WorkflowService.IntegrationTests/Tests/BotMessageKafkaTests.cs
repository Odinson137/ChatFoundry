using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Events;
using Shared.Domain.Enums;
using WorkflowService.Data;
using WorkflowService.Entities;
using WorkflowService.IntegrationTests.Services;

namespace WorkflowService.IntegrationTests.Tests
{
    [TestFixture]
    public class BotMessageKafkaTests
    {
        private WorkflowServiceFactory _factory;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _factory = new WorkflowServiceFactory();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _factory?.Dispose();
        }

        [Test]
        public async Task WorkflowService_Should_Start_Workflow_And_Process_Ask()
        {
            using var scope = _factory.Services.CreateScope();
            var producer = scope.ServiceProvider.GetRequiredService<ITopicProducer<BotIncomingMessage>>();
            var db = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
            // ===== Arrange =====

            var botId = Guid.NewGuid();

            var workflow = new Workflow
            {
                Id = Guid.NewGuid(),
                BotId = botId,
                SchemaJson = TestWorkflowSchemas.TestWorkflowJson,
                Version = 1,
                IsActiveBotWorkflow = true,
            };

            var bot = new Bot
            {
                Id = botId,
                Name = "Test bot"
            };

            db.Bots.Add(bot);
            db.Workflows.Add(workflow);
            await db.SaveChangesAsync();

            // ===== Act =====

            var incoming = new BotIncomingMessage(
                botId,
                ExternalUserId: "test-chat",
                Channel: DefaultChannel.Telegram,
                Payload: "hello",
                MessageExternalId: "1",
                new Dictionary<MessageParameter, string>
                {
                    [MessageParameter.FirstName] = "Yuri",
                    [MessageParameter.UserName] = "Yuri123",
                });

            await producer.Produce(incoming);

            for (var i = 0; i < 1000; i++)
                await Task.Delay(TimeSpan.FromSeconds(1));

            var ackIncoming = new BotIncomingMessage(
                botId,
                ExternalUserId: "test-chat",
                Channel: DefaultChannel.Telegram,
                Payload: "order",
                MessageExternalId: "2",
                new Dictionary<MessageParameter, string>
                {
                    [MessageParameter.FirstName] = "Yuri",
                    [MessageParameter.UserName] = "Yuri123",
                });
            
            await producer.Produce(ackIncoming);
            
            await Task.Delay(TimeSpan.FromSeconds(1000));
            Assert.Pass("Message consumed and workflow executed");
        }

    }
}
