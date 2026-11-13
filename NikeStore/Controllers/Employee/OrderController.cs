using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NikeStore.Models;
using NikeStore.ViewModels.Employee;

namespace NikeStore.Controllers.Employee
{
    // =====================================================================
    //  OrderController (Employee)
    //  Nghiệp vụ nhân viên: xem đơn, xác nhận, giao hàng, hoàn tất, hủy đơn,
    //  xác nhận đã nhận tiền chuyển khoản.
    //  Mọi thao tác GHI đều chạy trong transaction + khóa dòng đơn hàng.
    // =====================================================================
    public class OrderController : EmployeeBaseController
    {
        protected virtual string OrderModule { get { return "Employee"; } }

        private const int PageSize = 10;

        // Khớp CHECK của bảng inventory_transactions:
        // 'Import', 'Sale', 'CancelReturn', 'Adjustment'
        private const string RestockTransactionType = "CancelReturn";
        private const string BankTransfer = "BankTransfer";
        private const string Cod = "COD";

        // GET: Employee/Order?status=Pending&q=abc&page=1
        public ActionResult Index(string status, string q, int page = 1)
        {
            var query = db.orders.AsQueryable();

            string activeStatus = "All";
            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                activeStatus = OrderStatus.Normalize(status);
                string lower = activeStatus.ToLower();
                if (activeStatus == OrderStatus.Pending)
                {
                    query = query.Where(o => o.order_status == null || o.order_status.ToLower() == lower);
                }
                else
                {
                    query = query.Where(o => o.order_status.ToLower() == lower);
                }
            }

            q = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
            if (q != null)
            {
                query = query.Where(o => o.order_code.Contains(q)
                                      || o.recipient_name.Contains(q)
                                      || o.recipient_phone.Contains(q));
            }

            int total = query.Count();
            int totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
            page = Math.Min(Math.Max(1, page), totalPages);

            var paged = query
                .OrderByDescending(o => o.order_date)
                .ThenByDescending(o => o.order_id)
                .Skip((page - 1) * PageSize)
                .Take(PageSize);

            var vm = new OrderListVM
            {
                Items = ProjectRows(paged).ToList(),
                Status = activeStatus,
                Query = q,
                Page = page,
                TotalPages = totalPages,
                TotalCount = total,
                StatusCounts = GetStatusCounts()
            };

            return View("~/Views/" + OrderModule + "/Orders.cshtml", vm);
        }

        // GET: Employee/Order/Details/5
        public ActionResult Details(int id)
        {
            var o = db.orders.Find(id);
            if (o == null) return HttpNotFound();

            // customer_id có thể là int hoặc int? tùy model của nhóm: dùng int? cho cả hai trường hợp
            int? customerId = o.customer_id;
            var customer = customerId.HasValue ? db.users.Find(customerId.Value) : null;
            var handler = o.handled_by_user_id.HasValue ? db.users.Find(o.handled_by_user_id.Value) : null;
            var coupon = o.coupon_id.HasValue ? db.coupons.Find(o.coupon_id.Value) : null;

            // Ảnh của từng dòng (theo sản phẩm + màu của biến thể).
            // Ưu tiên: ảnh chính (is_primary) -> ảnh góc "Side" (giống trang khách) -> ảnh đầu tiên.
            var images = (from d in db.order_details
                          where d.order_id == id
                          join v in db.product_variants on d.variant_id equals v.variant_id
                          join i in db.product_images
                              on new { v.product_id, v.product_color_id }
                              equals new { i.product_id, i.product_color_id }
                          select new { d.order_detail_id, i.image_url, i.is_primary, i.image_angle })
                         .ToList()
                         .Where(x => !string.IsNullOrWhiteSpace(x.image_url))
                         .GroupBy(x => x.order_detail_id)
                         .ToDictionary(
                             g => g.Key,
                             g => NormalizeImageUrl(g
                                 .OrderByDescending(x => x.is_primary == true)
                                 .ThenByDescending(x => x.image_angle == "Side")
                                 .First().image_url));

            var items = db.order_details
                .Where(d => d.order_id == id)
                .OrderBy(d => d.order_detail_id)
                .ToList()
                .Select(d => new OrderItemVM
                {
                    ProductName = d.product_name,
                    ColorName = d.color_name,
                    SizeValue = d.size_value,
                    UnitPrice = d.unit_price,
                    Quantity = d.quantity,
                    ImageUrl = images.ContainsKey(d.order_detail_id) ? images[d.order_detail_id] : null
                })
                .ToList();

            var movements = (from t in db.inventory_transactions
                             where t.order_id == id
                             orderby t.created_at, t.transaction_id
                             select new StockMovementVM
                             {
                                 Type = t.transaction_type,
                                 Change = t.quantity_change,
                                 ProductName = t.product_variants.product.product_name,
                                 SizeValue = t.product_variants.size.size_value,
                                 Sku = t.product_variants.sku,
                                 Note = t.note,
                                 CreatedAt = t.created_at
                             }).ToList();

            var vm = new OrderDetailsVM
            {
                OrderId = o.order_id,
                Code = o.order_code,
                OrderDate = o.order_date,
                UpdatedAt = o.updated_at,
                Status = OrderStatus.Normalize(o.order_status),
                Recipient = o.recipient_name,
                Phone = o.recipient_phone,
                Address = o.shipping_address,
                Note = o.note,
                PaymentMethod = o.payment_method,
                PaymentStatus = PaymentStatus.Normalize(o.payment_status),
                Subtotal = o.subtotal,
                Discount = o.discount_amount ?? 0m,
                ShippingFee = o.shipping_fee ?? 0m,
                Total = o.total_amount,
                CouponCode = coupon != null ? coupon.code : null,
                CustomerId = customerId,
                CustomerName = customer != null ? customer.full_name : null,
                CustomerEmail = customer != null ? customer.email : null,
                HandledByName = handler != null ? handler.full_name : null,
                Items = items,
                Movements = movements
            };
            vm.NextStatuses = OrderStatus.Next(vm.Status);

            bool open = vm.Status == OrderStatus.Pending || vm.Status == OrderStatus.Confirmed || vm.Status == OrderStatus.Shipping;
            vm.NextPayments = (IsBank(o) && open) ? PaymentStatus.Next(vm.PaymentStatus) : new string[0];

            return View("~/Views/" + OrderModule + "/OrderDetails.cshtml", vm);
        }

