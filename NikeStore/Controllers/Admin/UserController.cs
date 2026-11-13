using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web.Mvc;
using NikeStore.Infrastructure;
using NikeStore.Models;
using NikeStore.ViewModels.Admin;

namespace NikeStore.Controllers.Admin
{
    [AdminOnly]
    public class UserController : Controller
    {
        private readonly NikeStoreDBEntities db = new NikeStoreDBEntities();

        public ActionResult Index(string q)
        {
            q = (q ?? "").Trim();
            IQueryable<user> query = db.users.AsNoTracking().Include(u => u.role);
            if (q.Length > 0)
                query = query.Where(u => u.full_name.Contains(q) || u.email.Contains(q) || u.phone.Contains(q));
            ViewBag.Query = q;
            return View("~/Views/Admin/Users.cshtml", query.OrderBy(u => u.full_name).ThenBy(u => u.user_id).ToList());
        }

        [HttpGet]
        public ActionResult Create()
        {
            return FormView("UserCreate", new UserForm { Active = true });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(UserForm form, string password)
        {
            ValidateAccount(form, 0);
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                ModelState.AddModelError("password", "Mật khẩu cần ít nhất 8 ký tự.");
            if (ModelState.IsValid)
            {
                var row = new user { created_at = DateTime.Now, password_hash = AccountPassword.Hash(password) };
                Apply(form, row);
                db.users.Add(row);
                if (SaveAccount()) return RedirectToAction("Index");
            }
            return FormView("UserCreate", form);
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            var row = db.users.Find(id);
            if (row == null) return HttpNotFound();
            return FormView("UserEdit", new UserForm
            {
                Id = row.user_id, Name = row.full_name, Email = row.email, Phone = row.phone,
                Address = row.address, RoleId = row.role_id, Active = row.is_active == true
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(int id, [Bind(Exclude = "Id")] UserForm form)
        {
            var row = db.users.Find(id);
            if (row == null) return HttpNotFound();
            form.Id = id;
            ValidateAccount(form, id);
            if (ModelState.IsValid)
            {
                Apply(form, row);
                if (SaveAccount())
                {
                    if (Equals(Session["UserID"], id))
                    {
                        var roleName = db.roles.Where(r => r.role_id == form.RoleId).Select(r => r.role_name).Single();
                        if (!form.Active || !string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase))
                        {
                            Session.Clear();
                            return RedirectToRoute("Admin", new { controller = "Account", action = "Login" });
                        }
                        Session["FullName"] = row.full_name;
                    }
                    return RedirectToAction("Index");
                }
            }
            return FormView("UserEdit", form);
        }

        private void ValidateAccount(UserForm form, int id)
        {
            form.Email = (form.Email ?? "").Trim();
            if (db.users.Any(u => u.user_id != id && u.email == form.Email))
                ModelState.AddModelError("Email", "Email này đã được sử dụng.");
            if (!db.roles.Any(r => r.role_id == form.RoleId))
                ModelState.AddModelError("RoleId", "Vui lòng chọn một vai trò trong danh sách.");
        }

        private static void Apply(UserForm form, user row)
        {
            row.full_name = form.Name.Trim();
            row.email = form.Email;
            row.phone = form.Phone == null ? null : form.Phone.Trim();
            row.address = form.Address == null ? null : form.Address.Trim();
            row.role_id = form.RoleId;
            row.is_active = form.Active;
            row.updated_at = DateTime.Now;
        }

        private bool SaveAccount()
        {
            try { db.SaveChanges(); return true; }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Không lưu được tài khoản. Hãy kiểm tra email trùng hoặc vai trò vừa thay đổi.");
                return false;
            }
        }

        private ActionResult FormView(string viewName, UserForm form)
        {
            ViewBag.Roles = new SelectList(db.roles.OrderBy(r => r.role_name).ToList(), "role_id", "role_name", form.RoleId);
            return View("~/Views/Admin/" + viewName + ".cshtml", form);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
