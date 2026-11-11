using Admin_doan.Models;//sửa lại đường dẫn của model
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Admin_doan.Controllers.Admin //sửa lại namespace theo cấu trúc đồ án.
{
    public class ProductController : Controller
    {
        // GET: Product
        private NikeStoreDBEntities db = new NikeStoreDBEntities();

        // Danh sách sản phẩm
        public ActionResult Products(string search)
        {
            var query = db.products
                .Include("categories")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    p.product_name.Contains(search) ||
                    p.product_code.Contains(search));
            }

            ViewBag.Search = search;

            return View("~/Views/Admin/Products.cshtml",
                query.OrderByDescending(p => p.product_id).ToList());
        }

        // Chi tiết sản phẩm
        public ActionResult ProductDetails(int id)
        {
            var product = db.products
                .Include("categories")
                .FirstOrDefault(p => p.product_id == id);

            if (product == null)
                return HttpNotFound();

            ViewBag.Variants = db.product_variants
                .Include("products")
                .Include("sizes")
                .Where(v => v.product_id == id)
                .ToList();

            ViewBag.Images = db.product_images
                .Where(i => i.product_id == id)
                .OrderBy(i => i.display_order)
                .ToList();

            return View("~/Views/Admin/ProductDetails.cshtml", product);
        }

        // GET: Thêm sản phẩm
        [HttpGet]
        public ActionResult ProductCreate()
        {
            ViewBag.Categories = new SelectList(
                db.categories.Where(c => c.is_active == true).ToList(),
                "category_id", "category_name");

            return View("~/Views/Admin/ProductCreate.cshtml");
        }

        // POST: Thêm sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProductCreate(products product, int[] categoryIds)
        {
            if (ModelState.IsValid)
            {
                product.created_at = DateTime.Now;
                product.updated_at = DateTime.Now;
                product.is_active = true;
                product.is_featured = false;

                if (categoryIds != null)
                {
                    var categories = db.categories
                        .Where(c => categoryIds.Contains(c.category_id))
                        .ToList();

                    foreach (var category in categories)
                        product.categories.Add(category);
                }

                db.products.Add(product);
                db.SaveChanges();

                return RedirectToAction("Products");
            }

            ViewBag.Categories = new SelectList(
                db.categories.Where(c => c.is_active == true).ToList(),
                "category_id", "category_name");

            return View("~/Views/Admin/ProductCreate.cshtml", product);
        }

        // GET: Sửa sản phẩm
        [HttpGet]
        public ActionResult ProductEdit(int id)
        {
            var product = db.products
                .Include("categories")
                .FirstOrDefault(p => p.product_id == id);

            if (product == null)
                return HttpNotFound();

            ViewBag.Categories = new MultiSelectList(
                db.categories.ToList(),
                "category_id",
                "category_name",
                product.categories.Select(c => c.category_id));

            return View("~/Views/Admin/ProductEdit.cshtml", product);
        }

        // POST: Sửa sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProductEdit(products model, int[] categoryIds)
        {
            var product = db.products
                .Include("categories")
                .FirstOrDefault(p => p.product_id == model.product_id);

            if (product == null)
                return HttpNotFound();

            if (ModelState.IsValid)
            {
                product.product_code = model.product_code;
                product.product_name = model.product_name;
                product.slug = model.slug;
                product.description = model.description;
                product.price = model.price;
                product.is_featured = model.is_featured;
                product.updated_at = DateTime.Now;

                product.categories.Clear();

                if (categoryIds != null)
                {
                    var categories = db.categories
                        .Where(c => categoryIds.Contains(c.category_id))
                        .ToList();

                    foreach (var category in categories)
                        product.categories.Add(category);
                }

                db.SaveChanges();

                return RedirectToAction("Products");
            }

            ViewBag.Categories = new MultiSelectList(
                db.categories.ToList(),
                "category_id", "category_name",
                categoryIds);

            return View("~/Views/Admin/ProductEdit.cshtml", product);
        }

        // Ngừng kinh doanh sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Deactivate(int id)
        {
            var product = db.products.Find(id);

            if (product == null)
                return HttpNotFound();

            product.is_active = false;
            product.updated_at = DateTime.Now;

            db.SaveChanges();

            return RedirectToAction("Products");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
