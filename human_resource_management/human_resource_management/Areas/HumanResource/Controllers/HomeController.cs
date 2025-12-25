using human_resource_management.Models;
using human_resource_management.Models.FormModel;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using human_resource_management.Filters;

namespace human_resource_management.Areas.HumanResource.Controllers
{
    [RoleAuthorize("hr")]
    public class HomeController : Controller
    {
        private ModelDBContext db = new ModelDBContext();

        // GET: HumanResource/Home
        // Tra loi: Dashboard hoac trang chu cua Admin
        public ActionResult Index()
        {
            return View();
        }

        // GET: HumanResource/ManagerEmployee
        // Hiển thị danh sách nhân viên kèm thông tin phòng ban, tài khoản và hợp đồng
        public ActionResult ManagerEmployee()
        {
            var nhanViens = db.NhanViens
                .Include(n => n.PhongBan)
                .Include(n => n.TaiKhoan)
                .Include(n => n.HopDongs)
                .ToList();
            return View(nhanViens);
        }

        // GET: HumanResource/Home/Create
        public ActionResult Create()
        {
            ViewBag.maPB = new SelectList(db.PhongBans, "maPB", "tenPB");
            return View(new NhanVienForm());
        }

        // POST: HumanResource/Home/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(NhanVienForm form)
        {
            // Kiểm tra tuổi (Phải từ 18 tuổi trở lên)
            if (form.ngaySinh.HasValue)
            {
                var age = DateTime.Today.Year - form.ngaySinh.Value.Year;
                if (form.ngaySinh.Value.Date > DateTime.Today.AddYears(-age)) age--;
                
                if (age < 18)
                {
                    ModelState.AddModelError("ngaySinh", "Nhân viên phải từ 18 tuổi trở lên.");
                }
            }

            if (ModelState.IsValid)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // 1. Chuyển đổi dữ liệu từ Form sang Entity Nhân viên
                        var nhanVien = new NhanVien
                        {
                            hoTen = form.hoTen,
                            ngaySinh = form.ngaySinh,
                            gioiTinh = form.gioiTinh,
                            diaChi = form.diaChi,
                            dienThoai = form.dienThoai,
                            email = form.email,
                            chucVu = form.chucVu,
                            trangThaiLamViec = form.trangThaiLamViec,
                            ngayVaoLam = form.ngayVaoLam,
                            maPB = form.maPB
                        };

                        // 2. Tạo đối tượng Hợp đồng từ thông tin trong form
                        var hopDong = new HopDong
                        {
                            loaiHD = form.loaiHD,
                            heSoLuong = form.heSoLuong,
                            luongCoBan = form.luongCoBan,
                            ngayBatDau = form.ngayVaoLam ?? DateTime.Now,
                            trangThai = true // Hợp đồng mặc định có hiệu lực
                        };
                        // Gán hợp đồng cho nhân viên (Navigation Property)
                        nhanVien.HopDongs.Add(hopDong);
                        // 3. Lưu tất cả vào Database
                        db.NhanViens.Add(nhanVien);
                        db.SaveChanges(); // EF tự xử lý lưu nhanVien trước để lấy ID cho hopDong
                        transaction.Commit();
                        // Thiết lập thông báo thành công
                        TempData["SuccessMessage"] = "Thêm nhân viên và khởi tạo hồ sơ thành công!";
                        return RedirectToAction("ManagerEmployee");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        ModelState.AddModelError("", "Có lỗi xảy ra khi lưu: " + ex.Message);
                    }
                }
            }

            ViewBag.maPB = new SelectList(db.PhongBans, "maPB", "tenPB", form.maPB);
            return View(form);
        }

        public ActionResult Salary()
        {
            return View();
        }

        public ActionResult Statistical()
        {
            // Thêm .Include("NhanViens") để tải luôn danh sách nhân viên đi kèm phòng ban
            var data = db.PhongBans.Include(p => p.NhanViens).ToList();
            return View(data);
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
