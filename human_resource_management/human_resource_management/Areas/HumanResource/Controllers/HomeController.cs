using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using human_resource_management.Filters;
using human_resource_management.Models;
using human_resource_management.Areas.HumanResource.Data;

namespace human_resource_management.Areas.HumanResource.Controllers
{
    [RoleAuthorize("Nhân sự")]
    public class HomeController : Controller
    {
        private ModelDBContext db = new ModelDBContext();

        /// <summary>
        /// GET: Trang chủ HR
        /// </summary>
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// GET: Quản lý nhân viên
        /// </summary>
        public ActionResult ManagerEmployee()
        {
            return View();
        }

        /// <summary>
        /// GET: Trang chọn kỳ lương
        /// - Hiển thị form chọn tháng/năm (luôn có)
        /// - Nếu chưa có bảng lương: hiển thị danh sách nhân viên chuẩn bị tính lương
        /// - Nếu đã có bảng lương: hiển thị chi tiết bảng lương
        /// </summary>
        public ActionResult Salary(int? thang, int? nam)
        {
            // Mặc định là tháng/năm hiện tại
            int selectedThang = thang ?? DateTime.Now.Month;
            int selectedNam = nam ?? DateTime.Now.Year;

            ViewBag.SelectedThang = selectedThang;
            ViewBag.SelectedNam = selectedNam;

            // Initialize empty lists
            ViewBag.SalaryDetails = new List<SalaryDetailViewModel>();
            ViewBag.EmployeeSalaryData = new List<EmployeeSalaryViewModel>();

            // Kiểm tra bảng lương đã tồn tại chưa
            var existingSalaryMonth = db.BangLuongThangs
                .FirstOrDefault(b => b.thang == selectedThang && b.nam == selectedNam);

            // Nếu đã có bảng lương → hiển thị chi tiết
            if (existingSalaryMonth != null)
            {
                ViewBag.SalaryMonthId = existingSalaryMonth.maBangLuongThang;
                ViewBag.Thang = existingSalaryMonth.thang;
                ViewBag.Nam = existingSalaryMonth.nam;
                ViewBag.TrangThai = existingSalaryMonth.trangThai;
                ViewBag.NgayTao = existingSalaryMonth.ngayTao;
                ViewBag.NguoiTao = existingSalaryMonth.nguoiTao;

                var salaryDetails = existingSalaryMonth.ChiTietBangLuongs
                    .Select(ct => new SalaryDetailViewModel
                    {
                        maChiTiet = ct.maChiTiet,
                        maNV = ct.maNV,
                        hoTen = ct.NhanVien.hoTen,
                        chucVu = ct.NhanVien.chucVu,
                        luongThucNhan = ct.luongThucNhan,
                        BaseSalary = ct.NhanVien.HopDongs
                            .FirstOrDefault(hd => hd.trangThai == true && hd.maNV == ct.maNV).luongCoBan,
                        ngayCong = db.ChamCongs
                            .Where(cc => cc.maNV == ct.maNV)
                            .ToList()
                            .Where(cc => cc.ngayCham.Year == selectedNam && cc.ngayCham.Month == selectedThang)
                            .Select(cc => cc.ngayCham.Day)
                            .Distinct()
                            .Count()
                    })
                    .OrderBy(x => x.maNV)
                    .ToList();

                ViewBag.SalaryDetails = salaryDetails;
                ViewBag.TotalSalary = salaryDetails.Count > 0 
                    ? salaryDetails.Sum(s => s.luongThucNhan) 
                    : 0m;

                return View();
            }

            // Nếu chưa có → chuẩn bị dữ liệu hiển thị danh sách nhân viên
            try
            {
                var employeeSalaryData = PrepareEmployeeSalaryData(selectedThang, selectedNam);
                ViewBag.EmployeeSalaryData = employeeSalaryData;
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Lỗi: {ex.Message}";
            }

            return View();
        }

