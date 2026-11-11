using Admin_doan.Models;//sửa lại đường dẫn của model
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Admin_doan.Controllers.Admin //sửa lại namespace theo cấu trúc đồ án.
{
    public class InventoryController : Controller
    {
        private NikeStoreDBEntities db = new NikeStoreDBEntities();

        // Danh sách tồn kho
        public ActionResult Inventory()
        {
            var variants = db.product_variants
                .Include("products")
                .Include("sizes")
                .OrderBy(v => v.products.product_name)
                .ToList();

            return View("~/Views/Admin/Inventory.cshtml", variants);
        }

        // Nhập kho hoặc điều chỉnh kho
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStock(
            int variant_id,
            int quantity_change,
            string transaction_type,
            string note)
        {
            if (quantity_change == 0)
            {
                TempData["Error"] = "Số lượng thay đổi phải khác 0.";
                return RedirectToAction("Inventory");
            }

            if (transaction_type != "Import" &&
                transaction_type != "Adjustment")
            {
                TempData["Error"] = "Loại giao dịch không hợp lệ.";
                return RedirectToAction("Inventory");
            }

            var variant = db.product_variants.Find(variant_id);

            if (variant == null)
            {
                TempData["Error"] = "Không tìm thấy phiên bản sản phẩm.";
                return RedirectToAction("Inventory");
            }

            int currentStock = variant.stock_quantity ?? 0;
            int newStock = currentStock + quantity_change;

            if (newStock < 0)
            {
                TempData["Error"] = "Tồn kho không thể nhỏ hơn 0.";
                return RedirectToAction("Inventory");
            }

            if (transaction_type == "Import" && quantity_change < 0)
            {
                TempData["Error"] = "Nhập kho phải có số lượng dương.";
                return RedirectToAction("Inventory");
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // Cập nhật số lượng tồn kho
                    variant.stock_quantity = newStock;

                    // Ghi nhận lịch sử kho
                    inventory_transactions history =
                        new inventory_transactions();

                    history.variant_id = variant_id;

                    history.user_id = Session["UserId"] == null
                        ? (int?)null
                        : Convert.ToInt32(Session["UserId"]);

                    history.transaction_type = transaction_type;
                    history.quantity_change = quantity_change;
                    history.note = note;
                    history.created_at = DateTime.Now;

                    db.inventory_transactions.Add(history);

                    db.SaveChanges();

                    transaction.Commit();

                    TempData["Success"] = "Cập nhật tồn kho thành công.";
                }
                catch (Exception)
                {
                    transaction.Rollback();

                    TempData["Error"] =
                        "Có lỗi xảy ra khi cập nhật tồn kho.";
                }
            }

            return RedirectToAction("Inventory");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
