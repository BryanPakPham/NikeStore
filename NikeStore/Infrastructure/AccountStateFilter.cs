using System;
using System.Linq;
using System.Web.Mvc;
using NikeStore.Models;

namespace NikeStore.Infrastructure
{
    public sealed class AccountStateFilter : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationContext context)
        {
            var session = context.HttpContext.Session;
            if (session == null || session["UserID"] == null) return;
            int id;
            if (!int.TryParse(Convert.ToString(session["UserID"]), out id))
            {
                session.Clear();
                return;
            }
            using (var db = new NikeStoreDBEntities())
            {
                var account = db.users.Where(u => u.user_id == id && u.is_active == true)
                    .Select(u => new { Name = u.full_name, Role = u.role.role_name }).SingleOrDefault();
                if (account == null)
                {
                    session.Clear();
                    session.Abandon();
                    return;
                }
                session["Role"] = account.Role;
                session["FullName"] = account.Name;
            }
        }
    }
}
