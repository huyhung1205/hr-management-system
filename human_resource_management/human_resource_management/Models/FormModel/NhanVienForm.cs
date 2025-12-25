using System;
using System.ComponentModel.DataAnnotations;

namespace human_resource_management.Models.FormModel
{
    public class NhanVienForm
    {
        public int maNV { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string hoTen { get; set; }

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? ngaySinh { get; set; }

        [Display(Name = "Giới tính")]
        public bool? gioiTinh { get; set; }

        [StringLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự")]
        [Display(Name = "Địa chỉ")]
        public string diaChi { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [StringLength(15, ErrorMessage = "Số điện thoại không quá 15 số")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string dienThoai { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không quá 100 ký tự")]
        [Display(Name = "Email")]
        public string email { get; set; }

        [Required(ErrorMessage = "Chức vụ không được để trống")]
        [StringLength(100, ErrorMessage = "Chức vụ không quá 100 ký tự")]
        [Display(Name = "Chức vụ")]
        public string chucVu { get; set; }

        [StringLength(50)]
        [Display(Name = "Trạng thái làm việc")]
        public string trangThaiLamViec { get; set; }

        [Display(Name = "Ngày vào làm")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? ngayVaoLam { get; set; }

        [Display(Name = "Ngày kết thúc")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? ngayKetThucLam { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phòng ban")]
        [Display(Name = "Phòng ban")]
        public int? maPB { get; set; }

        // --- Thông tin hợp đồng đi kèm ---
        [Required(ErrorMessage = "Vui lòng chọn loại hợp đồng")]
        [Display(Name = "Loại hợp đồng")]
        public byte loaiHD { get; set; }

        [Required(ErrorMessage = "Hệ số lương không được để trống")]
        [Range(1, 10, ErrorMessage = "Hệ số lương từ 1 đến 10")]
        [Display(Name = "Hệ số lương")]
        public decimal heSoLuong { get; set; }

        [Required(ErrorMessage = "Lương cơ bản không được để trống")]
        [Range(0, double.MaxValue, ErrorMessage = "Lương cơ bản không hợp lệ")]
        [Display(Name = "Lương cơ bản")]
        public decimal luongCoBan { get; set; }
    }
}