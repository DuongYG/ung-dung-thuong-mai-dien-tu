using Microsoft.AspNetCore.Mvc;
using SV22T1020583.BusinessLayers;
using SV22T1020583.Models.Catalog;
using SV22T1020583.Models.Common;
using SV22T1020583.Shop.Models;

namespace SV22T1020583.Shop.Controllers
{
    /// <summary>
    /// Quản lý sản phẩm
    /// </summary>
    public class ProductController : Controller
    {
        /// <summary>
        /// Hiển thị danh sách sản phẩm.
        /// Cho phép tìm kiếm theo tên, lọc theo danh mục và khoảng giá.
        /// </summary>
        /// <param name="condition">
        /// Điều kiện tìm kiếm sản phẩm bao gồm:
        /// từ khóa tìm kiếm, danh mục, giá tối thiểu, giá tối đa và phân trang.
        /// </param>
        /// <returns>Trang danh sách sản phẩm</returns>
        public async Task<IActionResult> Index(ProductSearchViewModel condition)
        {
            var categoryInput = new PaginationSearchInput()
            {
                Page = 1,
                PageSize = 0,
                SearchValue = ""
            };

            // Lấy dữ liệu danh mục (Truyền đối số categoryInput)
            var categoryResult = await CatalogDataService.ListCategoriesAsync(categoryInput);
            ViewBag.Categories = categoryResult.DataItems;

            // Chuyển đổi condition sang định dạng input của Business Layer cho Sản phẩm
            var input = new ProductSearchInput()
            {
                Page = condition.Page <= 0 ? 1 : condition.Page,
                PageSize = condition.PageSize <= 0 ? 12 : condition.PageSize,
                SearchValue = condition.SearchValue ?? "",
                CategoryID = condition.CategoryID,
                MinPrice = condition.MinPrice,
                MaxPrice = condition.MaxPrice
            };

            condition.Result = await CatalogDataService.ListProductsAsync(input);
            return View(condition);
        }
        /// <summary>
        /// Hiển thị thông tin chi tiết của một sản phẩm
        /// bao gồm hình ảnh và các thuộc tính của sản phẩm.
        /// </summary>
        /// <param name="id">Mã sản phẩm cần xem</param>
        /// <returns>Trang chi tiết sản phẩm</returns>
        public async Task<IActionResult> Detail(int id)
        {
            if (id <= 0) return RedirectToAction("Index");

            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null) return RedirectToAction("Index");

            ViewBag.Photos = await CatalogDataService.ListPhotosAsync(id);
            ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(id);

            return View(product);
        }
    }
}