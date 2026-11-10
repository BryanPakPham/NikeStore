using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NikeStore.Models;
using NikeStore.ViewModels.Employee;

namespace NikeStore.Controllers.Employee
{
    // =====================================================================
    //  EmployeeBaseController
    //  Lớp cha dùng chung cho 3 controller của Employee
    //  (Dashboard / Order / Customer). Gồm: DbContext, kiểm tra đăng nhập,
    //  và vài hàm dùng chung.
    // =====================================================================
    public abstract class EmployeeBaseController : Controller
    {
        protected readonly NikeStoreDBEntities db = new NikeStoreDBEntities();

        // true = bắt buộc đăng nhập. AccountController.Login đang lưu Session:
        //   Session["UserID"]   (int)    - user_id của người đăng nhập
        //   Session["FullName"] (string) - họ tên hiển thị
        //   Session["Role"]     (string) - "Customer" / "Employee" / "Admin"
        // Người chưa đăng nhập hoặc không phải Employee/Admin sẽ bị chuyển về trang Login.
        protected static readonly bool RequireLogin = true;

        protected int? CurrentUserId
        {
            get
            {
                object v = Session["UserID"] ?? Session["UserId"];
                if (v == null) return null;
                if (v is int) return (int)v;
                int parsed;
                return int.TryParse(Convert.ToString(v), out parsed) ? parsed : (int?)null;
            }
        }

        private bool IsStaff()
        {
            string role = Convert.ToString(Session["Role"] ?? Session["RoleName"]);
            return CurrentUserId.HasValue
                && (string.Equals(role, "Employee", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase));
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (RequireLogin && !IsStaff())
            {
                filterContext.Result = new RedirectResult("~/Customer/Account/Login");
                return;
            }

            string name = Convert.ToString(Session["FullName"]);
            ViewBag.EmployeeName = string.IsNullOrWhiteSpace(name) ? "Nhân viên" : name;
            base.OnActionExecuting(filterContext);
        }

        // Số đơn theo từng trạng thái (đã chuẩn hóa tên trạng thái)
        protected Dictionary<string, int> GetStatusCounts()
        {
            var raw = db.orders
                .GroupBy(o => o.order_status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            var result = OrderStatus.All.ToDictionary(s => s, s => 0);
            foreach (var r in raw)
            {
                string key = OrderStatus.Normalize(r.Status);
                int existing;
                result.TryGetValue(key, out existing);
                result[key] = existing + r.Count;
            }
            return result;
        }

        // Chuyển query đơn hàng thành dòng hiển thị trong bảng
        protected IQueryable<OrderRowVM> ProjectRows(IQueryable<order> query)
        {
            return query.Select(o => new OrderRowVM
            {
                OrderId = o.order_id,
                Code = o.order_code,
                OrderDate = o.order_date,
                Recipient = o.recipient_name,
                Phone = o.recipient_phone,
                ItemCount = o.order_details.Sum(d => (int?)d.quantity) ?? 0,
                Total = o.total_amount,
                RawStatus = o.order_status,
                PaymentMethod = o.payment_method,
                PaymentStatus = o.payment_status
            });
        }

        protected ActionResult RedirectToLocal(string returnUrl, string fallback)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return Redirect(fallback);
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

    // =====================================================================
    //  DashboardController (Employee) - Trang tổng quan công việc
    // =====================================================================
    public class DashboardController : EmployeeBaseController
    {
        private const int LowStockThreshold = 10;
        private const int ListSize = 6;

        // GET: Employee/Dashboard
        public ActionResult Index()
        {
            var vm = new DashboardVM();
            vm.StatusCounts = GetStatusCounts();

            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);
            vm.OrdersToday = db.orders.Count(o => o.order_date >= today && o.order_date < tomorrow);

            // Đơn chuyển khoản chưa nhận được tiền (chưa hủy)
            vm.AwaitingPayment = db.orders.Count(o => o.payment_method == "BankTransfer"
                && (o.payment_status == null || o.payment_status == "Unpaid")
                && (o.order_status == null || o.order_status != "Cancelled"));

            // Đơn đang chờ xác nhận: cũ nhất lên đầu để xử lý trước
            vm.PendingOrders = ProjectRows(
                db.orders
                  .Where(o => o.order_status == null || o.order_status.ToLower() == "pending")
                  .OrderBy(o => o.order_date)
                  .ThenBy(o => o.order_id)
                  .Take(ListSize))
                .ToList();

            // Biến thể sắp hết hàng (chỉ để theo dõi, việc nhập kho thuộc Admin/Inventory)
            var lowStock = from v in db.product_variants
                           join pc in db.product_colors on v.product_color_id equals pc.product_color_id
                           where v.is_active == true && v.stock_quantity <= LowStockThreshold
                           select new { v, pc };

            vm.LowStockCount = lowStock.Count();
            vm.LowStockThreshold = LowStockThreshold;
            vm.LowStock = lowStock
                .OrderBy(x => x.v.stock_quantity)
                .Take(ListSize)
                .Select(x => new LowStockVM
                {
                    ProductName = x.v.product.product_name,
                    ColorName = x.pc.color.color_name,
                    SizeValue = x.v.size.size_value,
                    Sku = x.v.sku,
                    Stock = x.v.stock_quantity ?? 0
                })
                .ToList();

            return View("~/Views/Employee/Dashboard.cshtml", vm);
        }
    }
}

// =========================================================================
//  ViewModels + helper dùng chung cho toàn bộ khu vực Employee
//  (để trong file controller để không phải sửa NikeStore.csproj)
// =========================================================================
namespace NikeStore.ViewModels.Employee
{
    // Trạng thái đơn hàng.
    // Luồng xử lý: Pending -> Confirmed -> Shipping -> Completed (hoặc Cancelled khi chưa giao).
    // Nếu CSDL của nhóm đặt tên trạng thái khác, chỉ cần sửa các hằng số bên dưới.
    public static class OrderStatus
    {
        public const string Pending = "Pending";
        public const string Confirmed = "Confirmed";
        public const string Shipping = "Shipping";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";

