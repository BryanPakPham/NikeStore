using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using NikeStore.Infrastructure;
using NikeStore.Models;
using NikeStore.ViewModels.Admin;

namespace NikeStore.Controllers.Admin
{
    [AdminOnly]
    public class InventoryController : Controller
    {
        private const int HistoryPageSize = 25;
        private readonly NikeStoreDBEntities db = new NikeStoreDBEntities();

        public ActionResult Inventory(string q, int? variantId, int page = 1)
        {
            q = (q ?? "").Trim();
            var variants = from v in db.product_variants
                           join pc in db.product_colors on v.product_color_id equals pc.product_color_id into matches
                           from pc in matches.DefaultIfEmpty()
                           select new VariantRow
                           {
                               Id = v.variant_id, Sku = v.sku, Product = v.product.product_name,
                               Color = pc.color.color_name, Size = v.size.size_value,
                               SizeSystem = v.size.size_system, Stock = v.stock_quantity ?? 0
                           };
            if (q.Length > 0)
                variants = variants.Where(v => v.Sku.Contains(q) || v.Product.Contains(q) || v.Color.Contains(q));
            var history = db.inventory_transactions.AsNoTracking()
                .Include(t => t.product_variants).Include(t => t.user).Include(t => t.order)
                .Where(t => variants.Any(v => v.Id == t.variant_id));
            if (variantId.HasValue) history = history.Where(t => t.variant_id == variantId.Value);
            int pages = Math.Max(1, (int)Math.Ceiling(history.Count() / (double)HistoryPageSize));
            page = Math.Max(1, Math.Min(pages, page));
            return View("~/Views/Admin/Inventory.cshtml", new InventoryPage
            {
                Query = q, VariantId = variantId, Page = page, Pages = pages,
                Variants = variants.OrderBy(v => v.Product).ThenBy(v => v.Color).ThenBy(v => v.Size).ThenBy(v => v.Id).ToList(),
                History = history.OrderByDescending(t => t.created_at).ThenByDescending(t => t.transaction_id)
                    .Skip((page - 1) * HistoryPageSize).Take(HistoryPageSize).ToList()
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult UpdateStock(int variant_id, int quantity_change, string transaction_type, string note, string q)
        {
            note = (note ?? "").Trim();
            if (!ModelState.IsValid || quantity_change == 0 || note.Length > 255 ||
                (transaction_type != "Import" && transaction_type != "Adjustment") ||
                (transaction_type == "Import" && quantity_change < 0))
            {
                TempData["Error"] = "Nhập kho cần số dương. Điều chỉnh cần số khác 0. Ghi chú tối đa 255 ký tự.";
                return RedirectToAction("Inventory", new { q, variantId = variant_id });
            }

            try
            {
                using (var tx = db.Database.BeginTransaction())
                {
                    // Một UPDATE có điều kiện để không ghi đè thao tác bán, hoàn hoặc nhập kho đồng thời.
                    int changed = db.Database.ExecuteSqlCommand(
                        "UPDATE dbo.product_variants SET stock_quantity = CONVERT(int, CONVERT(bigint, ISNULL(stock_quantity, 0)) + @p0) " +
                        "WHERE variant_id = @p1 AND CONVERT(bigint, ISNULL(stock_quantity, 0)) + @p0 BETWEEN 0 AND 2147483647",
                        quantity_change, variant_id);
                    if (changed != 1)
                    {
                        TempData["Error"] = "Không tìm thấy biến thể hoặc số lượng mới nằm ngoài giới hạn tồn kho.";
                        return RedirectToAction("Inventory", new { q, variantId = variant_id });
                    }
                    db.inventory_transactions.Add(new inventory_transactions
                    {
                        variant_id = variant_id, user_id = Convert.ToInt32(Session["UserID"]),
                        transaction_type = transaction_type, quantity_change = quantity_change,
                        note = note.Length == 0 ? null : note, created_at = DateTime.Now
                    });
                    db.SaveChanges();
                    tx.Commit();
                    TempData["Success"] = "Đã cập nhật tồn kho và ghi lịch sử.";
                }
            }
            catch (DbUpdateException) { TempData["Error"] = "Không lưu được giao dịch kho. Hãy kiểm tra lại lịch sử."; }
            catch (SqlException) { TempData["Error"] = "Không cập nhật được kho. Hãy tải lại số lượng hiện tại."; }
            return RedirectToAction("Inventory", new { q, variantId = variant_id });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
