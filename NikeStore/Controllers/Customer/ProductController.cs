using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NikeStore.Models;
namespace NikeStore.Controllers.Customer
{
    public class ProductController : Controller
    {
        public NikeStoreDBEntities data = new NikeStoreDBEntities();
        public ActionResult Index(decimal? minPrice, decimal? maxPrice, string categoryName)
        {
            var sanPham = data.products.Where(p => p.is_active == true);
            // Lọc theo danh mục
            if (!string.IsNullOrEmpty(categoryName))
            {
                sanPham = sanPham.Where(p => p.categories.Any(c => c.category_name == categoryName));
            }
            // Lọc theo giá
            if (minPrice.HasValue)
            {
                sanPham = sanPham.Where(p => p.price >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                sanPham = sanPham.Where(p => p.price <= maxPrice.Value);
            }
            var danhSachSanPham = sanPham.ToList();
            // Lấy ảnh Side của sản phẩm
            var danhSachId = danhSachSanPham.Select(p => p.product_id).ToList();
            ViewBag.DanhSachAnh = data.product_images
                .Where(i => danhSachId.Contains(i.product_id) && i.image_angle == "Side")
                .ToList();
            return View("~/Views/Customer/Products.cshtml", danhSachSanPham);
        }

        // Hiển thị danh sách Collection
        public ActionResult Collections()
        {
            var danhSachDanhMuc = data.categories
                .Include("products")
                .Where(c => c.is_active == true && c.products.Any(p => p.is_active == true))
                .OrderBy(c => c.display_order)
                .ToList();
            var danhSachId = danhSachDanhMuc
                .SelectMany(c => c.products.Where(p => p.is_active == true))
                .Select(p => p.product_id)
                .Distinct()
                .ToList();
            ViewBag.DanhSachAnh = data.product_images
                .Where(i => danhSachId.Contains(i.product_id) && i.image_angle == "Side")
                .ToList();
            return View("~/Views/Customer/Collections.cshtml", danhSachDanhMuc);
        }

        // Hiển thị sản phẩm thuộc Collection được chọn
        public ActionResult CollectionProducts(string collectionName)
        {
            var danhSachSanPham = data.products
                .Where(p => p.is_active == true && p.product_name.Contains(collectionName))
                .ToList();
            var danhSachId = danhSachSanPham.Select(p => p.product_id).ToList();
            ViewBag.DanhSachAnh = data.product_images
                .Where(i => danhSachId.Contains(i.product_id) && i.image_angle == "Side")
                .ToList();
            ViewBag.CollectionName = collectionName;
            return View("~/Views/Customer/CollectionProducts.cshtml", danhSachSanPham);
        }
        // Xem chi tiết sản phẩm
        public ActionResult Details(int id)
        {
            var sanPham = data.products.FirstOrDefault(p => p.product_id == id && p.is_active == true);
            if (sanPham == null)
            {
                return HttpNotFound();
            }
            // Lấy danh sách màu
            var danhSachMau = data.product_colors
                .Include("color")
                .Where(m => m.product_id == id && m.is_active == true)
                .ToList();
            // Lấy ảnh Side và Top của từng màu
            var danhSachAnh = data.product_images
                .Where(a => a.product_id == id && (a.image_angle == "Side" || a.image_angle == "Top"))
                .ToList();
            // Lấy kích thước còn hàng
            var danhSachSize = data.product_variants
                .Include("size")
                .Where(v => v.product_id == id && v.is_active == true && v.stock_quantity > 0)
                .ToList();
            ViewBag.DanhSachMau = danhSachMau;
            ViewBag.DanhSachAnh = danhSachAnh;
            ViewBag.DanhSachSize = danhSachSize;
            return View("~/Views/Customer/ProductDetails.cshtml", sanPham);
        }
    }
}