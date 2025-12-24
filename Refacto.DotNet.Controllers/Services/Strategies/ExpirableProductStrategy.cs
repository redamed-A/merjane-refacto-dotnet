using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Services.Strategies
{
    public class ExpirableProductStrategy : IProductStrategy
    {
        public bool CanHandle(string? type) => type == "EXPIRABLE";

        public void Handle(Product p, AppDbContext ctx, INotificationService ns)
        {
            if (p.Available > 0 && p.ExpiryDate > DateTime.Now.Date)
            {
                p.Available -= 1;
                ctx.SaveChanges();
            }
            else
            {
                ns.SendExpirationNotification(p.Name, (DateTime)p.ExpiryDate);
                p.Available = 0;
                ctx.SaveChanges();
            }
        }
    }
}
