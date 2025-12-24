using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Services.Strategies
{
    public class NormalProductStrategy : IProductStrategy
    {
        public bool CanHandle(string? type) => type == "NORMAL";

        public void Handle(Product p, AppDbContext ctx, INotificationService ns)
        {
            if (p.Available > 0)
            {
                p.Available -= 1;
                ctx.SaveChanges();
            }
            else
            {
                if (p.LeadTime > 0)
                {
                    ns.SendDelayNotification(p.LeadTime, p.Name);
                    ctx.SaveChanges();
                }
            }
        }
    }
}
