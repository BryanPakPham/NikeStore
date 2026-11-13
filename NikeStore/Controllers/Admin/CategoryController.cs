using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web.Mvc;
using NikeStore.Infrastructure;
using NikeStore.Models;

namespace NikeStore.Controllers.Admin
{
    [AdminOnly]
    public class CategoryController : Controller
    {
        private readonly NikeStoreDBEntities db = new NikeStoreDBEntities();

        public ActionResult Categories()
        {
            return View("~/Views/Admin/Categories.cshtml",
                db.categories.OrderBy(c => c.display_order).ThenBy(c => c.category_name).ToList());
        }

        [HttpGet]
        public ActionResult CategoryCreate()
        {
            return View("~/Views/Admin/CategoryCreate.cshtml", new category());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult CategoryCreate([Bind(Include = "category_name,slug,description,display_order")] category input)
        {
            ValidateCategory(input, 0);
            if (ModelState.IsValid)
            {
                input.is_active = true;
                input.created_at = input.updated_at = DateTime.Now;
                db.categories.Add(input);
                if (Save()) return RedirectToAction("Categories");
            }
            return View("~/Views/Admin/CategoryCreate.cshtml", input);
        }

        [HttpGet]
        public ActionResult CategoryEdit(int id)
        {
            var row = db.categories.Find(id);
            if (row == null) return HttpNotFound();
            return View("~/Views/Admin/CategoryEdit.cshtml", row);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult CategoryEdit([Bind(Include = "category_id,category_name,slug,description,display_order,is_active")] category input)
        {
            var row = db.categories.Find(input.category_id);
            if (row == null) return HttpNotFound();
            ValidateCategory(input, row.category_id);
            if (ModelState.IsValid)
            {
                row.category_name = input.category_name;
                row.slug = input.slug;
                row.description = input.description;
                row.display_order = input.display_order;
                if (input.is_active.HasValue) row.is_active = input.is_active;
                row.updated_at = DateTime.Now;
                if (Save()) return RedirectToAction("Categories");
            }
            return View("~/Views/Admin/CategoryEdit.cshtml", input);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Deactivate(int id)
        {
            var row = db.categories.Find(id);
            if (row == null) return HttpNotFound();
            row.is_active = false;
            row.updated_at = DateTime.Now;
            if (!Save()) TempData["Error"] = "Không thể ngừng danh mục. Hãy tải lại danh sách.";
            return RedirectToAction("Categories");
        }

        private void ValidateCategory(category input, int id)
        {
            input.category_name = (input.category_name ?? "").Trim();
            input.slug = (input.slug ?? "").Trim();
            if (input.category_name.Length == 0 || input.category_name.Length > 100)
                ModelState.AddModelError("category_name", "Tên danh mục cần từ 1 đến 100 ký tự.");
            if (input.slug.Length == 0 || input.slug.Length > 120)
                ModelState.AddModelError("slug", "Slug cần từ 1 đến 120 ký tự.");
            else if (db.categories.Any(c => c.category_id != id && c.slug == input.slug))
                ModelState.AddModelError("slug", "Slug này đã thuộc về một danh mục khác.");
            if (input.description != null && input.description.Length > 500)
                ModelState.AddModelError("description", "Mô tả tối đa 500 ký tự.");
        }

        private bool Save()
        {
            try { db.SaveChanges(); return true; }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Không lưu được danh mục. Hãy kiểm tra slug và tải lại nếu dữ liệu vừa thay đổi.");
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
