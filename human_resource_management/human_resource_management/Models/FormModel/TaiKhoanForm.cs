using System.ComponentModel.DataAnnotations;

namespace human_resource_management.Models.FormModel
{
    public class TaiKhoanForm
    {
        [Required(ErrorMessage = "Mã nhân viên không được để trống")]
        [Display(Name = "Mã nhân viên")]
        public int maNV { get; set; }

        [Required(ErrorMessage = "Tên tài khoản không được để trống")]
        [StringLength(50, ErrorMessage = "Tên tài khoản không quá 50 ký tự")]
        [Display(Name = "Tên tài khoản")]
        public string tenTK { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 255 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string matKhau { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vai trò")]
        [StringLength(50)]
        [Display(Name = "Vai trò")]
        public string vaiTro { get; set; }
    }
}