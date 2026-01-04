using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using human_resource_management.Filters;
using human_resource_management.Models;
using human_resource_management.Areas.HumanResource.Data;

namespace human_resource_management.Areas.HumanResource.Controllers
{
    [RoleAuthorize("Nhân sự")]
    /// <summary>
    /// Controller chính cho phân hệ Nhân sự (HR).
    /// Chứa các chức năng quản lý nhân viên, tính lương và thống kê báo cáo.
    /// Yêu cầu quyền "Nhân sự" để truy cập.
    /// </summary>
    public class HomeController : Controller
    {
        private ModelDBContext db = new ModelDBContext();

        /// <summary>
        /// GET: Trang chủ HR
        /// Hiển thị Dashboard chính của phân hệ nhân sự.
        /// </summary>
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// GET: Quản lý danh sách nhân viên.
        /// - Hiển thị danh sách nhân viên dạng bảng.
        /// - Hỗ trợ tìm kiếm theo nhiều tiêu chí (Tên, Mã, Phòng ban).
        /// - Hỗ trợ phân trang dữ liệu.
        /// </summary>
        /// <param name="searchString">Từ khóa tìm kiếm</param>
        /// <param name="page">Số trang hiện tại (mặc định là 1)</param>
        public ActionResult ManagerEmployee(string searchString, int? page)
        {
            int pageSize = 10; // Số lượng bản ghi trên mỗi trang
            int pageNumber = (page ?? 1);

            // 1. Khởi tạo query
            var query = db.NhanViens
                .Include("PhongBan")
                .Include("TaiKhoan")
                .Include("HopDongs")
                .AsQueryable();

            // 2. Kiểm tra nếu có từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                int searchId;
                bool isId = int.TryParse(searchString, out searchId);

                if (isId)
                {
                    // Nếu là số: Tìm chính xác Mã NV (Hoặc Tên/Phòng ban có chứa số này)
                    // Thay vì Contains() gây ra việc tìm "1" ra "11", "12"... giờ chỉ ra "1"
                    query = query.Where(nv =>
                        nv.maNV == searchId ||
                        nv.hoTen.Contains(searchString) ||
                        (nv.PhongBan != null && nv.PhongBan.tenPB.Contains(searchString))
                    );
                }
                else
                {
                    // Nếu không phải số: Chỉ tìm theo Tên hoặc Phòng Ban
                    query = query.Where(nv =>
                        nv.hoTen.Contains(searchString) ||
                        (nv.PhongBan != null && nv.PhongBan.tenPB.Contains(searchString))
                    );
                }
            }

            // Sắp xếp bắt buộc trước khi phân trang
            query = query.OrderBy(nv => nv.maNV);

            // 3. Tính toán phân trang
            int totalRecords = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            // 4. Lấy dữ liệu cho trang hiện tại
            var data = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            // 5. Truyền thông tin phân trang qua ViewBag
            ViewBag.CurrentFilter = searchString;
            ViewBag.PageNumber = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.HasPreviousPage = pageNumber > 1;
            ViewBag.HasNextPage = pageNumber < totalPages;

            return View(data);
        }

        #region Thêm mới nhân viên

        /// <summary>
        /// GET: Hiển thị form tạo hồ sơ nhân viên mới
        /// - Chuẩn bị danh sách phòng ban cho dropdown
        /// - Khởi tạo form trống
        /// </summary>
        public ActionResult Create()
        {
            // Lấy danh sách phòng ban để hiển thị trong dropdown
            ViewBag.maPB = new SelectList(db.PhongBans, "maPB", "tenPB");
            
            // Trả về form với model rỗng
            return View(new Models.FormModel.NhanVienForm());
        }

        /// <summary>
        /// POST: Xử lý lưu thông tin nhân viên mới
        /// - Kiểm tra tuổi >= 18
        /// - Tạo nhân viên mới
        /// - Tạo hợp đồng đầu tiên cho nhân viên
        /// - Lưu vào database trong một transaction
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Models.FormModel.NhanVienForm form)
        {
            // Bước 1: Kiểm tra tuổi nhân viên (phải từ 18 tuổi trở lên)
            if (form.ngaySinh.HasValue)
            {
                var tuoi = DateTime.Today.Year - form.ngaySinh.Value.Year;
                
                // Điều chỉnh nếu chưa đến ngày sinh trong năm nay
                if (form.ngaySinh.Value.Date > DateTime.Today.AddYears(-tuoi))
                {
                    tuoi--;
                }
                
                if (tuoi < 18)
                {
                    ModelState.AddModelError("ngaySinh", "Nhân viên phải từ 18 tuổi trở lên.");
                }
            }

            // Bước 2: Nếu dữ liệu hợp lệ, tiến hành lưu
            if (ModelState.IsValid)
            {
                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // 2.1: Tạo đối tượng Nhân viên từ form
                        var nhanVien = new NhanVien
                        {
                            hoTen = form.hoTen,
                            ngaySinh = form.ngaySinh,
                            gioiTinh = form.gioiTinh,
                            diaChi = form.diaChi,
                            dienThoai = form.dienThoai,
                            email = form.email,
                            chucVu = form.chucVu,
                            trangThaiLamViec = form.trangThaiLamViec ?? "Đang làm việc",
                            ngayVaoLam = form.ngayVaoLam,
                            maPB = form.maPB
                        };

                        // 2.2: Tạo Hợp đồng đầu tiên cho nhân viên
                        var hopDong = new HopDong
                        {
                            loaiHD = form.loaiHD,
                            heSoLuong = form.heSoLuong,
                            luongCoBan = form.luongCoBan,
                            ngayBatDau = form.ngayVaoLam ?? DateTime.Now,
                            trangThai = true // Hợp đồng mặc định có hiệu lực
                        };

                        // 2.3: Gán hợp đồng vào nhân viên (Navigation Property)
                        nhanVien.HopDongs.Add(hopDong);

                        // 2.4: Lưu nhân viên vào database (EF tự lưu HopDong theo)
                        db.NhanViens.Add(nhanVien);
                        db.SaveChanges();

                        // 2.5: Commit transaction nếu thành công
                        transaction.Commit();

                        // 2.6: Thiết lập thông báo thành công và chuyển hướng
                        TempData["SuccessMessage"] = "Thêm nhân viên và khởi tạo hồ sơ thành công!";
                        return RedirectToAction("ManagerEmployee");
                    }
                    catch (Exception ex)
                    {
                        // Rollback nếu có lỗi
                        transaction.Rollback();
                        ModelState.AddModelError("", "Có lỗi xảy ra khi lưu: " + ex.Message);
                    }
                }
            }

            // Nếu có lỗi, trả lại form với dữ liệu đã nhập
            ViewBag.maPB = new SelectList(db.PhongBans, "maPB", "tenPB", form.maPB);
            return View(form);
        }

        #endregion

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

        /// <summary>
        /// GET: Thống kê nhân sự.
        /// - Hiển thị báo cáo phân bổ nhân sự theo từng phòng ban.
        /// - Dữ liệu dùng để vẽ biểu đồ tròn (Pie Chart).
        /// </summary>
        public ActionResult Statistical()
        {
            // Sử dụng 'using' để quản lý bộ nhớ tốt nhất
            using (ModelDBContext db = new ModelDBContext())
            {
                // Thêm .Include("NhanViens") để tải luôn danh sách nhân viên đi kèm phòng ban
                // Giúp đếm số lượng chính xác ngay lập tức (Eager Loading)
                var data = db.PhongBans.Include("NhanViens").ToList();

                return View(data);
            }
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