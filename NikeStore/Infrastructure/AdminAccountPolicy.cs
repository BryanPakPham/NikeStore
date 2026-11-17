using System;

namespace NikeStore.Infrastructure
{
    public static class AdminAccountPolicy
    {
        public static string ValidateChange(int actorId, int targetId, bool wasActiveAdmin,
            string newRole, bool active, int activeAdminCount)
        {
            bool keepsAdmin = active && string.Equals(newRole, "Admin", StringComparison.OrdinalIgnoreCase);
            if (actorId == targetId && !keepsAdmin)
                return "Bạn không thể tự khóa hoặc tự gỡ quyền quản trị của tài khoản đang đăng nhập.";
            if (wasActiveAdmin && !keepsAdmin && activeAdminCount <= 1)
                return "Hệ thống phải có ít nhất một quản trị viên đang hoạt động.";
            return null;
        }

        public static string RoleLabel(string role)
        {
            if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)) return "Quản trị viên";
            if (string.Equals(role, "Employee", StringComparison.OrdinalIgnoreCase)) return "Nhân viên";
            if (string.Equals(role, "Customer", StringComparison.OrdinalIgnoreCase)) return "Khách hàng";
            return role ?? "Chưa có vai trò";
        }

        public static int PageCount(int total, int pageSize)
        {
            if (pageSize < 1) throw new ArgumentOutOfRangeException("pageSize");
            return Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        }
    }
}
