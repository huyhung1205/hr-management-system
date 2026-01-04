using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using human_resource_management.Models;

namespace human_resource_management.Controllers
{
    /// <summary>
    /// Controller quản lý xác thực và phân quyền người dùng (Đăng nhập, Đăng xuất).
    /// </summary>
    public class AccountController : Controller
    {
        private ModelDBContext db = new ModelDBContext();

        // GET: Account/Login - Hiển thị trang đăng nhập

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: Account/Login - Xử lý đăng nhập

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                // Tìm kiếm tài khoản khớp Username và Password (đang so sánh plain text)

                var user = db.TaiKhoans.FirstOrDefault(u => u.tenTK == model.TenTK && u.matKhau == model.MatKhau);

                if (user != null)
                {
                    // Tạo Authentication Ticket chứa thông tin vai trò (Role)

                    FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(
                        1,
                        user.tenTK,
                        DateTime.Now,
                        DateTime.Now.AddMinutes(30),
                        model.RememberMe,
                        user.vaiTro,
                        FormsAuthentication.FormsCookiePath
                    );

                    // Mã hóa Auth Ticket
                    string encryptedTicket = FormsAuthentication.Encrypt(ticket);

                    // Tạo Cookie xác thực với cấu hình bảo mật

                    HttpCookie authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
                    authCookie.HttpOnly = true;
                    authCookie.Path = FormsAuthentication.FormsCookiePath;
                    authCookie.Expires = ticket.Expiration;
                    Response.Cookies.Add(authCookie);

                    // Lưu thông tin người dùng vào Session để sử dụng trong phiên làm việc

                    Session["UserName"] = user.tenTK;
                    Session["UserRole"] = user.vaiTro;
                    Session["MaNV"] = user.maNV;

                    // Điều hướng dựa trên Vai trò (Role)

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        // Chuyển hướng đến Area tương ứng với Vai trò

                        switch (user.vaiTro)
                        {
                            case "Admin":
                                return RedirectToAction("Index", "Home", new { area = "Admin" });
                            case "Nhân sự":
                                return RedirectToAction("Index", "Home", new { area = "HumanResource" });
                            case "Nhân viên":
                                return RedirectToAction("Index", "Home", new { area = "Employee" });
                            default:
                                return RedirectToAction("Index", "Home");
                        }
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Tên tài khoản hoặc mật khẩu không đúng.");
                }
            }

            return View(model);
        }

        // GET: Account/Logout - Xử lý đăng xuất

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        // Hàm hỗ trợ: Mã hóa mật khẩu bằng thuật toán SHA256 (nên dùng khi đăng ký/đổi mật khẩu)
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
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