        public static readonly string[] All = { Pending, Confirmed, Shipping, Completed, Cancelled };

        public static string Normalize(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return Pending;
            string s = status.Trim();
            foreach (string known in All)
            {
                if (string.Equals(known, s, StringComparison.OrdinalIgnoreCase)) return known;
            }
            return s;
        }

        public static string Css(string status)
        {
            string s = Normalize(status).ToLowerInvariant();
            var chars = s.Where(char.IsLetterOrDigit).ToArray();
            return "status-" + new string(chars);
        }

        public static string[] Next(string status)
        {
            switch (Normalize(status))
            {
                case Pending: return new[] { Confirmed, Cancelled };
                case Confirmed: return new[] { Shipping, Cancelled };
                case Shipping: return new[] { Completed };
                default: return new string[0];
            }
        }

        public static bool CanMove(string from, string to)
        {
            return Next(from).Contains(Normalize(to));
        }

        // Tên hiển thị tiếng Việt (giá trị lưu trong CSDL vẫn là tiếng Anh)
        public static string Label(string status)
        {
            switch (Normalize(status))
            {
                case Pending: return "Chờ xác nhận";
                case Confirmed: return "Đã xác nhận";
                case Shipping: return "Đang giao";
                case Completed: return "Hoàn tất";
                case Cancelled: return "Đã hủy";
                default: return status;
            }
        }

        public static string ActionLabel(string target)
        {
            switch (Normalize(target))
            {
                case Confirmed: return "Xác nhận đơn";
                case Shipping: return "Bàn giao vận chuyển";
                case Completed: return "Hoàn tất đơn";
                case Cancelled: return "Hủy đơn";
                default: return target;
            }
        }
    }

    // Trạng thái thanh toán (khớp CHECK trong CSDL): Unpaid, Paid, Failed, Refunded
    public static class PaymentStatus
    {
        public const string Unpaid = "Unpaid";
        public const string Paid = "Paid";
        public const string Failed = "Failed";
        public const string Refunded = "Refunded";

        public static readonly string[] All = { Unpaid, Paid, Failed, Refunded };

        public static string Normalize(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return Unpaid;
            string s = status.Trim();
            foreach (string known in All)
            {
                if (string.Equals(known, s, StringComparison.OrdinalIgnoreCase)) return known;
            }
            return s;
        }

