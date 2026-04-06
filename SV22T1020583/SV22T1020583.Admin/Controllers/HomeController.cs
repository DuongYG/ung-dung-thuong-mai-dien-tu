using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace SV22T1020583.Admin.Controllers
{
    /// <summary>
    /// Các chức năng của trang chủ
    /// </summary>
    [Authorize]
    public class HomeController : Controller
    {
        /// <summary>
        /// Trang chủ
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
