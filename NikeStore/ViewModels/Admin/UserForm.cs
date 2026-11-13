using System.ComponentModel.DataAnnotations;

namespace NikeStore.ViewModels.Admin
{
    public class UserForm
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên."), StringLength(100)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress(ErrorMessage = "Email không hợp lệ."), StringLength(254)]
        public string Email { get; set; }

        [StringLength(20)]
        public string Phone { get; set; }

        [StringLength(300)]
        public string Address { get; set; }

        public int RoleId { get; set; }
        public bool Active { get; set; }
    }
}