        public static string Css(string status)
        {
            return "pay-" + Normalize(status).ToLowerInvariant();
        }

        // Nhân viên chỉ xác nhận thanh toán chuyển khoản: Unpaid -> Paid/Failed, Failed -> Paid
        public static string[] Next(string status)
        {
            switch (Normalize(status))
            {
                case Unpaid: return new[] { Paid, Failed };
                case Failed: return new[] { Paid };
                default: return new string[0];
            }
        }

        public static bool CanMove(string from, string to)
        {
            return Next(from).Contains(Normalize(to));
        }

        public static string Label(string status)
        {
            switch (Normalize(status))
            {
                case Unpaid: return "Chưa thanh toán";
                case Paid: return "Đã thanh toán";
                case Failed: return "Thất bại";
                case Refunded: return "Đã hoàn tiền";
                default: return status;
            }
        }

        public static string ActionLabel(string target)
        {
            switch (Normalize(target))
            {
                case Paid: return "Đã nhận tiền";
                case Failed: return "Thanh toán thất bại";
                default: return target;
            }
        }
    }

    public static class StaffFormat
    {
        private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

        public static string Money(decimal value)
        {
            // Giá trong CSDL là VND: 2.929.000 ₫
            return value.ToString("#,##0", Vi) + " ₫";
        }

        public static string Money(decimal? value)
        {
            return value.HasValue ? Money(value.Value) : "-";
        }

        public static string Date(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : "-";
        }

        public static string DateTimeText(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) : "-";
        }

        public static string Today()
        {
            return DateTime.Now.ToString("dddd, dd/MM/yyyy", Vi).ToUpper(Vi);
        }

        public static string Method(string method)
        {
            if (string.IsNullOrWhiteSpace(method)) return "-";
            if (string.Equals(method, "COD", StringComparison.OrdinalIgnoreCase)) return "Thanh toán khi nhận hàng (COD)";
            if (string.Equals(method, "BankTransfer", StringComparison.OrdinalIgnoreCase)) return "Chuyển khoản ngân hàng";
            return method;
        }

        public static string Movement(string type)
        {
            switch ((type ?? "").Trim().ToLowerInvariant())
            {
                case "sale": return "Bán hàng";
                case "cancelreturn": return "Hoàn kho (hủy đơn)";
                case "import": return "Nhập kho";
                case "adjustment": return "Điều chỉnh";
                default: return type;
            }
        }

        public static string Initial(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            return name.Trim().Substring(0, 1).ToUpperInvariant();
        }

        public static string Size(decimal size)
        {
            return size.ToString("0.#", CultureInfo.InvariantCulture);
        }
    }

    public class OrderRowVM
    {
        public int OrderId { get; set; }
        public string Code { get; set; }
        public DateTime? OrderDate { get; set; }
        public string Recipient { get; set; }
        public string Phone { get; set; }
        public int ItemCount { get; set; }
        public decimal Total { get; set; }
        public string RawStatus { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }

        public string Status { get { return OrderStatus.Normalize(RawStatus); } }
        public string PayStatus { get { return NikeStore.ViewModels.Employee.PaymentStatus.Normalize(this.PaymentStatus); } }
    }

    public class LowStockVM
    {
        public string ProductName { get; set; }
        public string ColorName { get; set; }
        public decimal SizeValue { get; set; }
        public string Sku { get; set; }
        public int Stock { get; set; }
    }

    public class DashboardVM
    {
        public DashboardVM()
        {
            StatusCounts = new Dictionary<string, int>();
            PendingOrders = new List<OrderRowVM>();
            LowStock = new List<LowStockVM>();
        }

        public Dictionary<string, int> StatusCounts { get; set; }
        public int OrdersToday { get; set; }
        public int AwaitingPayment { get; set; }
        public List<OrderRowVM> PendingOrders { get; set; }
        public List<LowStockVM> LowStock { get; set; }
        public int LowStockCount { get; set; }
        public int LowStockThreshold { get; set; }

        public int Count(string status)
        {
            int n;
            return StatusCounts.TryGetValue(status, out n) ? n : 0;
        }
    }
}
