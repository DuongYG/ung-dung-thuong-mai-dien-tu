using Microsoft.AspNetCore.Mvc;
using SV22T1020583.BusinessLayers;
using SV22T1020583.Models.Catalog;
using SV22T1020583.Models.Common;
using SV22T1020583.Shop.Models;
using System.Diagnostics;

namespace SV22T1020583.Shop.Controllers
{
    /// <summary>
    /// Quản lý trang chủ
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private const int PAGE_SIZE = 12;
        /// <summary>
        /// Khởi tạo HomeController
        /// </summary>
        /// <param name="logger">Đối tượng ghi log của hệ thống</param>
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        /// <summary>
        /// Hiển thị trang chủ với danh sách sản phẩm.
        /// Hỗ trợ tìm kiếm theo tên, lọc theo danh mục và khoảng giá.
        /// </summary>
        /// <param name="page">Trang hiện tại</param>
        /// <param name="searchValue">Từ khóa tìm kiếm sản phẩm</param>
        /// <param name="categoryID">Mã danh mục sản phẩm</param>
        /// <param name="minPrice">Giá tối thiểu</param>
        /// <param name="maxPrice">Giá tối đa</param>
        /// <returns>Trang danh sách sản phẩm</returns>
        public async Task<IActionResult> Index(int page = 1, string searchValue = "", int categoryID = 0, decimal minPrice = 0, decimal maxPrice = 0)
        {
    
            var productInput = new ProductSearchInput()
            {
                Page = page,
                PageSize = PAGE_SIZE,
                SearchValue = searchValue ?? "",
                CategoryID = categoryID,
                MinPrice = minPrice, 
                MaxPrice = maxPrice
            };

            // Lấy danh mục cho Sidebar
            var categoryInput = new PaginationSearchInput() { Page = 1, PageSize = 0, SearchValue = "" };
            var productData = await CatalogDataService.ListProductsAsync(productInput);
            var categoryData = await CatalogDataService.ListCategoriesAsync(categoryInput);

            // Gán dữ liệu vào ViewBag để Sidebar và Phân trang nhận được giá trị hiện tại
            ViewBag.Categories = categoryData.DataItems;
            ViewBag.SearchValue = searchValue;
            ViewBag.CategoryID = categoryID;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            return View(productData);
        }
        /// <summary>
        /// Hiển thị trang chính sách bảo mật của website
        /// </summary>
        /// <returns>Trang Privacy</returns>
        public IActionResult Privacy()
        {
            return View();
        }
        /// <summary>
        /// Hiển thị trang thông báo lỗi của hệ thống
        /// </summary>
        /// <returns>Trang hiển thị thông tin lỗi</returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}