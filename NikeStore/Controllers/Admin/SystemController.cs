using System.IO;
using System;
using System.Web.Mvc;
using NikeStore.Infrastructure;
using NikeStore.ViewModels.Admin;

namespace NikeStore.Controllers.Admin
{
    [AdminOnly]
    public class SystemController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            return View("~/Views/Admin/System.cshtml", AdminSettingsStore.Current().Load());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Index(AdminSettings model)
        {
            model.DisplayName = (model.DisplayName ?? "").Trim();
            if (model.DisplayName.Length == 0)
                ModelState.AddModelError("DisplayName", "Vui lòng nhập tên khu vực quản trị.");
            if (ModelState.IsValid)
            {
                try
                {
                    AdminSettingsStore.Current().Save(model);
                    TempData["Success"] = "Đã lưu cấu hình quản trị.";
                    return RedirectToAction("Index");
                }
                catch (IOException) { ModelState.AddModelError("", "Chưa lưu được cấu hình. Vui lòng thử lại."); }
                catch (UnauthorizedAccessException) { ModelState.AddModelError("", "Ứng dụng cần quyền ghi thư mục App_Data để lưu cấu hình."); }
            }
            return View("~/Views/Admin/System.cshtml", model);
        }
    }
}
