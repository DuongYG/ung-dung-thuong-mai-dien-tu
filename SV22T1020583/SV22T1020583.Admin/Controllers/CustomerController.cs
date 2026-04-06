using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020583.Admin.AppCodes;
using SV22T1020583.Models.Common;
using SV22T1020583.Models.Partner;

namespace SV22T1020583.Admin.Controllers
{
    /// <summary>
    /// Các chức năng quản lý dữ liệu liên quan đến khách hàng
    /// </summary>
    [Authorize]
    public class CustomerController : Controller
    {
        /// <summary>
        /// Tên của biến để lưu điều kiện tìm kiếm khách hàng trong session
        /// </summary>
        private const string CUSTOMER_SEARCH = "CustomerSearchInput";
        /// <summary>
        /// Nhập đầu vào tìm kiếm, hiển thị kết quả tìm kiếm trên View
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CUSTOMER_SEARCH);
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
            var result = await PartnerDataService.ListCustomersAsync(input);
            ApplicationContext.SetSessionData(CUSTOMER_SEARCH, input);
            return View(result);
        }
        /// <summary>
        /// Bổ sung một khách hàng mới
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Thêm mới khách hàng";
            var model = new Customer()
            {
                CustomerID = 0,
                IsLocked = false
            };

            return View("Edit", model);
        }
        /// <summary>
        /// Cập nhật thông tin của một khách hàng
        /// </summary>
        /// <param name="id">Mã khách hàng cần cập nhật</param>
        /// <returns></returns>

        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin khách hàng";

            // LỖI TẠI ĐÂY: Phải có await để lấy dữ liệu thực sự từ Task
            var model = await PartnerDataService.GetCustomerAsync(id);

            if (model == null)
            {
                return RedirectToAction("Index");
            }

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> SaveData(Customer data)
        {
            try
            {
                ViewBag.Title = data.CustomerID == 0 ? "Bổ sung khách hàng" : "Cập nhật thông tin khách hàng";
                //Kiểm tra dữ liệu đầu vào
                //Sử dụng ModelState để lưu trữ thông tin lỗi và hiển thị lỗi
                //Yêu cầu phải nhập: Tên, email, Tỉnh/Thành
                if (string.IsNullOrWhiteSpace(data.CustomerName))
                {
                    ModelState.AddModelError(nameof(data.CustomerName), "Tên khách hàng không được để trống");
                }

                if (string.IsNullOrWhiteSpace(data.Email))
                {
                    ModelState.AddModelError(nameof(data.Email), "Email khách hàng không được để trống");
                }
                else if (!await PartnerDataService.ValidateCustomerEmailAsync(data.Email, data.CustomerID))
                {
                    ModelState.AddModelError(nameof(data.Email), "Email này đã có người sử dụng");
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
                if (string.IsNullOrEmpty(data.CustomerName)) data.ContactName = data.CustomerName;
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (string.IsNullOrEmpty(data.Address)) data.Address = "";
                //Lưu vào CSDL
                if (data.CustomerID == 0)
                {
                    await PartnerDataService.AddCustomerAsync(data);
                }
                else
                {
                    await PartnerDataService.UpdateCustomerAsync(data);
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
        /// Xóa một khách hàng
        /// </summary>
        /// <param name="id">Mã khách hàng cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await PartnerDataService.DeleteCustomerAsync(id);
                return RedirectToAction("Index");
            }

            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index");
            }

            ViewBag.CanDelete = !(await PartnerDataService.IsUsedCustomerAsync(id));

            return View(model);
        }
        /// <summary>
        /// Đổi mật khẩu của một khách hàng
        /// </summary>
        /// <param name="id">Mã khách hàng cần đổi mật khẩu</param>
        /// <returns></returns>

        // GET: Customer/ChangePassword/5
        public async Task<IActionResult> ChangePassword(int id)
        {
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }
        // POST: Customer/UpdatePassword
        [HttpPost]
        public async Task<IActionResult> UpdatePassword(int customerID, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword != confirmPassword)
            {
                // Nếu mật khẩu trống hoặc không khớp, quay lại trang đổi mật khẩu và báo lỗi
                ModelState.AddModelError("", "Mật khẩu xác nhận không khớp hoặc trống.");
                var model = await PartnerDataService.GetCustomerAsync(customerID);
                return View("ChangePassword", model);
            }
            // Thực hiện gọi hàm cập nhật mật khẩu từ Service
            // await PartnerDataService.UpdatePasswordAsync(customerID, newPassword);
            return RedirectToAction("Index");
        }
    }
}
