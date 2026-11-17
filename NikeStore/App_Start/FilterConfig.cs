using System.Web;
using System.Web.Mvc;

namespace NikeStore
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new Infrastructure.AccountStateFilter(), -1000);
        }
    }
}
