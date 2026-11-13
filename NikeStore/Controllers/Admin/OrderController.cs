using NikeStore.Infrastructure;

namespace NikeStore.Controllers.Admin
{
    [AdminOnly]
    public class OrderController : NikeStore.Controllers.Employee.OrderController
    {
        // Hai khu vực dùng cùng giao dịch xác nhận, trừ/hoàn kho và cập nhật thanh toán.
        protected override string OrderModule { get { return "Admin"; } }
    }
}
