using System;
using System.Linq;
using System.Collections.Generic;
using System.Web.Mvc;
using NikeStore.Models;
namespace NikeStore.Controllers.Customer
{
    public class CartController : Controller
    {
        public NikeStoreDBEntities data = new NikeStoreDBEntities();

        // Hiển thị giỏ hàng
        public ActionResult Index()
        {
            var gioHang = Session["GioHang"] as Dictionary<int, int>;
            if (gioHang == null || gioHang.Count == 0)
            {
                ViewBag.GioHang = new Dictionary<int, int>();
                ViewBag.DanhSachMau = new List<product_colors>();
                ViewBag.DanhSachAnh = new List<product_images>();
                return View("~/Views/Customer/Cart.cshtml", new List<product_variants>());
            }
            var danhSachId = gioHang.Keys.ToList();
            // Lấy thông tin sản phẩm, size
            var danhSachSanPham = data.product_variants
                .Include("product")
                .Include("size")
                .Where(v => danhSachId.Contains(v.variant_id))
                .ToList();
            // Lấy thông tin màu
            var danhSachMauId = danhSachSanPham
                .Select(v => v.product_color_id)
                .Distinct()
                .ToList();
            ViewBag.GioHang = gioHang;
            ViewBag.DanhSachMau = data.product_colors
                .Include("color")
                .Where(m => danhSachMauId.Contains(m.product_color_id))
                .ToList();
            // Lấy ảnh Side
            ViewBag.DanhSachAnh = data.product_images
                .Where(a => danhSachMauId.Contains(a.product_color_id) && a.image_angle == "Side")
                .ToList();
            Session["CartCount"] = gioHang.Values.Sum();
            return View("~/Views/Customer/Cart.cshtml", danhSachSanPham);
        }

        // Thêm sản phẩm vào giỏ hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddToCart(int productId, int productColorId, int sizeId)
        {
            // Tìm đúng sản phẩm, màu và size
            var bienThe = data.product_variants.FirstOrDefault(v =>
                v.product_id == productId &&
                v.product_color_id == productColorId &&
                v.size_id == sizeId &&
                v.is_active == true);
            if (bienThe == null || (bienThe.stock_quantity ?? 0) <= 0)
            {
                return RedirectToAction("Details", "Product", new { id = productId });
            }
            // Lấy giỏ hàng từ Session
            var gioHang = Session["GioHang"] as Dictionary<int, int>;
            if (gioHang == null)
            {
                gioHang = new Dictionary<int, int>();
            }
            // Thêm hoặc tăng số lượng
            int soLuong = gioHang.ContainsKey(bienThe.variant_id) ? gioHang[bienThe.variant_id] : 0;
            if (soLuong < (bienThe.stock_quantity ?? 0))
            {
                gioHang[bienThe.variant_id] = soLuong + 1;
            }
            // Lưu giỏ hàng và cập nhật số lượng
            Session["GioHang"] = gioHang;
            Session["CartCount"] = gioHang.Values.Sum();
            return RedirectToAction("Details", "Product", new { id = productId });
        }
    }
}