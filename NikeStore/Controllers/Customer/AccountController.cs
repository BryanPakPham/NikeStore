
using System;
using System.Linq;
using System.Web.Mvc;
using System.Security.Cryptography;
using NikeStore.Models;

namespace NikeStore.Controllers
{
    public class AccountController : Controller
    {
        public NikeStoreDBEntities data = new NikeStoreDBEntities();

        // Hiển thị trang đăng ký
        public ActionResult Register()
        {
            return View("~/Views/Customer/Register.cshtml");
        }

        // Xử lý đăng ký
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(string full_name, string email, string password, string phone, string address)
        {
            email = email.Trim();

            // Kiểm tra email đã tồn tại
            var kiemTra = data.users.FirstOrDefault(u => u.email == email);

            if (kiemTra != null)
            {
                ViewBag.ThongBao = "Email already exists!";
                return View();
            }

            // Lấy quyền Customer
            var roleCustomer = data.roles.FirstOrDefault(r => r.role_name == "Customer");

            if (roleCustomer == null)
            {
                ViewBag.ThongBao = "Customer role not found!";
                return View();
            }

            // Tạo tài khoản mới
            user taiKhoan = new user();

            taiKhoan.full_name = full_name;
            taiKhoan.email = email;
            taiKhoan.password_hash =password;
            taiKhoan.phone = phone;
            taiKhoan.address = address;
            taiKhoan.role_id = roleCustomer.role_id;
            taiKhoan.is_active = true;
            taiKhoan.created_at = DateTime.Now;

            data.users.Add(taiKhoan);
            data.SaveChanges();

            TempData["ThongBao"] = "Register successfully! Please login.";

            return View("~/Views/Customer/Register.cshtml");
        }

        // Hiển thị trang đăng nhập
        public ActionResult Login()
        {
            return View("~/Views/Customer/Login.cshtml");
        }

        // Xử lý đăng nhập
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string email, string password)
        {
            email = email.Trim();

            var taiKhoan = data.users
                .FirstOrDefault(u => u.email == email);

            if (taiKhoan == null ||
                taiKhoan.is_active != true)
            {
                ViewBag.ThongBao = "Email or password is incorrect!";
                return View();
            }

            // Lưu thông tin đăng nhập
            Session["UserID"] = taiKhoan.user_id;
            Session["FullName"] = taiKhoan.full_name;
            Session["Role"] = taiKhoan.role.role_name;

            // Chuyển trang theo quyền
            if (taiKhoan.role.role_name == "Admin")
            {
                return Redirect("~/Admin/Home/Index");
            }
            else if (taiKhoan.role.role_name == "Employee")
            {
                return Redirect("~/Employee/Home/Index");
            }

            return View("~/Views/Customer/Login.cshtml");
        }

        // Đăng xuất
        public ActionResult Logout()
        {
            Session.Clear();

            return Redirect("~/Customer/Home/Index");
        }
    }
}
