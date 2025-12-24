using Moq;
using Moq.EntityFrameworkCore;
using Refacto.DotNet.Controllers.Controllers;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Impl;

namespace Refacto.DotNet.Controllers.Tests.Controllers
{
    public class OrdersControllerTests
    {
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly Mock<AppDbContext> _mockDbContext;
        private readonly IProductService _productService;
        private readonly IOrderService _orderService;
        private readonly OrdersController _controller;

        public OrdersControllerTests()
        {
            _mockNotificationService = new Mock<INotificationService>();
            _mockDbContext = new Mock<AppDbContext>();

            _mockDbContext.Setup(x => x.Orders).ReturnsDbSet(new List<Order>());
            _mockDbContext.Setup(x => x.Products).ReturnsDbSet(new List<Product>());

            _productService = new ProductService(_mockNotificationService.Object, _mockDbContext.Object);
            _orderService = new OrderService(_mockDbContext.Object);
            _controller = new OrdersController(_orderService, _productService, _mockDbContext.Object);
        }

        private void SetupOrder(long orderId, Product product)
        {
            var order = new Order
            {
                Id = orderId,
                Items = new List<Product> { product }
            };
            _mockDbContext.Setup(x => x.Orders).ReturnsDbSet(new List<Order> { order });
        }

        [Fact]
        public async Task ProcessOrder_NormalProduct_Available_DecrementsStock()
        {
            var product = new Product { Id = 1, Type = "NORMAL", Available = 10, LeadTime = 5, Name = "P1" };
            SetupOrder(1, product);

            await _controller.ProcessOrder(1);

            Assert.Equal(9, product.Available);
            _mockDbContext.Verify(x => x.SaveChanges(), Times.Once);
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ProcessOrder_NormalProduct_NotAvailable_NotifiesDelay()
        {
            var product = new Product { Id = 1, Type = "NORMAL", Available = 0, LeadTime = 5, Name = "P1" };
            SetupOrder(1, product);

            await _controller.ProcessOrder(1);

            Assert.Equal(0, product.Available);
            _mockDbContext.Verify(x => x.SaveChanges(), Times.Once);
            _mockNotificationService.Verify(x => x.SendDelayNotification(5, "P1"), Times.Once);
        }

        [Fact]
        public async Task ProcessOrder_SeasonalProduct_InSeason_Available_DecrementsStock()
        {
            var product = new Product
            {
                Id = 1,
                Type = "SEASONAL",
                Available = 10,
                LeadTime = 5,
                Name = "P1",
                SeasonStartDate = DateTime.Now.AddDays(-10),
                SeasonEndDate = DateTime.Now.AddDays(10)
            };
            SetupOrder(1, product);

            await _controller.ProcessOrder(1);

            Assert.Equal(9, product.Available);
            _mockDbContext.Verify(x => x.SaveChanges(), Times.Once);
        }

        [Fact]
        public async Task ProcessOrder_SeasonalProduct_BeforeSeason_NotifiesOutOfStock()
        {
            var product = new Product
            {
                Id = 1,
                Type = "SEASONAL",
                Available = 10,
                LeadTime = 5,
                Name = "P1",
                SeasonStartDate = DateTime.Now.AddDays(10),
                SeasonEndDate = DateTime.Now.AddDays(20)
            };
            SetupOrder(1, product);

            await _controller.ProcessOrder(1);

            _mockNotificationService.Verify(x => x.SendOutOfStockNotification("P1"), Times.Once);
        }

        [Fact]
        public async Task ProcessOrder_SeasonalProduct_AfterSeason_NotifiesOutOfStock()
        {
            var product = new Product
            {
                Id = 1,
                Type = "SEASONAL",
                Available = 10,
                LeadTime = 5,
                Name = "P1",
                SeasonStartDate = DateTime.Now.AddDays(-20),
                SeasonEndDate = DateTime.Now.AddDays(-10)
            };
            SetupOrder(1, product);

            await _controller.ProcessOrder(1);

            _mockNotificationService.Verify(x => x.SendOutOfStockNotification("P1"), Times.Once);
            Assert.Equal(0, product.Available);
        }

        [Fact]
        public async Task ProcessOrder_SeasonalProduct_InSeason_NotAvailable_LeadTimeWithinSeason_NotifiesDelay()
        {
            var product = new Product
            {
                Id = 1,
                Type = "SEASONAL",
                Available = 0,
                LeadTime = 5,
                Name = "P1",
                SeasonStartDate = DateTime.Now.AddDays(-10),
                SeasonEndDate = DateTime.Now.AddDays(10)
            };
            SetupOrder(1, product);

            await _controller.ProcessOrder(1);

            _mockNotificationService.Verify(x => x.SendDelayNotification(5, "P1"), Times.Once);
        }

        [Fact]
        public async Task ProcessOrder_SeasonalProduct_InSeason_NotAvailable_LeadTimeExceedsSeason_NotifiesOutOfStock()
        {
            var product = new Product
            {
                Id = 1,
                Type = "SEASONAL",
                Available = 0,
                LeadTime = 15,
                Name = "P1",
                SeasonStartDate = DateTime.Now.AddDays(-10),
                SeasonEndDate = DateTime.Now.AddDays(10)
            };
            SetupOrder(1, product);

            await _controller.ProcessOrder(1);

            _mockNotificationService.Verify(x => x.SendOutOfStockNotification("P1"), Times.Once);
            Assert.Equal(0, product.Available);
        }

        [Fact]
        public async Task ProcessOrder_ExpirableProduct_NotExpired_Available_DecrementsStock()
        {
            var product = new Product
            {
                Id = 1,
                Type = "EXPIRABLE",
                Available = 10,
                LeadTime = 5,
                Name = "P1",
                ExpiryDate = DateTime.Now.AddDays(10)
            };
            SetupOrder(1, product);

            await _controller.ProcessOrder(1);

            Assert.Equal(9, product.Available);
            _mockDbContext.Verify(x => x.SaveChanges(), Times.Once);
        }

        [Fact]
        public async Task ProcessOrder_ExpirableProduct_Expired_NotifiesExpiration()
        {
            var expiryDate = DateTime.Now.AddDays(-1);
            var product = new Product
            {
                Id = 1,
                Type = "EXPIRABLE",
                Available = 10,
                LeadTime = 5,
                Name = "P1",
                ExpiryDate = expiryDate
            };
            SetupOrder(1, product);

            await _controller.ProcessOrder(1);

            _mockNotificationService.Verify(x => x.SendExpirationNotification("P1", expiryDate), Times.Once);
            Assert.Equal(0, product.Available);
        }

        [Fact]
        public async Task ProcessOrder_ExpirableProduct_NotExpired_NotAvailable_NotifiesExpiration()
        {
            var expiryDate = DateTime.Now.AddDays(10);
            var product = new Product
            {
                Id = 1,
                Type = "EXPIRABLE",
                Available = 0,
                LeadTime = 5,
                Name = "P1",
                ExpiryDate = expiryDate
            };
            SetupOrder(1, product);

            await _controller.ProcessOrder(1);

            _mockNotificationService.Verify(x => x.SendExpirationNotification("P1", expiryDate), Times.Once);
            Assert.Equal(0, product.Available);
        }
    }
}
