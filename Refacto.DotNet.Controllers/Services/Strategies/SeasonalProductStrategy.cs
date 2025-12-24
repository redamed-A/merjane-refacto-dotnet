using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Services.Strategies
{
    public class SeasonalProductStrategy : IProductStrategy
    {
        public bool CanHandle(string? type) => type == "SEASONAL";

        public void Handle(Product p, AppDbContext ctx, INotificationService ns)
        {
            if (DateTime.Now.Date > p.SeasonStartDate && DateTime.Now.Date < p.SeasonEndDate && p.Available > 0)
            {
                p.Available -= 1;
                ctx.SaveChanges();
            }
            else
            {
                if (DateTime.Now.AddDays(p.LeadTime) > p.SeasonEndDate)
                {
                    ns.SendOutOfStockNotification(p.Name);
                    p.Available = 0;
                    ctx.SaveChanges();
                }
                else if (p.SeasonStartDate > DateTime.Now)
                {
                    ns.SendOutOfStockNotification(p.Name);
                    ctx.SaveChanges();
                }
                else
                {
                    ns.SendDelayNotification(p.LeadTime, p.Name);
                    ctx.SaveChanges();
                }
            }
        }
    }
}
