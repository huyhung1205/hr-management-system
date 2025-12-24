using human_resource_management.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace human_resource_management.Areas.HumanResource.Controllers
{
    public class HomeController : Controller
    {
        // GET: HumanResource/Home
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult ManagerEmployee()
        {
            return View();
        }
        public ActionResult Salary()
        {
            return View();
        }
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

    }
}