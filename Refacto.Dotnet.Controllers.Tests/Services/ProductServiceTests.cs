using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Impl;

namespace Refacto.DotNet.Controllers.Tests.Services
{
    public class ProductServiceTests
    {
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly Mock<AppDbContext> _mockDbContext;
        private readonly Mock<DbSet<Product>> _mockDbSet;
        private readonly ProductService _productService;

        public ProductServiceTests()
        {
            _mockNotificationService = new Mock<INotificationService>();
            _mockDbContext = new Mock<AppDbContext>();
            _mockDbSet = new Mock<DbSet<Product>>();
            _ = _mockDbContext.Setup(x => x.Products).ReturnsDbSet(Array.Empty<Product>());
            _productService = new ProductService(_mockNotificationService.Object, _mockDbContext.Object);
        }

        [Fact]
        public void ProcessProduct_Normal_Available_DecrementsStock()
        {
            // GIVEN
            Product product = new()
            {
                LeadTime = 15,
                Available = 10,
                Type = "NORMAL",
                Name = "RJ45 Cable"
            };

            // WHEN
            _productService.ProcessProduct(product);

            // THEN
            Assert.Equal(9, product.Available);
            _mockDbContext.Verify(ctx => ctx.SaveChanges(), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void ProcessProduct_Normal_NotAvailable_NotifiesDelay()
        {
            // GIVEN
            Product product = new()
            {
                LeadTime = 15,
                Available = 0,
                Type = "NORMAL",
                Name = "RJ45 Cable"
            };

            // WHEN
            _productService.ProcessProduct(product);

            // THEN
            Assert.Equal(0, product.Available);
            Assert.Equal(15, product.LeadTime);
            _mockDbContext.Verify(ctx => ctx.SaveChanges(), Times.Once());
            _mockNotificationService.Verify(service => service.SendDelayNotification(product.LeadTime, product.Name), Times.Once());
        }

        [Fact]
        public void ProcessProduct_Seasonal_InSeason_Available_DecrementsStock()
        {
            // GIVEN
            Product product = new()
            {
                Type = "SEASONAL",
                Available = 10,
                LeadTime = 5,
                Name = "Seasonal Product",
                SeasonStartDate = DateTime.Now.AddDays(-10),
                SeasonEndDate = DateTime.Now.AddDays(10)
            };

            // WHEN
            _productService.ProcessProduct(product);

            // THEN
            Assert.Equal(9, product.Available);
            _mockDbContext.Verify(ctx => ctx.SaveChanges(), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void ProcessProduct_Seasonal_InSeason_NotAvailable_LeadTimeFits_NotifiesDelay()
        {
            // GIVEN
            Product product = new()
            {
                Type = "SEASONAL",
                Available = 0,
                LeadTime = 5,
                Name = "Seasonal Product",
                SeasonStartDate = DateTime.Now.AddDays(-10),
                SeasonEndDate = DateTime.Now.AddDays(10)
            };

            // WHEN
            _productService.ProcessProduct(product);

            // THEN
            Assert.Equal(0, product.Available);
            _mockDbContext.Verify(ctx => ctx.SaveChanges(), Times.Once());
            _mockNotificationService.Verify(service => service.SendDelayNotification(product.LeadTime, product.Name), Times.Once());
        }

        [Fact]
        public void ProcessProduct_Seasonal_InSeason_NotAvailable_LeadTimeExceeds_NotifiesOutOfStock()
        {
            // GIVEN
            Product product = new()
            {
                Type = "SEASONAL",
                Available = 0,
                LeadTime = 15,
                Name = "Seasonal Product",
                SeasonStartDate = DateTime.Now.AddDays(-10),
                SeasonEndDate = DateTime.Now.AddDays(10)
            };

            // WHEN
            _productService.ProcessProduct(product);

            // THEN
            Assert.Equal(0, product.Available);
            _mockDbContext.Verify(ctx => ctx.SaveChanges(), Times.Once());
            _mockNotificationService.Verify(service => service.SendOutOfStockNotification(product.Name), Times.Once());
        }

        [Fact]
        public void ProcessProduct_Seasonal_BeforeSeason_NotifiesOutOfStock()
        {
            // GIVEN
            Product product = new()
            {
                Type = "SEASONAL",
                Available = 10,
                LeadTime = 5,
                Name = "Seasonal Product",
                SeasonStartDate = DateTime.Now.AddDays(10),
                SeasonEndDate = DateTime.Now.AddDays(20)
            };

            // WHEN
            _productService.ProcessProduct(product);

            // THEN
            _mockDbContext.Verify(ctx => ctx.SaveChanges(), Times.Once()); // It saves changes even if just notifying? Let's check strategy. Yes, it does.
            _mockNotificationService.Verify(service => service.SendOutOfStockNotification(product.Name), Times.Once());
        }

        [Fact]
        public void ProcessProduct_Seasonal_AfterSeason_NotifiesOutOfStock()
        {
            // GIVEN
            Product product = new()
            {
                Type = "SEASONAL",
                Available = 10,
                LeadTime = 5,
                Name = "Seasonal Product",
                SeasonStartDate = DateTime.Now.AddDays(-20),
                SeasonEndDate = DateTime.Now.AddDays(-10)
            };

            // WHEN
            _productService.ProcessProduct(product);

            // THEN
            Assert.Equal(0, product.Available); // Strategy sets available to 0
            _mockDbContext.Verify(ctx => ctx.SaveChanges(), Times.Once());
            _mockNotificationService.Verify(service => service.SendOutOfStockNotification(product.Name), Times.Once());
        }

        [Fact]
        public void ProcessProduct_Expirable_NotExpired_Available_DecrementsStock()
        {
            // GIVEN
            Product product = new()
            {
                Type = "EXPIRABLE",
                Available = 10,
                LeadTime = 5,
                Name = "Expirable Product",
                ExpiryDate = DateTime.Now.AddDays(10)
            };

            // WHEN
            _productService.ProcessProduct(product);

            // THEN
            Assert.Equal(9, product.Available);
            _mockDbContext.Verify(ctx => ctx.SaveChanges(), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void ProcessProduct_Expirable_Expired_NotifiesExpiration()
        {
            // GIVEN
            var expiryDate = DateTime.Now.AddDays(-1);
            Product product = new()
            {
                Type = "EXPIRABLE",
                Available = 10,
                LeadTime = 5,
                Name = "Expirable Product",
                ExpiryDate = expiryDate
            };

            // WHEN
            _productService.ProcessProduct(product);

            // THEN
            Assert.Equal(0, product.Available);
            _mockDbContext.Verify(ctx => ctx.SaveChanges(), Times.Once());
            _mockNotificationService.Verify(service => service.SendExpirationNotification(product.Name, expiryDate), Times.Once());
        }

        [Fact]
        public void ProcessProduct_Expirable_NotExpired_NotAvailable_NotifiesExpiration()
        {
            // GIVEN
            var expiryDate = DateTime.Now.AddDays(10);
            Product product = new()
            {
                Type = "EXPIRABLE",
                Available = 0,
                LeadTime = 5,
                Name = "Expirable Product",
                ExpiryDate = expiryDate
            };

            // WHEN
            _productService.ProcessProduct(product);

            // THEN
            Assert.Equal(0, product.Available);
            _mockDbContext.Verify(ctx => ctx.SaveChanges(), Times.Once());
            _mockNotificationService.Verify(service => service.SendExpirationNotification(product.Name, expiryDate), Times.Once());
        }
    }
}
