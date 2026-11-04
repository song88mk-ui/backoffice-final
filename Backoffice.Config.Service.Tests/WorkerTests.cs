using Backoffice.Config.Service.Repository;
using Backoffice.Shared.Data.Backoffice.Entities;
using Backoffice.Shared.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Backoffice.Config.Service.Tests
{
    // BO-138: the worker logged every failure and returned, so the message broker acked it and the message was lost.
    // A failure now throws on the first delivery (the broker nacks and requeues it) and is reported and acked on the retry.
    public class WorkerTests
    {
        private static async Task<(Mock<IMessageBroker> Broker, Mock<IBackofficeRepository> Repository)> StartWorker()
        {
            var broker = new Mock<IMessageBroker>();
            var repository = new Mock<IBackofficeRepository>();
            var worker = new Worker(NullLogger<Worker>.Instance, broker.Object, repository.Object);

            await worker.StartAsync(CancellationToken.None);
            await worker.ExecuteTask; // ExecuteAsync runs in the background, so wait until it has subscribed to the broker

            return (broker, repository);
        }

        private static Exception Deliver(Mock<IMessageBroker> broker, string messageType, string payload, bool redelivered)
        {
            var properties = new Mock<IBasicProperties>();
            properties.SetupGet(p => p.Type).Returns(messageType);

            var args = new BasicDeliverEventArgs
            {
                Body = Encoding.UTF8.GetBytes(payload),
                BasicProperties = properties.Object,
                Redelivered = redelivered
            };

            var exception = Record.Exception(() => broker.Raise(b => b.MessageReceived += null, broker.Object, args));

            return exception is TargetInvocationException { InnerException: not null } wrapped ? wrapped.InnerException : exception;
        }

        [Fact]
        public async Task A_failed_message_is_thrown_back_to_the_broker_on_the_first_delivery()
        {
            var (broker, repository) = await StartWorker();
            repository.Setup(r => r.DeleteConfigTemplate(It.IsAny<ConfigTemplate>())).Throws(new InvalidOperationException("database down"));

            var exception = Deliver(broker, "DeleteConfigTemplate", "{\"Id\":10}", redelivered: false);

            Assert.IsType<InvalidOperationException>(exception);
        }

        [Fact]
        public async Task A_message_that_fails_again_on_the_retry_is_reported_and_not_thrown()
        {
            var (broker, repository) = await StartWorker();
            repository.Setup(r => r.UpdateClientMargin(It.IsAny<long>(), It.IsAny<IMessageBroker>())).Throws(new InvalidOperationException("database down"));

            var exception = Deliver(broker, "UpdateClientMargin", "20", redelivered: true);

            Assert.Null(exception);
            repository.Verify(r => r.UpdateClientMargin(20, broker.Object), Times.Once);
        }

        [Fact]
        public async Task A_successful_message_is_handled_without_an_exception()
        {
            var (broker, repository) = await StartWorker();

            var exception = Deliver(broker, "DeleteConfigTemplate", "{\"Id\":10}", redelivered: false);

            Assert.Null(exception);
            repository.Verify(r => r.DeleteConfigTemplate(It.Is<ConfigTemplate>(c => c.Id == 10)), Times.Once);
        }
    }
}
