using System.ComponentModel.DataAnnotations;

namespace human_resource_management.Models.FormModel
{
    public class PhongBanForm
    {
        public int maPB { get; set; }

        [Required(ErrorMessage = "Tên phòng ban không được để trống")]
        [StringLength(100, ErrorMessage = "Tên phòng ban không quá 100 ký tự")]
        [Display(Name = "Tên phòng ban")]
        public string tenPB { get; set; }
    }
}