using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using human_resource_management.Filters;

namespace human_resource_management.Areas.Employee.Controllers
{
    [RoleAuthorize("Nhân viên")]
    public class HomeController : Controller
    {
        // GET: Employee/Home
        public ActionResult Index()
        {
            return View();
        }
    }
}