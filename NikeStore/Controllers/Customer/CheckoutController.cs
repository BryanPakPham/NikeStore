using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using NikeStore.Models;
namespace NikeStore.Controllers.Customer
{
    public class CheckoutController : Controller
    {
        public NikeStoreDBEntities data = new NikeStoreDBEntities();
        // Hiển thị trang thanh toán
        public ActionResult Index()
        {
            var gioHang = Session["GioHang"] as Dictionary<int, int>;
            if (gioHang == null || gioHang.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }
            var danhSachId = gioHang.Keys.ToList();
            // Lấy thông tin sản phẩm và size
            var danhSachSanPham = data.product_variants
                .Include("product")
                .Include("size")
                .Where(v => danhSachId.Contains(v.variant_id))
                .ToList();
            // Lấy thông tin màu sắc
            var danhSachMauId = danhSachSanPham
                .Select(v => v.product_color_id)
                .Distinct()
                .ToList();
            // Lấy ảnh sản phẩm
            ViewBag.DanhSachAnh = data.product_images
                .Where(a => danhSachMauId.Contains(a.product_color_id) && a.image_angle == "Side")
                .ToList();
            ViewBag.GioHang = gioHang;
            ViewBag.DanhSachMau = data.product_colors
                .Include("color")
                .Where(m => danhSachMauId.Contains(m.product_color_id))
                .ToList();
            // Tính tổng tiền
            decimal tongTien = 0;
            foreach (var item in danhSachSanPham)
            {
                tongTien += item.product.price * gioHang[item.variant_id];
            }
            ViewBag.TongTien = tongTien;
            Session["CartCount"] = gioHang.Values.Sum();
            return View("~/Views/Customer/Checkout.cshtml", danhSachSanPham);
        }
    }
}