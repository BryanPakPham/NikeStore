using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NikeStore.Models;

namespace NikeStore.Controllers.Customer
{
    public class OrderController : Controller
    {
        public NikeStoreDBEntities data = new NikeStoreDBEntities();
        // Xác nhận đặt hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PlaceOrder(string recipientName, string recipientPhone, string shippingAddress, string note, string paymentMethod)
        {
            var gioHang = Session["GioHang"] as Dictionary<int, int>;

            if (gioHang == null || gioHang.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            // Lấy thông tin sản phẩm trong giỏ
            var danhSachId = gioHang.Keys.ToList();

            var danhSachSanPham = data.product_variants
                .Include("product")
                .Include("size")
                .Where(v => danhSachId.Contains(v.variant_id))
                .ToList();

            // Tính tổng tiền
            decimal tongTien = 0;

            foreach (var item in danhSachSanPham)
            {
                tongTien += item.product.price * gioHang[item.variant_id];
            }

            // Tạo đơn hàng
            order donHang = new order();

            donHang.order_code = "NK" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            donHang.customer_id = Session["UserID"] as int?;
            donHang.order_date = DateTime.Now;
            donHang.recipient_name = recipientName;
            donHang.recipient_phone = recipientPhone;
            donHang.shipping_address = shippingAddress;
            donHang.subtotal = tongTien;
            donHang.discount_amount = 0;
            donHang.shipping_fee = 0;
            donHang.total_amount = tongTien;
            donHang.order_status = "Pending";
            donHang.payment_method = paymentMethod;
            donHang.payment_status = "Unpaid";
            donHang.note = note;
            donHang.updated_at = DateTime.Now;

            data.orders.Add(donHang);
            data.SaveChanges();

            // Lưu chi tiết đơn hàng
            foreach (var item in danhSachSanPham)
            {
                var mau = data.product_colors
                    .Include("color")
                    .FirstOrDefault(m => m.product_color_id == item.product_color_id);

                order_details chiTiet = new order_details();

                chiTiet.order_id = donHang.order_id;
                chiTiet.variant_id = item.variant_id;
                chiTiet.product_name = item.product.product_name;
                chiTiet.size_value = item.size.size_value;
                chiTiet.color_name = mau != null && mau.color != null
                    ? mau.color.color_name : "";
                chiTiet.unit_price = item.product.price;
                chiTiet.quantity = gioHang[item.variant_id];

                data.order_details.Add(chiTiet);
            }

            data.SaveChanges();

            // Xóa giỏ hàng sau khi lưu thành công
            Session["GioHang"] = null;
            Session["CartCount"] = 0;

            TempData["ThongBao"] = "Your order has been placed successfully!";

            return Redirect("~/Customer/Home/Index");
        }
    }
}