        // =================================================================
        // POST: Employee/Order/UpdateStatus
        // Trong MỘT transaction:
        //   1. Khóa dòng đơn (UPDLOCK) -> 2 nhân viên không xử lý cùng lúc
        //   2. Đọc lại trạng thái mới nhất, kiểm tra đúng luồng + quy tắc thanh toán
        //   3. Cập nhật trạng thái, người xử lý, thanh toán
        //   4. Khi HỦY: hoàn kho (CancelReturn), trả lại 1 lượt coupon, hoàn tiền nếu đã thanh toán
        // Lỗi ở bất kỳ bước nào => rollback.
        // =================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(int id, string newStatus, string returnUrl)
        {
            string fallback = Url.Content("~/" + OrderModule + "/Order/Details/" + id);
            string target = OrderStatus.Normalize(newStatus);

            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    LockOrder(id);

                    var o = db.orders.Find(id);
                    if (o == null)
                    {
                        tx.Rollback();
                        return HttpNotFound();
                    }
                    db.Entry(o).Reload();

                    string current = OrderStatus.Normalize(o.order_status);
                    string pay = PaymentStatus.Normalize(o.payment_status);

                    if (!OrderStatus.CanMove(current, target))
                    {
                        tx.Rollback();
                        TempData["Error"] = "Đơn " + o.order_code + " đang ở trạng thái \"" + OrderStatus.Label(current)
                                          + "\", không thể chuyển sang \"" + OrderStatus.Label(target) + "\".";
                        return RedirectToLocal(returnUrl, fallback);
                    }

                    // Chuyển khoản phải được xác nhận ĐÃ NHẬN TIỀN trước khi giao hàng
                    if (target == OrderStatus.Shipping && IsBank(o) && pay != PaymentStatus.Paid)
                    {
                        tx.Rollback();
                        TempData["Error"] = "Đơn " + o.order_code
                                          + " thanh toán bằng chuyển khoản. Hãy xác nhận ĐÃ NHẬN TIỀN trước khi giao hàng.";
                        return RedirectToLocal(returnUrl, fallback);
                    }

                    int? uid = ValidUserId();

                    o.order_status = target;
                    o.updated_at = DateTime.Now;
                    if (uid.HasValue) o.handled_by_user_id = uid.Value;

                    string extra = "";

                    // XÁC NHẬN: đảm bảo kho đã được trừ cho đơn này.
                    // PlaceOrder (phía khách) hiện chưa trừ kho, nên trừ tại đây.
                    // Nếu đã có dòng 'Sale' trong inventory_transactions thì bỏ qua (không trừ 2 lần).
                    if (target == OrderStatus.Confirmed)
                    {
                        string shortage;
                        int deducted = DeductStockForOrder(o, uid, out shortage);
                        if (shortage != null)
                        {
                            tx.Rollback();
                            TempData["Error"] = shortage;
                            return RedirectToLocal(returnUrl, fallback);
                        }
                        if (deducted > 0) extra += " Đã trừ " + deducted + " đôi khỏi kho.";
                    }

                    // COD: giao thành công = đã thu tiền
                    if (target == OrderStatus.Completed && IsCod(o) && pay == PaymentStatus.Unpaid)
                    {
                        o.payment_status = PaymentStatus.Paid;
                        extra += " Đã ghi nhận thu tiền.";
                    }

                    if (target == OrderStatus.Cancelled)
                    {
                        int restocked = RestockForOrder(o, uid);
                        if (restocked > 0) extra += " Đã hoàn " + restocked + " đôi vào kho.";

                        if (o.coupon_id.HasValue)
                        {
                            db.Database.ExecuteSqlCommand(
                                "UPDATE dbo.coupons SET used_count = CASE WHEN ISNULL(used_count, 0) > 0 THEN used_count - 1 ELSE 0 END WHERE coupon_id = @p0",
                                o.coupon_id.Value);
                            extra += " Đã trả lại 1 lượt dùng mã giảm giá.";
                        }

                        if (pay == PaymentStatus.Paid)
                        {
                            o.payment_status = PaymentStatus.Refunded;
                            extra += " Thanh toán chuyển sang \"Đã hoàn tiền\".";
                        }
                    }

                    db.SaveChanges();
                    tx.Commit();

                    TempData["Success"] = "Đơn " + o.order_code + " đã chuyển sang \"" + OrderStatus.Label(target) + "\"." + extra;
                }
                catch (Exception)
                {
                    tx.Rollback();
                    TempData["Error"] = "Không cập nhật được đơn hàng. Dữ liệu chưa bị thay đổi, vui lòng thử lại.";
                }
            }

            return RedirectToLocal(returnUrl, fallback);
        }

        // =================================================================
        // POST: Employee/Order/UpdatePayment
        // Nhân viên xác nhận tiền chuyển khoản (Unpaid -> Paid / Failed).
        // =================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdatePayment(int id, string newPaymentStatus, string returnUrl)
        {
            string fallback = Url.Content("~/" + OrderModule + "/Order/Details/" + id);
            string target = PaymentStatus.Normalize(newPaymentStatus);

            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    LockOrder(id);

                    var o = db.orders.Find(id);
                    if (o == null)
                    {
                        tx.Rollback();
                        return HttpNotFound();
                    }
                    db.Entry(o).Reload();

                    string st = OrderStatus.Normalize(o.order_status);
                    string pay = PaymentStatus.Normalize(o.payment_status);
                    bool open = st == OrderStatus.Pending || st == OrderStatus.Confirmed || st == OrderStatus.Shipping;

                    if (!IsBank(o) || !open || !PaymentStatus.CanMove(pay, target))
                    {
                        tx.Rollback();
                        TempData["Error"] = "Không thể đổi thanh toán của đơn " + o.order_code + " sang \"" + PaymentStatus.Label(target) + "\".";
                        return RedirectToLocal(returnUrl, fallback);
                    }

                    int? uid = ValidUserId();
                    o.payment_status = target;
                    o.updated_at = DateTime.Now;
                    if (uid.HasValue) o.handled_by_user_id = uid.Value;

                    db.SaveChanges();
                    tx.Commit();

                    TempData["Success"] = "Thanh toán của đơn " + o.order_code + " đã chuyển sang \"" + PaymentStatus.Label(target) + "\".";
                }
                catch (Exception)
                {
                    tx.Rollback();
                    TempData["Error"] = "Không cập nhật được thanh toán. Dữ liệu chưa bị thay đổi, vui lòng thử lại.";
                }
            }

            return RedirectToLocal(returnUrl, fallback);
        }

        // ---------------------------------------------------------------
        // Đường dẫn ảnh trong CSDL có thể là "~/Content/...", "/Content/...", "http..." hoặc "Content/..."
        private static string NormalizeImageUrl(string url)
        {
            url = url.Trim();
            if (url.StartsWith("~/") || url.StartsWith("/") || url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return url;
            return "~/" + url;
        }

        private static bool IsBank(order o)
        {
            return string.Equals(o.payment_method, BankTransfer, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsCod(order o)
        {
            return string.IsNullOrEmpty(o.payment_method)
                || string.Equals(o.payment_method, Cod, StringComparison.OrdinalIgnoreCase);
        }

        private void LockOrder(int id)
        {
            db.Database.SqlQuery<int>(
                "SELECT order_id FROM dbo.orders WITH (UPDLOCK, ROWLOCK) WHERE order_id = @p0", id).ToList();
        }

        private int? ValidUserId()
        {
            int? uid = CurrentUserId;
            if (uid.HasValue && !db.users.Any(u => u.user_id == uid.Value)) return null;
            return uid;
        }

        // Trừ kho cho đơn (1 lần duy nhất). Trả về tổng số lượng đã trừ.
        // Không đủ hàng => shortage có nội dung báo lỗi và không trừ gì (caller rollback).
        private int DeductStockForOrder(order o, int? userId, out string shortage)
        {
            shortage = null;

            bool alreadyDeducted = db.inventory_transactions
                .Any(t => t.order_id == o.order_id && t.transaction_type == "Sale");
            if (alreadyDeducted) return 0;

            var lines = db.order_details
                .Where(d => d.order_id == o.order_id)
                .ToList()
                .GroupBy(d => d.variant_id)
                .Select(g => new { VariantId = g.Key, Qty = g.Sum(x => x.quantity), Name = g.First().product_name, Size = g.First().size_value })
                .ToList();

            int total = 0;
            foreach (var line in lines)
            {
                // khóa dòng biến thể rồi mới đọc tồn kho
                int stock = db.Database.SqlQuery<int?>(
                    "SELECT stock_quantity FROM dbo.product_variants WITH (UPDLOCK, ROWLOCK) WHERE variant_id = @p0",
                    line.VariantId).FirstOrDefault() ?? 0;

                if (stock < line.Qty)
                {
                    shortage = "Không đủ hàng trong kho cho \"" + line.Name + "\" size "
                             + StaffFormat.Size(line.Size) + " (cần " + line.Qty + ", còn " + stock + "). Chưa xác nhận đơn.";
                    return 0;
                }

                db.Database.ExecuteSqlCommand(
                    "UPDATE dbo.product_variants SET stock_quantity = stock_quantity - @p0 WHERE variant_id = @p1",
                    line.Qty, line.VariantId);

                db.inventory_transactions.Add(new inventory_transactions
                {
                    variant_id = line.VariantId,
                    user_id = userId,
                    order_id = o.order_id,
                    transaction_type = "Sale",
                    quantity_change = -line.Qty,
                    note = "Ban hang: xac nhan don " + o.order_code,
                    created_at = DateTime.Now
                });
                total += line.Qty;
            }
            return total;
        }

        // Hoàn kho đúng số lượng ĐÃ bị trừ cho đơn (tính từ inventory_transactions: Sale âm,
        // trừ đi những lần đã hoàn), nên không hoàn thừa và không hoàn hai lần.
        // Cộng kho bằng 1 câu lệnh nguyên tử, an toàn khi khách khác đang đặt hàng.
        private int RestockForOrder(order o, int? userId)
        {
            var net = db.inventory_transactions
                .Where(t => t.order_id == o.order_id)
                .GroupBy(t => t.variant_id)
                .Select(g => new { VariantId = g.Key, Net = g.Sum(x => x.quantity_change) })
                .ToList()
                .Where(x => x.Net < 0)
                .ToList();

            int total = 0;
            foreach (var row in net)
            {
                int qty = -row.Net;

                db.Database.ExecuteSqlCommand(
                    "UPDATE dbo.product_variants SET stock_quantity = ISNULL(stock_quantity, 0) + @p0 WHERE variant_id = @p1",
                    qty, row.VariantId);

                db.inventory_transactions.Add(new inventory_transactions
                {
                    variant_id = row.VariantId,
                    user_id = userId,
                    order_id = o.order_id,
                    transaction_type = RestockTransactionType,
                    quantity_change = qty,
                    note = "Hoan kho: huy don " + o.order_code,
                    created_at = DateTime.Now
                });
                total += qty;
            }
            return total;
        }
    }
}

namespace NikeStore.ViewModels.Employee
{
    public class OrderListVM
    {
        public List<OrderRowVM> Items { get; set; }
        public string Status { get; set; }          // "All" hoặc tên trạng thái
        public string Query { get; set; }
        public int Page { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public Dictionary<string, int> StatusCounts { get; set; }

        public int CountFor(string status)
        {
            if (string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCounts.Values.Sum();
            }
            int n;
            return StatusCounts.TryGetValue(status, out n) ? n : 0;
        }
    }

    public class OrderItemVM
    {
        public string ProductName { get; set; }
        public string ColorName { get; set; }
        public decimal SizeValue { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string ImageUrl { get; set; }
        public decimal LineTotal { get { return UnitPrice * Quantity; } }
    }

    public class StockMovementVM
    {
        public string Type { get; set; }
        public int Change { get; set; }
        public string ProductName { get; set; }
        public decimal SizeValue { get; set; }
        public string Sku { get; set; }
        public string Note { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class OrderDetailsVM
    {
        public int OrderId { get; set; }
        public string Code { get; set; }
        public DateTime? OrderDate { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Status { get; set; }
        public string Recipient { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Note { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal Total { get; set; }
        public string CouponCode { get; set; }
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string HandledByName { get; set; }
        public List<OrderItemVM> Items { get; set; }
        public List<StockMovementVM> Movements { get; set; }
        public string[] NextStatuses { get; set; }
        public string[] NextPayments { get; set; }
    }
}
