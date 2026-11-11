using Admin_doan.Models;//sửa lại đường dẫn của model
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Admin_doan.Controllers.Admin //sửa lại namespace theo cấu trúc đồ án.
{
    public class CategoryController : Controller
    {
        private NikeStoreDBEntities db = new NikeStoreDBEntities();

        // Danh sách danh mục
        public ActionResult Categories()
        {
            var categories = db.categories
                .OrderBy(c => c.display_order)
                .ToList();

            return View("~/Views/Admin/Categories.cshtml", categories);
        }

        // GET: Thêm danh mục
        [HttpGet]
        public ActionResult CategoryCreate()
        {
            return View("~/Views/Admin/CategoryCreate.cshtml");
        }

        // POST: Thêm danh mục
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CategoryCreate(categories model)
        {
            if (ModelState.IsValid)
            {
                model.created_at = DateTime.Now;
                model.updated_at = DateTime.Now;
                model.is_active = true;

                db.categories.Add(model);
                db.SaveChanges();

                return RedirectToAction("Categories");
            }

            return View("~/Views/Admin/CategoryCreate.cshtml", model);
        }

        // GET: Sửa danh mục
        [HttpGet]
        public ActionResult CategoryEdit(int id)
        {
            var category = db.categories.Find(id);

            if (category == null)
                return HttpNotFound();

            return View("~/Views/Admin/CategoryEdit.cshtml", category);
        }

        // POST: Sửa danh mục
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CategoryEdit(categories model)
        {
            var category = db.categories.Find(model.category_id);

            if (category == null)
                return HttpNotFound();

            if (ModelState.IsValid)
            {
                category.category_name = model.category_name;
                category.slug = model.slug;
                category.description = model.description;
                category.display_order = model.display_order;
                category.updated_at = DateTime.Now;

                db.SaveChanges();

                return RedirectToAction("Categories");
            }

            return View("~/Views/Admin/CategoryEdit.cshtml", model);
        }

        // Ngừng hoạt động danh mục
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Deactivate(int id)
        {
            var category = db.categories.Find(id);

            if (category == null)
                return HttpNotFound();

            category.is_active = false;
            category.updated_at = DateTime.Now;

            db.SaveChanges();

            return RedirectToAction("Categories");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
