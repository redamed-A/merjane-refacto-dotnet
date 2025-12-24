using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Services
{
    public interface IOrderService
    {
        Task<Order> GetOrder(long orderId);
    }
}
