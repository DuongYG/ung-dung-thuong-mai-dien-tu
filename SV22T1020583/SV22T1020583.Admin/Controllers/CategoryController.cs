using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020583.Admin.AppCodes;
using SV22T1020583.BusinessLayers;
using SV22T1020583.Models.Catalog;
using SV22T1020583.Models.Common;

namespace SV22T1020583.Admin.Controllers
{
    /// <summary>
    /// Cung cấp các chức năng liên quan đến loại hàng
    /// </summary>
    [Authorize]
    public class CategoryController : Controller
    {   
        private const string CATEGORY_SEARCH = "CategorySearchInput";
        /// <summary>
        /// Nhập đầu vào tìm kiếm và hiển thị kết quả tìm kiếm
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CATEGORY_SEARCH);
            if (input == null)
            {
                input = new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = ""
                };
            }
            return View(input);
        }
        /// <summary>
        /// Tìm kiếm và trả về kết quả
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await CatalogDataService.ListCategoriesAsync(input);
            ApplicationContext.SetSessionData(CATEGORY_SEARCH, input);
            return View(result);
        }
        /// <summary>
        /// Bổ sung loại hàng mới
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung loại hàng";
            var model = new Category() { CategoryID = 0 };
            return View("Edit", model);
        }
        /// <summary>
        /// Cập nhật thông tin loại hàng
        /// </summary>
        /// <param name="id">Mã loại hàng cần cập nhật</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật loại hàng";
            var model = await CatalogDataService.GetCategoryAsync(id);
            if (model == null) return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Category data, IFormFile? uploadPhoto)
        {
            if (string.IsNullOrWhiteSpace(data.CategoryName))
                ModelState.AddModelError(nameof(data.CategoryName), "Tên loại hàng không được để trống");

            // Nếu có upload ảnh mới
            if (uploadPhoto != null && uploadPhoto.Length > 0)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(uploadPhoto.FileName);

                string path = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/images/categories",
                    fileName
                );

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await uploadPhoto.CopyToAsync(stream);
                }

                data.Photo = fileName;
            }
            else
            {
                // giữ ảnh cũ khi update
                if (data.CategoryID != 0)
                {
                    var oldData = await CatalogDataService.GetCategoryAsync(data.CategoryID);
                    if (oldData != null)
                        data.Photo = oldData.Photo;
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Title = data.CategoryID == 0 ? "Bổ sung loại hàng" : "Cập nhật loại hàng";
                return View("Edit", data);
            }

            if (data.CategoryID == 0)
                await CatalogDataService.AddCategoryAsync(data);
            else
                await CatalogDataService.UpdateCategoryAsync(data);

            return RedirectToAction("Index");
        }
        /// <summary>
        /// Xóa loại hàng
        /// </summary>
        /// <param name="id">Mã loại hàng cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            var model = await CatalogDataService.GetCategoryAsync(id);
            if (model == null) return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Sử dụng đúng tên hàm IsUsedCategoryAsync từ file Service bạn gửi
            if (await CatalogDataService.IsUsedCategoryAsync(id))
            {
                ModelState.AddModelError("Error", "Không thể xóa loại hàng đang có dữ liệu liên quan");
                var model = await CatalogDataService.GetCategoryAsync(id);
                return View("Delete", model);
            }

            await CatalogDataService.DeleteCategoryAsync(id);
            return RedirectToAction("Index");
        }
    }
}