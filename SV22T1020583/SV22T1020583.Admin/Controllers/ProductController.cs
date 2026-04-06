using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020583.Admin.AppCodes;
using SV22T1020583.BusinessLayers;
using SV22T1020583.Models.Catalog;

namespace SV22T1020583.Admin.Controllers
{
    /// <summary>
    /// Các chức năng quản lý dữ liệu liên quan đến mặt hàng
    /// </summary>
    [Authorize]
    public class ProductController : Controller
    {
        /// <summary>
        /// Tên của biến để lưu điều kiện tìm kiếm mặt hàng trong session
        /// </summary>
        private const string PRODUCT_SEARCH = "ProductSearchInput";

        /// <summary>
        /// Nhập đầu vào tìm kiếm, hiển thị kết quả tìm kiếm trên View
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(PRODUCT_SEARCH);
            if (input == null)
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = ""
                };
            return View(input);
        }

        /// <summary>
        /// Tìm kiếm và trả về kết quả
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<IActionResult> Search(ProductSearchInput input)
        {
            ApplicationContext.SetSessionData(PRODUCT_SEARCH, input);
            var data = await CatalogDataService.ListProductsAsync(input);
            return PartialView(data);
        }

        /// <summary>
        /// Giao diện bổ sung mặt hàng mới
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung mặt hàng";
            var data = new Product()
            {
                ProductID = 0,
                Photo = "nophoto.png",
                IsSelling = true
            };
            return View("Edit", data);
        }

        /// <summary>
        /// Giao diện cập nhật thông tin mặt hàng
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin mặt hàng";
            var data = await CatalogDataService.GetProductAsync(id);
            if (data == null)
                return RedirectToAction("Index");

            return View(data);
        }

        /// <summary>
        /// Lưu dữ liệu mặt hàng
        /// </summary>
        /// <param name="data"></param>
        /// <param name="uploadPhoto"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Save(Product data, IFormFile? uploadPhoto)
        {
            if (string.IsNullOrWhiteSpace(data.ProductName))
                ModelState.AddModelError(nameof(data.ProductName), "Tên mặt hàng không được để trống");

            if (!ModelState.IsValid)
            {
                ViewBag.Title = data.ProductID == 0 ? "Bổ sung mặt hàng" : "Cập nhật thông tin mặt hàng";
                return View("Edit", data);
            }

            if (uploadPhoto != null)
            {
                string fileName = $"{DateTime.Now.Ticks}_{uploadPhoto.FileName}";
                string folder = Path.Combine(ApplicationContext.WebHostEnviroment.WebRootPath, "images", "products");
                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadPhoto.CopyToAsync(stream);
                }
                data.Photo = fileName;
            }

            if (data.ProductID == 0)
                await CatalogDataService.AddProductAsync(data);
            else
                await CatalogDataService.UpdateProductAsync(data);

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Xác nhận xóa mặt hàng
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            var data = await CatalogDataService.GetProductAsync(id);
            if (data == null)
                return RedirectToAction("Index");
            return View(data);
        }

        /// <summary>
        /// Thực hiện xóa mặt hàng
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> DeleteConfirm(int id)
        {
            await CatalogDataService.DeleteProductAsync(id);
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Hiển thị danh sách hình ảnh của mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng cần lấy hình ảnh</param>
        /// <returns></returns>
        public async Task<IActionResult> ListPhotos(int id)
        {
            var data = await CatalogDataService.ListPhotosAsync(id);
            return PartialView(data);
        }

        /// <summary>
        /// Bổ sung hình ảnh mới cho mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng cần thêm hình ảnh</param>
        /// <returns></returns>
        public IActionResult CreatePhoto(int id)
        {
            ViewBag.Title = "Bổ sung ảnh cho mặt hàng";
            var data = new ProductPhoto()
            {
                ProductID = id,
                PhotoID = 0,
                DisplayOrder = 1,
                IsHidden = false
            };
            return View("EditPhoto", data);
        }

        /// <summary>
        /// Cập nhật hình ảnh của mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng có hình ảnh cần cập nhật</param>
        /// <param name="photoid">Mã hình ảnh cần cập nhật</param>
        /// <returns></returns>
        public async Task<IActionResult> EditPhoto(int id, long photoid)
        {
            ViewBag.Title = "Thay đổi ảnh của mặt hàng";
            var data = await CatalogDataService.GetPhotoAsync(photoid);
            if (data == null)
                return RedirectToAction("Edit", new { id = id });

            return View(data);
        }

        /// <summary>
        /// Lưu ảnh mặt hàng
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SavePhoto(ProductPhoto data, IFormFile? uploadPhoto)
        {
            if (uploadPhoto != null)
            {
                string fileName = $"{DateTime.Now.Ticks}_{uploadPhoto.FileName}";
                string folder = Path.Combine(ApplicationContext.WebHostEnviroment.WebRootPath, "images", "products");
                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadPhoto.CopyToAsync(stream);
                }
                data.Photo = fileName;
            }

            if (data.PhotoID == 0)
                await CatalogDataService.AddPhotoAsync(data);
            else
                await CatalogDataService.UpdatePhotoAsync(data);

            return RedirectToAction("Edit", new { id = data.ProductID });
        }

        /// <summary>
        /// Xóa hình ảnh của mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng có hình ảnh cần xóa</param>
        /// <param name="photoid">Mã hình ảnh cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> DeletePhoto(int id, long photoid)
        {
            await CatalogDataService.DeletePhotoAsync(photoid);
            return RedirectToAction("Edit", new { id = id });
        }

        /// <summary>
        /// Hiển thị danh sách thuộc tính của mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng cần lấy thuộc tính</param>
        /// <returns></returns>
        public async Task<IActionResult> ListAttribute(int id)
        {
            var data = await CatalogDataService.ListAttributesAsync(id);
            return PartialView(data);
        }

        /// <summary>
        /// Giao diện bổ sung thuộc tính
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IActionResult CreateAttribute(int id)
        {
            ViewBag.Title = "Bổ sung thuộc tính";
            var data = new ProductAttribute()
            {
                ProductID = id,
                AttributeID = 0,
                DisplayOrder = 1
            };
            return View("EditAttribute", data);
        }

        /// <summary>
        /// Giao diện cập nhật thuộc tính
        /// </summary>
        /// <param name="id"></param>
        /// <param name="attributeid"></param>
        /// <returns></returns>
        public async Task<IActionResult> EditAttribute(int id, long attributeid)
        {
            ViewBag.Title = "Thay đổi thuộc tính";
            var data = await CatalogDataService.GetAttributeAsync(attributeid);
            if (data == null)
                return RedirectToAction("Edit", new { id = id });

            return View(data);
        }

        /// <summary>
        /// Lưu thuộc tính
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> SaveAttribute(ProductAttribute data)
        {
            if (string.IsNullOrWhiteSpace(data.AttributeName))
                ModelState.AddModelError(nameof(data.AttributeName), "Tên thuộc tính không được để trống");

            if (!ModelState.IsValid)
            {
                ViewBag.Title = data.AttributeID == 0 ? "Bổ sung thuộc tính" : "Thay đổi thuộc tính";
                return View("EditAttribute", data);
            }

            if (data.AttributeID == 0)
                await CatalogDataService.AddAttributeAsync(data);
            else
                await CatalogDataService.UpdateAttributeAsync(data);

            return RedirectToAction("Edit", new { id = data.ProductID });
        }

        /// <summary>
        /// Thực hiện xóa thuộc tính
        /// </summary>
        /// <param name="id"></param>
        /// <param name="attributeid"></param>
        /// <returns></returns>
        public async Task<IActionResult> DeleteAttribute(int id, long attributeid)
        {
            await CatalogDataService.DeleteAttributeAsync(attributeid);
            return RedirectToAction("Edit", new { id = id });
        }
    }
}