using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using NikeStore.Models;

namespace NikeStore.Infrastructure
{
    public sealed class AdminOnlyAttribute : FilterAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationContext context)
        {
            var session = context.HttpContext.Session;
            int id;
            if (session == null || !int.TryParse(Convert.ToString(session["UserID"]), out id))
            {
                context.Result = new RedirectToRouteResult("Admin",
                    new RouteValueDictionary(new { controller = "Account", action = "Login" }));
                return;
            }

            // Kiểm tra lại để khóa tài khoản hoặc đổi quyền có hiệu lực ngay.
            using (var db = new NikeStoreDBEntities())
            {
                var account = db.users.Where(u => u.user_id == id && u.is_active == true)
                    .Select(u => new { Role = u.role.role_name, Name = u.full_name }).SingleOrDefault();
                if (account == null || !string.Equals(account.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    context.Result = new HttpStatusCodeResult(403);
                    return;
                }
                session["Role"] = account.Role;
                session["FullName"] = account.Name;
            }
        }
    }
}
