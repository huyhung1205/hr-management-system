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
    public class AccountController : Controller
    {
        private ModelDBContext db = new ModelDBContext();

        // GET: Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                // Find user with matching username and password (plain text for now)
                var user = db.TaiKhoans.FirstOrDefault(u => u.tenTK == model.TenTK && u.matKhau == model.MatKhau);

                if (user != null)
                {
                    // Create authentication ticket with role information
                    FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(
                        1,
                        user.tenTK,
                        DateTime.Now,
                        DateTime.Now.AddMinutes(30),
                        model.RememberMe,
                        user.vaiTro,
                        FormsAuthentication.FormsCookiePath
                    );

                    // Encrypt the ticket
                    string encryptedTicket = FormsAuthentication.Encrypt(ticket);

                    // Create cookie with proper settings
                    HttpCookie authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
                    authCookie.HttpOnly = true;
                    authCookie.Path = FormsAuthentication.FormsCookiePath;
                    authCookie.Expires = ticket.Expiration;
                    Response.Cookies.Add(authCookie);

                    // Store user info in session
                    Session["UserName"] = user.tenTK;
                    Session["UserRole"] = user.vaiTro;
                    Session["MaNV"] = user.maNV;

                    // Redirect based on role
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        // Redirect to appropriate area based on role
                        //switch (user.vaiTro.ToLower())
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

        // GET: Account/Logout
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        // Helper method to hash password with SHA256
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