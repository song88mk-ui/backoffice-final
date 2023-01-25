
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using System.Threading.Tasks;

namespace Backoffice.API.Repositories
{
    public interface IRabbitMQRepository
    {
        Task<BaseResponse<MessageResponse>> Publish(MessageRequest request);
    }
}
