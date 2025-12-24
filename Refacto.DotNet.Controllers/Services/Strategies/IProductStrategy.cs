using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Services.Strategies
{
    public interface IProductStrategy
    {
        bool CanHandle(string? type);
        void Handle(Product product, AppDbContext ctx, INotificationService ns);
    }
}
