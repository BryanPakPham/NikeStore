using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using NikeStore.Infrastructure;
using NikeStore.Models;
using NikeStore.ViewModels.Admin;

namespace NikeStore.Controllers.Admin
{
    [AdminOnly]
    public class RoleController : Controller
    {
        private readonly NikeStoreDBEntities db = new NikeStoreDBEntities();

        public ActionResult Index()
        {
            var rows = db.roles.AsNoTracking().OrderBy(r => r.role_id).Select(r => new RoleSummary
            {
                Id = r.role_id, Name = r.role_name, Description = r.description,
                Accounts = r.users.Count(), ActiveAccounts = r.users.Count(u => u.is_active == true)
            }).ToList();
            return View("~/Views/Admin/Roles.cshtml", rows);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
