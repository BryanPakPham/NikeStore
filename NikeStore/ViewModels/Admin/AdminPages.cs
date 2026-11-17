using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace NikeStore.ViewModels.Admin
{
    public class AdminSettings
    {
        public AdminSettings()
        {
            DisplayName = "NikeStore Admin";
            LowStockThreshold = 10;
            RowsPerPage = 20;
        }

        [Required(ErrorMessage = "Vui lòng nhập tên khu vực quản trị.")]
        [StringLength(60, ErrorMessage = "Tên khu vực quản trị tối đa 60 ký tự.")]
        public string DisplayName { get; set; }

        [Range(0, 1000, ErrorMessage = "Ngưỡng tồn kho phải từ 0 đến 1000.")]
        public int LowStockThreshold { get; set; }

        [Range(10, 50, ErrorMessage = "Số tài khoản mỗi trang phải từ 10 đến 50.")]
        public int RowsPerPage { get; set; }
    }

    public class UserListRow
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }
        public bool Active { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class UserListPage
    {
        public List<UserListRow> Items { get; set; }
        public List<SelectListItem> Roles { get; set; }
        public string Query { get; set; }
        public int? RoleId { get; set; }
        public bool? Active { get; set; }
        public bool EmployeesOnly { get; set; }
        public int Page { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int CurrentUserId { get; set; }
    }

    public class RoleSummary
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Accounts { get; set; }
        public int ActiveAccounts { get; set; }
    }

    public class AdminOrderRow
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Recipient { get; set; }
        public DateTime? Date { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; }
    }

    public class AdminStockRow
    {
        public string Sku { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
    }

    public class AdminDashboard
    {
        public int ActiveProducts { get; set; }
        public int ActiveEmployees { get; set; }
        public int TotalCustomers { get; set; }
        public int PendingOrders { get; set; }
        public int LowStockCount { get; set; }
        public int LowStockThreshold { get; set; }
        public decimal CompletedRevenueThisMonth { get; set; }
        public List<AdminOrderRow> RecentOrders { get; set; }
        public List<AdminStockRow> LowStock { get; set; }
    }
}
