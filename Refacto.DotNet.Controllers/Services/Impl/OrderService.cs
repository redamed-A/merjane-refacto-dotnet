using Microsoft.EntityFrameworkCore;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Services.Impl
{
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _ctx;

        public OrderService(AppDbContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<Order> GetOrder(long orderId)
        {
            Order? order = await _ctx.Orders
                .Include(o => o.Items)
                .SingleOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                throw new ArgumentException($"Order with ID {orderId} not found.");
            }

            return order;
        }
    }
}
