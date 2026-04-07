using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020583.BusinessLayers;
using SV22T1020583.Models.Security;
using System.Security.Claims;

namespace SV22T1020583.Admin.Controllers
{
    /// <summary>
    /// Các chức năng liên quan đến tài khoản
    /// </summary>
    [Authorize]
    public class AccountController : Controller
    {
        /// <summary>
        /// Đăng nhập
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            ViewBag.Username = username;

            // Kiểm tra đầu vào
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Error", "Vui lòng nhập đầy đủ Email và Mật khẩu");
                return View();
            }

            // Xác thực tài khoản từ Database (SecurityDataService đã có MD5 bên trong)
            var userAccount = await SecurityDataService.AuthorizeAsync(username, password);

            if (userAccount == null)
            {
                ModelState.AddModelError("Error", "Đăng nhập thất bại. Sai tài khoản hoặc mật khẩu.");
                return View();
            }

            // 3. Chuyển đổi từ UserAccount (Model DB) sang WebUserData (Model Session/Cookie)
            var userData = new WebUserData()

            {
                UserId = userAccount.UserId,
                UserName = userAccount.UserName,
                DisplayName = userAccount.DisplayName,
                Email = userAccount.Email,
                Photo = userAccount.Photo,
                Roles = userAccount.RoleNames?.Split(',')
                                     .Select(r => r.Trim())
                                     .ToList() ?? new List<string>()

            };

            var principal = userData.CreatePrincipal();
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Home");
        }
        /// <summary>
        /// Đăng xuất
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }
        /// <summary>
        /// Đổi mật khẩu
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public IActionResult ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                ModelState.AddModelError("Error", "Vui lòng nhập đầy đủ mật khẩu");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("Error", "Xác nhận mật khẩu mới không khớp");
                return View();
            }

            ViewBag.Message = "Đổi mật khẩu thành công!";
            return View();
        }
    }
}


