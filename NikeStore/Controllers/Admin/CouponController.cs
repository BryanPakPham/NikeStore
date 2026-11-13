using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web.Mvc;
using NikeStore.Infrastructure;
using NikeStore.Models;

namespace NikeStore.Controllers.Admin
{
    [AdminOnly]
    public class CouponController : Controller
    {
        private readonly NikeStoreDBEntities db = new NikeStoreDBEntities();
        private const string Fields = "code,discount_type,discount_value,min_order_value,max_discount,start_at,end_at,usage_limit,is_active";

        public ActionResult Index(string q)
        {
            q = (q ?? "").Trim();
            IQueryable<coupon> query = db.coupons.AsNoTracking();
            if (q.Length > 0) query = query.Where(c => c.code.Contains(q));
            ViewBag.Query = q;
            return View("~/Views/Admin/Coupons.cshtml", query.OrderByDescending(c => c.coupon_id).ToList());
        }

        [HttpGet]
        public ActionResult Create()
        {
            var input = new coupon
            {
                discount_type = "Percent", start_at = DateTime.Today, end_at = DateTime.Today.AddMonths(1),
                is_active = true, min_order_value = 0
            };
            return FormView("CouponCreate", input);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = Fields)] coupon input)
        {
            ValidateCoupon(input, 0);
            if (ModelState.IsValid)
            {
                input.used_count = 0;
                input.created_at = input.updated_at = DateTime.Now;
                db.coupons.Add(input);
                if (Save()) return RedirectToAction("Index");
            }
            return FormView("CouponCreate", input);
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            var row = db.coupons.Find(id);
            if (row == null) return HttpNotFound();
            return FormView("CouponEdit", row);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(int id, [Bind(Include = Fields)] coupon input)
        {
            // Đồng bộ với thao tác hoàn lượt dùng khi một đơn hàng bị hủy.
            using (var tx = db.Database.BeginTransaction())
            {
                db.Database.SqlQuery<int>(
                    "SELECT coupon_id FROM dbo.coupons WITH (UPDLOCK, ROWLOCK) WHERE coupon_id = @p0", id).ToList();
                var row = db.coupons.Find(id);
                if (row == null) return HttpNotFound();
                input.coupon_id = id;
                input.used_count = row.used_count;
                ValidateCoupon(input, id);
                if (ModelState.IsValid)
                {
                    row.code = input.code;
                    row.discount_type = input.discount_type;
                    row.discount_value = input.discount_value;
                    row.min_order_value = input.min_order_value;
                    row.max_discount = input.max_discount;
                    row.start_at = input.start_at;
                    row.end_at = input.end_at;
                    row.usage_limit = input.usage_limit;
                    row.is_active = input.is_active;
                    row.updated_at = DateTime.Now;
                    if (Save())
                    {
                        tx.Commit();
                        return RedirectToAction("Index");
                    }
                }
            }
            return FormView("CouponEdit", input);
        }

        private ActionResult FormView(string view, coupon model)
        {
            // Một số bộ dữ liệu dùng Amount thay cho Fixed. Giữ loại tiền đang có trong CSDL.
            var existing = db.coupons.AsNoTracking().Select(c => c.discount_type).Distinct().ToList();
            var fixedTypes = existing.Where(t => string.Equals(t, "Fixed", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(t, "Amount", StringComparison.OrdinalIgnoreCase)).ToList();
            if (fixedTypes.Count == 0) fixedTypes.Add("Fixed");
            var values = new[] { new SelectListItem { Value = "Percent", Text = "Phần trăm (%)" } }
                .Concat(fixedTypes.Select(t => new SelectListItem { Value = t, Text = "Số tiền (" + t + ")" })).ToList();
            ViewBag.DiscountTypes = new SelectList(values, "Value", "Text", model.discount_type);
            return View("~/Views/Admin/" + view + ".cshtml", model);
        }

        private void ValidateCoupon(coupon input, int id)
        {
            input.code = (input.code ?? "").Trim().ToUpperInvariant();
            if (input.code.Length == 0 || input.code.Length > 30)
                ModelState.AddModelError("code", "Mã giảm giá cần từ 1 đến 30 ký tự.");
            else if (db.coupons.Any(c => c.coupon_id != id && c.code == input.code))
                ModelState.AddModelError("code", "Mã giảm giá đã tồn tại.");

            bool percent = string.Equals(input.discount_type, "Percent", StringComparison.OrdinalIgnoreCase);
            bool fixedAmount = string.Equals(input.discount_type, "Fixed", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(input.discount_type, "Amount", StringComparison.OrdinalIgnoreCase);
            if (!percent && !fixedAmount) ModelState.AddModelError("discount_type", "Loại giảm giá không hợp lệ.");
            if (input.discount_value <= 0 || (percent && input.discount_value > 100))
                ModelState.AddModelError("discount_value", "Mức giảm phải dương và tỷ lệ phần trăm tối đa là 100.");
            if (input.min_order_value < 0 || input.max_discount < 0)
                ModelState.AddModelError("", "Giá trị đơn tối thiểu và mức giảm tối đa phải từ 0 trở lên.");
            if (!FitsMoney(input.discount_value) || !FitsMoney(input.min_order_value) || !FitsMoney(input.max_discount))
                ModelState.AddModelError("", "Số tiền được phép tối đa 16 chữ số nguyên và 2 chữ số thập phân.");
            if (input.start_at.Year < 1753 || input.end_at.Year < 1753 || input.end_at <= input.start_at)
                ModelState.AddModelError("end_at", "Hãy chọn thời điểm kết thúc sau thời điểm bắt đầu.");
            if (input.usage_limit.HasValue && input.usage_limit.Value < (input.used_count ?? 0))
                ModelState.AddModelError("usage_limit", "Giới hạn không thể âm hoặc thấp hơn số lượt đã dùng.");
        }

        private static bool FitsMoney(decimal? value)
        {
            return !value.HasValue || (value.Value <= 9999999999999999.99m && decimal.Round(value.Value, 2) == value.Value);
        }

        private bool Save()
        {
            try { db.SaveChanges(); return true; }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Không lưu được mã. Hãy kiểm tra mã trùng và điều kiện của bảng coupons.");
                return false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
