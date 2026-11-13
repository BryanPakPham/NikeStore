using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using NikeStore.Infrastructure;
using NikeStore.Models;

namespace NikeStore.Controllers.Admin
{
    public class AccountController : Controller
    {
        [HttpGet]
        public ActionResult Login()
        {
            return View("~/Views/Admin/Login.cshtml");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Login(string email, string password)
        {
            email = (email ?? "").Trim();
            using (var db = new NikeStoreDBEntities())
            {
                var account = db.users.Include(u => u.role)
                    .FirstOrDefault(u => u.email == email && u.is_active == true);
                if (account != null && account.role != null &&
                    string.Equals(account.role.role_name, "Admin", StringComparison.OrdinalIgnoreCase) &&
                    AccountPassword.Matches(account.password_hash, password))
                {
                    Session["UserID"] = account.user_id;
                    Session["Role"] = account.role.role_name;
                    Session["FullName"] = account.full_name;
                    return RedirectToRoute("Admin", new { controller = "Dashboard", action = "Index" });
                }
            }
            ViewBag.Error = "Email, mật khẩu hoặc quyền quản trị không hợp lệ.";
            return View("~/Views/Admin/Login.cshtml");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToRoute("Admin", new { controller = "Account", action = "Login" });
        }
    }
}
