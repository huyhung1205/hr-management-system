using System.ComponentModel.DataAnnotations;

namespace human_resource_management.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Tên tài khoản không được để trống")]
        [Display(Name = "Tên tài khoản")]
        public string TenTK { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string MatKhau { get; set; }

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
    }
}
