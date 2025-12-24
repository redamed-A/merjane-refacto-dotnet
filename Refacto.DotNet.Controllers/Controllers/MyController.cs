using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Dtos.Product;
using Refacto.DotNet.Controllers.Services.Impl;

namespace Refacto.DotNet.Controllers.Controllers
{
    [ApiController]
    [Route("orders")]
    public class OrdersController : ControllerBase
    {
        private readonly ProductService _ps;
        private readonly AppDbContext _ctx;

        public OrdersController(ProductService ps, AppDbContext ctx)
        {
            _ps = ps;
            _ctx = ctx;
        }

        [HttpPost("{orderId}/processOrder")]
        [ProducesResponseType(200)]
        public ActionResult<ProcessOrderResponse> ProcessOrder(long orderId)
        {
            Entities.Order? order = _ctx.Orders
               .Include(o => o.Items)
               .SingleOrDefault(o => o.Id == orderId);
            Console.WriteLine(order);
            List<long> ids = new() { orderId };
            ICollection<Entities.Product>? products = order.Items;

            foreach (Entities.Product p in products)
            {
                _ps.ProcessProduct(p);
            }

            return new ProcessOrderResponse(order.Id);
        }
    }
}
