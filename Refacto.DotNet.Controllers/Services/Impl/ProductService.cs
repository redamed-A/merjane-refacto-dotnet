using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services.Strategies;

namespace Refacto.DotNet.Controllers.Services.Impl
{
    public class ProductService : IProductService
    {
        private readonly INotificationService _ns;
        private readonly AppDbContext _ctx;
        private readonly IEnumerable<IProductStrategy> _strategies;

        public ProductService(INotificationService ns, AppDbContext ctx)
        {
            _ns = ns;
            _ctx = ctx;
            _strategies = new List<IProductStrategy>
            {
                new NormalProductStrategy(),
                new SeasonalProductStrategy(),
                new ExpirableProductStrategy()
            };
        }
        public void ProcessProduct(Product p)
        {
            var strategy = _strategies.FirstOrDefault(s => s.CanHandle(p.Type));
            if (strategy != null)
            {
                strategy.Handle(p, _ctx, _ns);
            }
        }

    }
}
