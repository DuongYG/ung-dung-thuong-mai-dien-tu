using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020583.BusinessLayers;
using SV22T1020583.Models.Partner;
using SV22T1020583.Shop.Models;
using System.Security.Claims;

namespace SV22T1020583.Shop.Controllers
{
    /// <summary>
    /// Quản lý tài khoản
    /// </summary>
    public class AccountController : Controller
    {
        /// <summary>
        /// Hiển thị trang đăng nhập cho người dùng
        /// </summary>
        /// <param name="returnUrl">Đường dẫn quay lại sau khi đăng nhập thành công</param>
        /// <returns>View đăng nhập</returns>
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string returnUrl = "/")
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }
        /// <summary>
        /// Xử lý đăng nhập hệ thống cho người dùng
        /// </summary>
        /// <param name="model">Thông tin đăng nhập gồm Email và Password</param>
        /// <returns>
        /// Nếu đăng nhập thành công sẽ chuyển đến trang trước đó,
        /// nếu thất bại sẽ hiển thị lại trang đăng nhập kèm thông báo lỗi
        /// </returns>
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userAccount = await SecurityDataService.AuthorizeAsync(model.Email, model.Password);

            if (userAccount == null)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userAccount.DisplayName),
                new Claim(ClaimTypes.Email, userAccount.UserName),
                new Claim("CustomerID", userAccount.UserId)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return LocalRedirect(model.ReturnUrl);
        }
        /// <summary>
        /// Hiển thị giao diện đăng ký tài khoản khách hàng mới
        /// </summary>
        /// <returns>View đăng ký tài khoản</returns>
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Register()
        {
            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
            return View(new Customer());
        }
        /// <summary>
        /// Xử lý đăng ký tài khoản khách hàng
        /// </summary>
        /// <param name="data">Thông tin khách hàng đăng ký</param>
        /// <param name="confirmPassword">Mật khẩu xác nhận</param>
        /// <returns>
        /// Nếu đăng ký thành công sẽ chuyển về trang đăng nhập,
        /// nếu thất bại sẽ hiển thị lại form đăng ký
        /// </returns>
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Customer data, string confirmPassword)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
                return View(data);
            }

            if (data.Password != confirmPassword)
            {
                ModelState.AddModelError("confirmPassword", "Xác nhận mật khẩu không đúng");
            }

            if (!await PartnerDataService.ValidateCustomerEmailAsync(data.Email))
            {
                ModelState.AddModelError("Email", "Email đã tồn tại");
            }

            if (ModelState.IsValid)
            {
                data.Password = SecurityDataService.GetMD5(data.Password);

                int id = await PartnerDataService.AddCustomerAsync(data);

                if (id > 0)
                    return RedirectToAction("Login");

                ModelState.AddModelError("", "Không thể đăng ký tài khoản");
            }

            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
            return View(data);
        }
        /// <summary>
        /// Hiển thị thông tin hồ sơ cá nhân của khách hàng đang đăng nhập
        /// </summary>
        /// <returns>Trang hồ sơ cá nhân</returns>
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var id = User.FindFirst("CustomerID")?.Value;

            if (string.IsNullOrEmpty(id))
                return RedirectToAction("Login");

            var data = await PartnerDataService.GetCustomerAsync(int.Parse(id));

            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();

            return View(data);
        }
        /// <summary>
        /// Cập nhật thông tin hồ sơ cá nhân của khách hàng
        /// </summary>
        /// <param name="data">Thông tin khách hàng cần cập nhật</param>
        /// <returns>Chuyển về trang Profile sau khi cập nhật</returns>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(Customer data)
        {
            var current = await PartnerDataService.GetCustomerAsync(data.CustomerID);

            if (current == null)
                return RedirectToAction("Login");

            data.Password = current.Password;

            bool result = await PartnerDataService.UpdateCustomerAsync(data);

            TempData["Message"] = result ? "Cập nhật thành công" : "Cập nhật thất bại";

            return RedirectToAction("Profile");
        }
        /// <summary>
        /// Thay đổi mật khẩu cho tài khoản đang đăng nhập
        /// </summary>
        /// <param name="oldPassword">Mật khẩu cũ</param>
        /// <param name="newPassword">Mật khẩu mới</param>
        /// <param name="confirmPassword">Xác nhận mật khẩu mới</param>
        /// <returns>Chuyển về trang Profile sau khi đổi mật khẩu</returns>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (newPassword != confirmPassword)
            {
                TempData["ErrorPass"] = "Xác nhận mật khẩu không đúng";
                return RedirectToAction("Profile");
            }

            var user = await SecurityDataService.AuthorizeAsync(email!, oldPassword);

            if (user == null)
            {
                TempData["ErrorPass"] = "Mật khẩu cũ không đúng";
                return RedirectToAction("Profile");
            }

            await SecurityDataService.ChangePasswordAsync(email!, newPassword);

            TempData["MessagePass"] = "Đổi mật khẩu thành công";

            return RedirectToAction("Profile");
        }
        /// <summary>
        /// Đăng xuất tài khoản người dùng khỏi hệ thống
        /// </summary>
        /// <returns>Chuyển về trang đăng nhập</returns>
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}