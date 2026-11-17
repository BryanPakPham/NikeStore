using System;
using System.Data;
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

        public ActionResult Index(string q, int? roleId, bool? active, int page = 1)
        {
            return List(q, roleId, active, page, false);
        }

        public ActionResult Employees(string q, bool? active, int page = 1)
        {
            return List(q, null, active, page, true);
        }

        private ActionResult List(string q, int? roleId, bool? active, int page, bool employees)
        {
            q = (q ?? "").Trim();
            if (q.Length > 200) q = q.Substring(0, 200);
            var query = db.users.AsNoTracking().AsQueryable();
            if (employees) query = query.Where(u => u.role.role_name.ToLower() == "employee");
            else if (roleId.HasValue) query = query.Where(u => u.role_id == roleId.Value);
            if (active.HasValue)
                query = active.Value ? query.Where(u => u.is_active == true) : query.Where(u => u.is_active != true);
            if (q.Length > 0)
                query = query.Where(u => u.full_name.Contains(q) || u.email.Contains(q) || u.phone.Contains(q));
            int count = query.Count();
            int size = AdminSettingsStore.Current().Load().RowsPerPage;
            int pages = AdminAccountPolicy.PageCount(count, size);
            page = Math.Min(Math.Max(1, page), pages);
            var model = new UserListPage
            {
                Query = q, RoleId = roleId, Active = active, EmployeesOnly = employees,
                Page = page, TotalPages = pages, TotalCount = count,
                CurrentUserId = Convert.ToInt32(Session["UserID"]),
                Roles = db.roles.AsNoTracking().OrderBy(r => r.role_name).ToList()
                    .Select(r => new SelectListItem { Value = r.role_id.ToString(), Text = AdminAccountPolicy.RoleLabel(r.role_name) }).ToList(),
                Items = query.OrderByDescending(u => u.created_at).ThenByDescending(u => u.user_id)
                    .Skip((page - 1) * size).Take(size).Select(u => new UserListRow
                    {
                        Id = u.user_id, Name = u.full_name, Email = u.email, Phone = u.phone,
                        Role = u.role.role_name, Active = u.is_active == true, CreatedAt = u.created_at
                    }).ToList()
            };
            return View("~/Views/Admin/Users.cshtml", model);
        }

        [HttpGet]
        public ActionResult Create(bool employees = false)
        {
            var form = new UserForm { Active = true, EmployeesOnly = employees };
            if (employees)
                form.RoleId = db.roles.Where(r => r.role_name.ToLower() == "employee").Select(r => r.role_id).FirstOrDefault();
            return FormView("UserCreate", form);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(UserForm form, string password)
        {
            form.Id = 0;
            ValidateAccount(form, 0);
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8 || password.Length > 128)
                ModelState.AddModelError("password", "Mật khẩu cần từ 8 đến 128 ký tự.");
            if (ModelState.IsValid)
            {
                var row = new user { created_at = DateTime.Now, password_hash = AccountPassword.Hash(password) };
                Apply(form, row);
                db.users.Add(row);
                if (SaveAccount())
                {
                    TempData["Success"] = "Đã tạo tài khoản.";
                    return Back(form.EmployeesOnly);
                }
            }
            return FormView("UserCreate", form);
        }

        [HttpGet]
        public ActionResult Edit(int id, bool employees = false)
        {
            var row = db.users.Find(id);
            if (row == null) return HttpNotFound();
            return FormView("UserEdit", new UserForm
            {
                Id = row.user_id, Name = row.full_name, Email = row.email, Phone = row.phone,
                Address = row.address, RoleId = row.role_id, Active = row.is_active == true,
                EmployeesOnly = employees
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(int id, [Bind(Exclude = "Id")] UserForm form)
        {
            form.Id = id;
            ValidateAccount(form, id);
            if (ModelState.IsValid)
            {
                using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
                {
                    var row = db.users.Include(u => u.role).SingleOrDefault(u => u.user_id == id);
                    if (row == null) return HttpNotFound();
                    string newRole = db.roles.Where(r => r.role_id == form.RoleId).Select(r => r.role_name).Single();
                    string error = CheckChange(row, newRole, form.Active);
                    if (error != null) ModelState.AddModelError("", error);
                    else
                    {
                        Apply(form, row);
                        if (SaveAccount())
                        {
                            transaction.Commit();
                            if (Convert.ToInt32(Session["UserID"]) == id) Session["FullName"] = row.full_name;
                            TempData["Success"] = "Đã cập nhật tài khoản và phân quyền.";
                            return Back(form.EmployeesOnly);
                        }
                    }
                }
            }
            return FormView("UserEdit", form);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult SetActive(int id, bool active, bool employees = false)
        {
            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                var row = db.users.Include(u => u.role).SingleOrDefault(u => u.user_id == id);
                if (row == null) return HttpNotFound();
                string error = CheckChange(row, row.role.role_name, active);
                if (error != null)
                {
                    TempData["Error"] = error;
                    return Back(employees);
                }
                row.is_active = active;
                row.updated_at = DateTime.Now;
                if (SaveAccount())
                {
                    transaction.Commit();
                    TempData["Success"] = active ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản.";
                }
                else TempData["Error"] = "Chưa lưu được trạng thái tài khoản. Vui lòng thử lại.";
            }
            return Back(employees);
        }

        private string CheckChange(user row, string newRole, bool active)
        {
            int admins = db.users.Count(u => u.is_active == true && u.role.role_name.ToLower() == "admin");
            return AdminAccountPolicy.ValidateChange(Convert.ToInt32(Session["UserID"]), row.user_id,
                row.is_active == true && string.Equals(row.role.role_name, "Admin", StringComparison.OrdinalIgnoreCase),
                newRole, active, admins);
        }

        private void ValidateAccount(UserForm form, int id)
        {
            form.Name = (form.Name ?? "").Trim();
            form.Email = (form.Email ?? "").Trim();
            if (form.Name.Length == 0) ModelState.AddModelError("Name", "Vui lòng nhập họ tên.");
            string email = form.Email.ToLowerInvariant();
            if (db.users.Any(u => u.user_id != id && u.email.ToLower() == email))
                ModelState.AddModelError("Email", "Email này đã được sử dụng.");
            if (!db.roles.Any(r => r.role_id == form.RoleId))
                ModelState.AddModelError("RoleId", "Vui lòng chọn một vai trò trong danh sách.");
        }

        private static void Apply(UserForm form, user row)
        {
            row.full_name = form.Name;
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
            ViewBag.Roles = db.roles.OrderBy(r => r.role_name).ToList().Select(r => new SelectListItem
            {
                Value = r.role_id.ToString(), Text = AdminAccountPolicy.RoleLabel(r.role_name), Selected = r.role_id == form.RoleId
            }).ToList();
            return View("~/Views/Admin/" + viewName + ".cshtml", form);
        }

        private ActionResult Back(bool employees)
        {
            return RedirectToAction(employees ? "Employees" : "Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
