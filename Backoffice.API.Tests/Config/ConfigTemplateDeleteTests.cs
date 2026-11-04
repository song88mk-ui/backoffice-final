using Backoffice.API.Models;
using Backoffice.API.Repositories;
using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Data.Backoffice.Entities;
using Backoffice.Shared.Data.Translation;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Backoffice.API.Tests.Config
{
    // BO-138: deleting a template that no longer exists (e.g. a double click) threw a NullReferenceException, shown as
    // "Object reference not set to an instance of an object", and the delete message was published before the save.
    public class ConfigTemplateDeleteTests
    {
        private static readonly RequesterInfo Requester = new() { UserId = "1", UserName = "tester", Page = "Config" };

        private static (BackofficeRepository Sut, BackofficeContext Context, Mock<IRabbitMqRepository> RabbitMq) Build()
        {
            var name = Guid.NewGuid().ToString();
            var context = new BackofficeContext(new DbContextOptionsBuilder<BackofficeContext>().UseInMemoryDatabase(name).Options);
            var translation = new TranslationContext(new DbContextOptionsBuilder<TranslationContext>().UseInMemoryDatabase(name).Options);
            var rabbitMq = new Mock<IRabbitMqRepository>();
            rabbitMq.Setup(r => r.Publish(It.IsAny<MessageRequest>(), It.IsAny<string>()))
                .ReturnsAsync(new BaseResponse<MessageResponse> { StatusCode = StatusCode.Success });

            var sut = new BackofficeRepository(
                context,
                translation,
                new Mock<IPlatformRepository>().Object,
                new Mock<ISportsbookRepository>().Object,
                new Mock<ITradingRepository>().Object,
                new Mock<IMemCache>().Object,
                rabbitMq.Object);

            return (sut, context, rabbitMq);
        }

        [Fact]
        public async Task Deleting_a_missing_template_returns_a_clear_error_and_publishes_nothing()
        {
            var (sut, _, rabbitMq) = Build();

            var response = await sut.DeleteConfigTemplate("99", Requester);

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Contains("not found", response.Message);
            rabbitMq.Verify(r => r.Publish(It.IsAny<MessageRequest>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task The_delete_message_is_published_after_the_template_is_deleted()
        {
            var (sut, context, rabbitMq) = Build();
            context.ConfigTemplate.Add(new ConfigTemplate { Id = 7, Name = "Soccer PM", SportId = 1, FixtureStatusId = 0, Timestamp = DateTime.UtcNow });
            await context.SaveChangesAsync();

            bool? templateStoredAtPublish = null;
            rabbitMq.Setup(r => r.Publish(It.Is<MessageRequest>(m => m.MessageType == "DeleteConfigTemplate"), It.IsAny<string>()))
                .Callback(() => templateStoredAtPublish = context.ConfigTemplate.AsNoTracking().Any(t => t.Id == 7))
                .ReturnsAsync(new BaseResponse<MessageResponse> { StatusCode = StatusCode.Success });

            await sut.DeleteConfigTemplate("7", Requester);

            Assert.False(templateStoredAtPublish);
            rabbitMq.Verify(r => r.Publish(It.Is<MessageRequest>(m => m.RoutingKey == "r.bo.config.deleteconfigtemplate"), It.IsAny<string>()), Times.Once);
        }
    }
}
