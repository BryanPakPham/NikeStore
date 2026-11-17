using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using NikeStore.Infrastructure;
using NikeStore.Models;
using NikeStore.ViewModels.Admin;

namespace NikeStore.Controllers.Admin
{
    [AdminOnly]
    public class DashboardController : Controller
    {
        private readonly NikeStoreDBEntities db = new NikeStoreDBEntities();

        public ActionResult Index()
        {
            var settings = AdminSettingsStore.Current().Load();
            var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var end = start.AddMonths(1);
            var lowStock = db.product_variants.AsNoTracking().Where(v => v.is_active == true &&
                v.product.is_active == true && (v.stock_quantity ?? 0) <= settings.LowStockThreshold);
            var model = new AdminDashboard
            {
                ActiveProducts = db.products.Count(p => p.is_active == true),
                ActiveEmployees = db.users.Count(u => u.is_active == true && u.role.role_name.ToLower() == "employee"),
                TotalCustomers = db.users.Count(u => u.role.role_name.ToLower() == "customer"),
                PendingOrders = db.orders.Count(o => o.order_status == null || o.order_status.ToLower() == "pending"),
                LowStockThreshold = settings.LowStockThreshold,
                LowStockCount = lowStock.Count(),
                CompletedRevenueThisMonth = db.orders.Where(o => o.order_date >= start && o.order_date < end &&
                    o.order_status.ToLower() == "completed").Sum(o => (decimal?)o.total_amount) ?? 0m,
                RecentOrders = db.orders.AsNoTracking().OrderByDescending(o => o.order_date)
                    .ThenByDescending(o => o.order_id).Take(8).Select(o => new AdminOrderRow
                    {
                        Id = o.order_id, Code = o.order_code, Recipient = o.recipient_name,
                        Date = o.order_date, Total = o.total_amount, Status = o.order_status
                    }).ToList(),
                LowStock = lowStock.OrderBy(v => v.stock_quantity ?? 0).ThenBy(v => v.variant_id)
                    .Take(8).Select(v => new AdminStockRow
                    {
                        Sku = v.sku, ProductName = v.product.product_name, Quantity = v.stock_quantity ?? 0
                    }).ToList()
            };
            return View("~/Views/Admin/Dashboard.cshtml", model);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
