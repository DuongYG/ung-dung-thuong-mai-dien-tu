using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020583.Admin.AppCodes;
using SV22T1020583.Models.Common;
using SV22T1020583.Models.Partner;

namespace SV22T1020583.Admin.Controllers
{
    /// <summary>
    /// Các chức năng liên quan đến quản lý dữ liệu nhà cung cấp
    /// </summary>
    [Authorize]
    public class SupplierController : Controller
    {
        /// <summary>
        /// Tên của biến để lưu điều kiện tìm kiếm nhà cung cấp trong session
        /// </summary>
        private const string SUPPLIER_SEARCH = "SupplierSearchInput";
        /// <summary>
        /// Nhập đầu vào tìm kiếm, hiển thị kết quả tìm kiếm trên View
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SUPPLIER_SEARCH);
            if (input == null)
                input = new PaginationSearchInput()
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
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await PartnerDataService.ListSuppliersAsync(input);
            ApplicationContext.SetSessionData(SUPPLIER_SEARCH, input);
            return View(result);
        }
        /// <summary>
        /// Bổ sung thêm nhà cung cấp mới
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhà cung cấp";
            var model = new Supplier()
            {
                SupplierID = 0,
            };
            return View("Edit", model);
        }
        /// <summary>
        /// Cập nhật thông tin của một nhà cung cấp
        /// </summary>
        /// <param name="id">Mã nhà cung cấp cần cập nhật thông tin</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin nhà cung cấp";
            var model = await PartnerDataService.GetSupplierAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> SaveData(Supplier data)
        {
            try
            {
                ViewBag.Title = data.SupplierID == 0 ? "Bổ sung nhà cung cấp" : "Cập nhật nhà cung cấp";
                //Kiểm tra dữ liệu đầu vào
                //Sử dụng ModelState để lưu trữ thông tin lỗi và hiển thị lỗi
                //Yêu cầu phải nhập: Tên, email, Tỉnh/Thành
                if (string.IsNullOrWhiteSpace(data.SupplierName))
                {
                    ModelState.AddModelError(nameof(data.SupplierName), "Tên nhà cung cấp không được để trống");
                }
                if (string.IsNullOrWhiteSpace(data.Province))
                {
                    ModelState.AddModelError(nameof(data.Province), "Tỉnh/Thành phố không được để trống");
                }

                if (!ModelState.IsValid)
                {
                    //Nếu có lỗi, trả về View Edit để hiển thị lỗi
                    return View("Edit", data);
                }

                //Hiệu chỉnh dữ liệu (tùy chọn)
                if (string.IsNullOrEmpty(data.SupplierName)) data.ContactName = data.SupplierName;
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (string.IsNullOrEmpty(data.Email)) data.Email = "";
                if (string.IsNullOrEmpty(data.Address)) data.Address = "";
                //Lưu vào CSDL
                if (data.SupplierID == 0)
                {
                    await PartnerDataService.AddSupplierAsync(data);
                }
                else
                {
                    await PartnerDataService.UpdateSupplierAsync(data);
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                //Ghi log lỗi dựa vào thông tin trong ex (ex.Message và ex.StackTrace)
                ModelState.AddModelError("Error", "Hệ thống đang bận, vui lòng thử lại sau");
                return View("Edit", data);
            }
        }
        /// <summary>
        /// Giao diện xác nhận xóa một nhà cung cấp
        /// </summary>
        /// <param name="id">Mã nhà cung cấp cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id = 0)
        {
            var data = await PartnerDataService.GetSupplierAsync(id);
            if (data == null)
                return RedirectToAction("Index");

            return View(data);
        }

        /// <summary>
        /// Thực hiện xóa nhà cung cấp dựa trên mã ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> DeleteConfirm(int id)
        {
            await PartnerDataService.DeleteSupplierAsync(id);
            return RedirectToAction("Index");
        }
    }
}