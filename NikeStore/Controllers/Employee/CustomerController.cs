using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using NikeStore.Models;
using NikeStore.ViewModels.Employee;

namespace NikeStore.Controllers.Employee
{
    // =====================================================================
    //  CustomerController (Employee)
    //  Tra cứu thông tin khách hàng, xem đơn của khách, hỗ trợ cập nhật
    //  thông tin liên hệ. Nhân viên KHÔNG được xóa khách hàng (chỉ Admin).
    // =====================================================================
    public class CustomerController : EmployeeBaseController
    {
        private const int PageSize = 12;

        // Tên vai trò của khách hàng trong bảng roles (không phân biệt hoa/thường)
        private const string CustomerRoleName = "customer";

        private IQueryable<user> Customers()
        {
            return db.users.Where(u => u.role.role_name.ToLower() == CustomerRoleName);
        }

        // GET: Employee/Customer?q=abc&page=1
        public ActionResult Index(string q, int page = 1)
        {
            var query = Customers();

            q = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
            if (q != null)
            {
                query = query.Where(u => u.full_name.Contains(q)
                                      || u.email.Contains(q)
                                      || u.phone.Contains(q));
            }

            int total = query.Count();
            int totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
            page = Math.Min(Math.Max(1, page), totalPages);

            var items = query
                .OrderByDescending(u => u.created_at)
                .ThenBy(u => u.full_name)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(u => new CustomerRowVM
                {
                    UserId = u.user_id,
                    FullName = u.full_name,
                    Email = u.email,
                    Phone = u.phone,
                    Address = u.address,
                    IsActive = u.is_active ?? true,
                    CreatedAt = u.created_at,
                    OrderCount = db.orders.Count(o => o.customer_id == u.user_id)
                })
                .ToList();

            var vm = new CustomerListVM
            {
                Items = items,
                Query = q,
                Page = page,
                TotalPages = totalPages,
                TotalCount = total
            };

            return View("~/Views/Employee/Customers.cshtml", vm);
        }

        // GET: Employee/Customer/Details/5
        public ActionResult Details(int id)
        {
            var u = Customers().FirstOrDefault(x => x.user_id == id);
            if (u == null) return HttpNotFound();

            var orders = db.orders.Where(o => o.customer_id == id);

            var vm = new CustomerDetailsVM
            {
                UserId = u.user_id,
                FullName = u.full_name,
                Email = u.email,
                Phone = u.phone,
                Address = u.address,
                IsActive = u.is_active ?? true,
                CreatedAt = u.created_at,
                UpdatedAt = u.updated_at,
                OrderCount = orders.Count(),
                CompletedCount = orders.Count(o => o.order_status.ToLower() == "completed"),
                TotalSpent = orders
                    .Where(o => o.order_status.ToLower() == "completed")
                    .Sum(o => (decimal?)o.total_amount) ?? 0m,
                RecentOrders = ProjectRows(
                        orders.OrderByDescending(o => o.order_date)
                              .ThenByDescending(o => o.order_id)
                              .Take(10))
                    .ToList()
            };

            return View("~/Views/Employee/CustomerDetails.cshtml", vm);
        }

        // POST: Employee/Customer/Update
        // Hỗ trợ khách: sửa họ tên, số điện thoại, địa chỉ (email giữ nguyên vì là tên đăng nhập).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(int id, string fullName, string phone, string address)
        {
            string back = Url.Content("~/Employee/Customer/Details/" + id);

            var u = Customers().FirstOrDefault(x => x.user_id == id);
            if (u == null) return HttpNotFound();

            fullName = (fullName ?? "").Trim();
            phone = (phone ?? "").Trim();
            address = (address ?? "").Trim();

            if (fullName.Length == 0 || fullName.Length > 100)
            {
                TempData["Error"] = "Họ tên là bắt buộc (tối đa 100 ký tự).";
                return Redirect(back);
            }
            if (phone.Length > 20 || (phone.Length > 0 && !Regex.IsMatch(phone, @"^[0-9+\-\s().]+$")))
            {
                TempData["Error"] = "Số điện thoại không hợp lệ (tối đa 20 ký tự, chỉ gồm chữ số).";
                return Redirect(back);
            }
            if (address.Length > 300)
            {
                TempData["Error"] = "Địa chỉ quá dài (tối đa 300 ký tự).";
                return Redirect(back);
            }

            // Chỉ ghi những trường thật sự thay đổi
            bool changed = false;
            if (u.full_name != fullName) { u.full_name = fullName; changed = true; }
            if ((u.phone ?? "") != phone) { u.phone = phone.Length == 0 ? null : phone; changed = true; }
            if ((u.address ?? "") != address) { u.address = address.Length == 0 ? null : address; changed = true; }

            if (!changed)
            {
                TempData["Success"] = "Không có thay đổi nào để lưu.";
                return Redirect(back);
            }

            u.updated_at = DateTime.Now;
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    db.SaveChanges();
                    tx.Commit();
                    TempData["Success"] = "Đã cập nhật thông tin khách hàng.";
                }
                catch (Exception)
                {
                    tx.Rollback();
                    TempData["Error"] = "Không lưu được thay đổi. Dữ liệu chưa bị thay đổi, vui lòng thử lại.";
                }
            }

            return Redirect(back);
        }
    }
}

namespace NikeStore.ViewModels.Employee
{
    public class CustomerRowVM
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int OrderCount { get; set; }
    }

    public class CustomerListVM
    {
        public List<CustomerRowVM> Items { get; set; }
        public string Query { get; set; }
        public int Page { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
    }

    public class CustomerDetailsVM
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int OrderCount { get; set; }
        public int CompletedCount { get; set; }
        public decimal TotalSpent { get; set; }
        public List<OrderRowVM> RecentOrders { get; set; }
    }
}