        /// <summary>
        /// POST: Thực hiện tính lương
        /// - Tạo BangLuongThang
        /// - Tính lương chi tiết cho từng nhân viên
        /// - Lưu vào database
        /// - Redirect tới SalaryDetails để kiểm tra
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveCalculateSalary(int thang, int nam)
        {
            try
            {
                // Kiểm tra bảng lương có tồn tại không
                var existingSalaryMonth = db.BangLuongThangs
                    .FirstOrDefault(b => b.thang == thang && b.nam == nam);
                
                if (existingSalaryMonth != null)
                {
                    return RedirectToAction("Salary", new { thang, nam });
                }

                // Lấy danh sách nhân viên đang làm việc
                var employees = db.NhanViens
                    .Where(nv => nv.trangThaiLamViec == "Đang làm việc")
                    .ToList();

                var firstDayOfMonth = new DateTime(nam, thang, 1);
                var lastDayOfMonth = new DateTime(nam, thang, DateTime.DaysInMonth(nam, thang));

                // Tạo bảng lương tháng mới
                var bangLuongThang = new BangLuongThang
                {
                    thang = thang,
                    nam = nam,
                    ngayTao = DateTime.Now,
                    nguoiTao = Session["UserName"]?.ToString() ?? "Hệ thống",
                    trangThai = false,
                    ghiChu = $"Tính lương - Tháng {thang}/{nam}"
                };

                db.BangLuongThangs.Add(bangLuongThang);
                db.SaveChanges();

                // Tính lương chi tiết cho từng nhân viên
                foreach (var emp in employees)
                {
                    // Kiểm tra hợp đồng hiện hành
                    var validContract = db.HopDongs
                        .FirstOrDefault(hd => hd.maNV == emp.maNV && hd.trangThai == true &&
                                              hd.ngayBatDau <= lastDayOfMonth &&
                                              (hd.ngayKetThuc == null || hd.ngayKetThuc >= firstDayOfMonth));

                    if (validContract == null) continue;

                    // Tính số ngày công từ bảng chấm công
                    var workDays = db.ChamCongs
                        .Where(cc => cc.maNV == emp.maNV)
                        .ToList()
                        .Where(cc => cc.ngayCham.Year == nam && cc.ngayCham.Month == thang)
                        .Select(cc => cc.ngayCham.Day)
                        .Distinct()
                        .Count();

                    // Công thức: Lương thực nhận = Lương cơ bản × (Ngày công / 26)
                    decimal luongThucNhan = validContract.luongCoBan * ((decimal)workDays / 26);

                    var chiTietBangLuong = new ChiTietBangLuong
                    {
                        maBangLuongThang = bangLuongThang.maBangLuongThang,
                        maNV = emp.maNV,
                        luongThucNhan = luongThucNhan
                    };

                    db.ChiTietBangLuongs.Add(chiTietBangLuong);
                }

                db.SaveChanges();

                TempData["SuccessMessage"] = $"Tính lương thành công cho tháng {thang}/{nam}!";
                return RedirectToAction("Salary", new { thang, nam });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi tính lương: {ex.Message}";
                return RedirectToAction("Salary", new { thang, nam });
            }
        }

        /// <summary>
        /// GET: Xem chi tiết bảng lương
        /// - Hiển thị danh sách lương chi tiết
        /// - Cho phép chỉnh sửa/xóa
        /// </summary>
        public ActionResult SalaryDetails(int id)
        {
            var salaryMonth = db.BangLuongThangs
                .FirstOrDefault(b => b.maBangLuongThang == id);

            if (salaryMonth == null)
            {
                TempData["ErrorMessage"] = "Bảng lương không tồn tại.";
                return RedirectToAction("Salary");
            }

            ViewBag.SalaryMonthId = salaryMonth.maBangLuongThang;
            ViewBag.Thang = salaryMonth.thang;
            ViewBag.Nam = salaryMonth.nam;
            ViewBag.TrangThai = salaryMonth.trangThai;
            ViewBag.NgayTao = salaryMonth.ngayTao;
            ViewBag.NguoiTao = salaryMonth.nguoiTao;

            var salaryDetails = salaryMonth.ChiTietBangLuongs
                .Select(ct => new SalaryDetailViewModel
                {
                    maChiTiet = ct.maChiTiet,
                    maNV = ct.maNV,
                    hoTen = ct.NhanVien.hoTen,
                    chucVu = ct.NhanVien.chucVu,
                    luongThucNhan = ct.luongThucNhan,
                    BaseSalary = ct.NhanVien.HopDongs
                        .FirstOrDefault(hd => hd.trangThai == true && hd.maNV == ct.maNV).luongCoBan
                })
                .OrderBy(x => x.maNV)
                .ToList();

            ViewBag.SalaryDetails = salaryDetails;
            ViewBag.TotalSalary = salaryDetails.Count > 0 
                ? salaryDetails.Sum(s => s.luongThucNhan) 
                : 0m;

            return View();
        }

