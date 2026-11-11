using Admin_doan.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Admin_doan.Controllers.Admin
{
    public class AccountController : Controller
    {
        private NikeStoreDBEntities db = new NikeStoreDBEntities();

        // GET: Login
        [HttpGet]
        public ActionResult Login()
        {
            if (Session["UserId"] != null)
            {
                return RedirectToAction("Products", "Product");
            }

            return View("~/Views/Admin/Login.cshtml");
        }

        // POST: Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng nhập email và mật khẩu.";
                return View("~/Views/Admin/Login.cshtml");
            }

            // Tìm tài khoản theo email
            var user = db.users
                .Include("roles")
                .FirstOrDefault(u => u.email == email);

            // Kiểm tra tài khoản
            if (user == null || user.is_active != true)
            {
                ViewBag.Error = "Tài khoản không tồn tại hoặc đã bị khóa.";
                return View("~/Views/Admin/Login.cshtml");
            }

            // Kiểm tra quyền Admin
            if (user.roles == null ||
                user.roles.role_name != "Admin")
            {
                ViewBag.Error = "Bạn không có quyền truy cập hệ thống quản trị.";
                return View("~/Views/Admin/Login.cshtml");
            }

            // TODO: Xác thực password_hash tại đây.
            // Chưa xác định thuật toán mã hóa nên chưa thể
            // kiểm tra mật khẩu một cách chính xác.

            ViewBag.Error = "Chưa hoàn thiện xác thực mật khẩu.";
            return View("~/Views/Admin/Login.cshtml");
        }

        // POST: Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Login", "Account");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}