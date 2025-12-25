using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace human_resource_management.Areas.HumanResource.Data
{
    public class EmployeeSalaryViewModel
    {
        public int maNV { get; set; }
        public string hoTen { get; set; }
        public string chucVu { get; set; }
        public decimal luongCoBan { get; set; }
        public int ngayCong { get; set; }
        public bool hasWarning { get; set; }
    }
}