        /// <summary>
        /// GET: Xóa bảng lương
        /// - Xóa chi tiết bảng lương
        /// - Xóa bảng lương
        /// - Redirect tới trang chọn kỳ lương
        /// </summary>
        [HttpGet]
        public ActionResult DeleteSalary(int id)
        {
            try
            {
                var salaryMonth = db.BangLuongThangs
                    .FirstOrDefault(b => b.maBangLuongThang == id);
                
                if (salaryMonth == null)
                {
                    TempData["ErrorMessage"] = "Bảng lương không tồn tại.";
                    return RedirectToAction("Salary");
                }

                int thang = salaryMonth.thang;
                int nam = salaryMonth.nam;

                // Xóa chi tiết bảng lương trước (do foreign key)
                var details = db.ChiTietBangLuongs
                    .Where(ct => ct.maBangLuongThang == id)
                    .ToList();
                
                foreach (var detail in details)
                {
                    db.ChiTietBangLuongs.Remove(detail);
                }

                // Xóa bảng lương
                db.BangLuongThangs.Remove(salaryMonth);
                db.SaveChanges();

                TempData["SuccessMessage"] = $"Xóa bảng lương tháng {thang}/{nam} thành công.";
                return RedirectToAction("Salary", new { thang, nam });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi xóa: {ex.Message}";
                return RedirectToAction("Salary");
            }
        }

        public ActionResult Statistical()
        {
            return View();
        }

        /// <summary>
        /// Hỗ trợ: Chuẩn bị dữ liệu nhân viên để tính lương
        /// </summary>
        private List<EmployeeSalaryViewModel> PrepareEmployeeSalaryData(int thang, int nam)
        {
            var employees = db.NhanViens
                .Where(nv => nv.trangThaiLamViec == "Đang làm việc")
                .ToList();

            if (employees.Count == 0)
            {
                throw new Exception("Không có nhân viên nào đang làm việc trong hệ thống.");
            }

            var firstDayOfMonth = new DateTime(nam, thang, 1);
            var lastDayOfMonth = new DateTime(nam, thang, DateTime.DaysInMonth(nam, thang));

            var employeeSalaryData = new List<EmployeeSalaryViewModel>();
            var errorList = new List<string>();

            foreach (var emp in employees)
            {
                // Kiểm tra hợp đồng hiện hành
                var validContract = db.HopDongs
                    .FirstOrDefault(hd => hd.maNV == emp.maNV && hd.trangThai == true &&
                                          hd.ngayBatDau <= lastDayOfMonth &&
                                          (hd.ngayKetThuc == null || hd.ngayKetThuc >= firstDayOfMonth));

                if (validContract == null)
                {
                    errorList.Add($"{emp.maNV} - {emp.hoTen}: Không có hợp đồng hiện hành.");
                    continue;
                }

                // Tính số ngày công (nếu không có chấm công thì = 0)
                var workDays = db.ChamCongs
                    .Where(cc => cc.maNV == emp.maNV)
                    .ToList()
                    .Where(cc => cc.ngayCham.Year == nam && cc.ngayCham.Month == thang)
                    .Select(cc => cc.ngayCham.Day)
                    .Distinct()
                    .Count();

                // Chỉ thêm vào danh sách nếu có ngày công > 0
                if (workDays == 0)
                {
                    errorList.Add($"{emp.maNV} - {emp.hoTen}: Chưa có dữ liệu chấm công.");
                    continue;
                }

                employeeSalaryData.Add(new EmployeeSalaryViewModel
                {
                    maNV = emp.maNV,
                    hoTen = emp.hoTen,
                    chucVu = emp.chucVu,
                    luongCoBan = validContract.luongCoBan,
                    ngayCong = workDays,
                    hasWarning = workDays < 10
                });
            }

            if (errorList.Count > 0)
            {
                ViewBag.ErrorList = errorList;
            }

            return employeeSalaryData;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}