using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Dtos.Product;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Impl;

namespace Refacto.DotNet.Controllers.Controllers
{
    [ApiController]
    [Route("orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IProductService _productService;
        private readonly AppDbContext _ctx;

        public OrdersController(IOrderService orderService, IProductService productService, AppDbContext ctx)
        {
            _orderService = orderService;
            _productService = productService;
            _ctx = ctx;
        }

        [HttpPost("{orderId}/processOrder")]
        [ProducesResponseType(200)]
        public async Task<ActionResult<ProcessOrderResponse>> ProcessOrder(long orderId)
        {
            try
            {
                var order = await _orderService.GetOrder(orderId);
                if (order.Items != null)
                {
                    foreach (var p in order.Items)
                    {
                        _productService.ProcessProduct(p);
                    }
                }

                return new ProcessOrderResponse(order.Id);
            }
            catch (ArgumentException)
            {
                return NotFound($"Order {orderId} not found");
            }
        }
    }
}
