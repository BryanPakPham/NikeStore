using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NikeStore.Models;

namespace NikeStore.Controllers.Customer
{
    public class HomeController : Controller
    {
        public NikeStoreDBEntities data = new NikeStoreDBEntities();
        // GET: Home
        public ActionResult Index()
        {
            var sanPhamNoiBat = data.products
                .Where(p => p.is_active == true && p.is_featured == true)
                .Take(4)
                .ToList();
            var danhSachId = sanPhamNoiBat.Select(p => p.product_id).ToList();
            var danhSachAnh = data.product_images
                .Where(i => danhSachId.Contains(i.product_id) && i.image_angle == "Side")
                .ToList();
            ViewBag.DanhSachAnh = danhSachAnh;
            return View("~/Views/Customer/Home.cshtml", sanPhamNoiBat);
        }
        public ActionResult Contact_About()
        {
            return View("~/Views/Customer/Contact_About.cshtml");
        }
    }
}