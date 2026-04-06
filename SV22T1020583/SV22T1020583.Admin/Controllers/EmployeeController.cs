using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020583.Admin.AppCodes;
using SV22T1020583.BusinessLayers;
using SV22T1020583.Models.Common;
using SV22T1020583.Models.HR;

namespace SV22T1020583.Admin.Controllers
{
    /// <summary>
    /// Cung cấp các chức năng liên quan đến nhân viên
    /// </summary>
    [Authorize]
    public class EmployeeController : Controller
    {
        private const string EMPLOYEE_SEARCH = "EmployeeSearchInput";
        private readonly IWebHostEnvironment _hostEnvironment;

        public EmployeeController(IWebHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
        }
        /// <summary>
        /// Tìm kiếm và hiển thị danh sách nhân viên
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(EMPLOYEE_SEARCH);
            if (input == null)
                input = new PaginationSearchInput() { Page = 1, PageSize = ApplicationContext.PageSize, SearchValue = "" };
            return View(input);
        }
        /// <summary>
        /// Tìm kiếm và trả về kết quả tìm kiếm nhân viên
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await HRDataService.ListEmployeesAsync(input);
            ApplicationContext.SetSessionData(EMPLOYEE_SEARCH, input);
            return View(result);
        }
        /// <summary>
        /// Bổ sung nhân viên
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhân viên";
            var model = new Employee() { EmployeeID = 0, Photo = "nophoto.png" };
            return View("Edit", model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật nhân viên";
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null) return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Employee data, IFormFile? uploadPhoto)
        {
            try
            {
                //Kiểm tra dữ liệu đầu vào: FullName và Email là bắt buộc, Email chưa được sử dụng bởi nhân viên khác
                if (string.IsNullOrWhiteSpace(data.FullName)) ModelState.AddModelError(nameof(data.FullName), "Họ tên không được trống");
                if (string.IsNullOrWhiteSpace(data.Email)) ModelState.AddModelError(nameof(data.Email), "Email không được trống");
                if (!ModelState.IsValid) return View("Edit", data);

                // Xử lý upload ảnh
                if (uploadPhoto != null)
                {
                    string fileName = $"{DateTime.Now.Ticks}_{uploadPhoto.FileName}";
                    string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/employees", fileName);
                    using (var stream = new FileStream(path, FileMode.Create)) { await uploadPhoto.CopyToAsync(stream); }
                    data.Photo = fileName;
                }

                //Tiền xử lý dữ liệu trước khi lưu vào database
                if (string.IsNullOrEmpty(data.Address)) data.Address = "";
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (string.IsNullOrEmpty(data.Photo)) data.Photo = "nophoto.png";

                if (data.EmployeeID == 0) await HRDataService.AddEmployeeAsync(data);
                else await HRDataService.UpdateEmployeeAsync(data);

                return RedirectToAction("Index"); // Chuyển về trang danh sách
            }
            catch//(Exception ex)
            {
                //TODO: Ghi log lỗi căn cứ vào ex.Message và ex.StackTrace
                ModelState.AddModelError("", "Lỗi hệ thống.");
                return View("Edit", data);
            }
        }
        /// <summary>
        /// Xóa nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null) return RedirectToAction("Index");
            ViewBag.CanDelete = !(await HRDataService.IsUsedEmployeeAsync(id));
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await HRDataService.DeleteEmployeeAsync(id);
            return RedirectToAction("Index");
        }
        /// <summary>
        /// Đổi mật khẩu cho nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên cần đổi mật khẩu</param>
        /// <returns></returns>
        public async Task<IActionResult> ChangePassword(int id)
        {
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null) return RedirectToAction("Index");
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> UpdatePassword(int employeeID, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword != confirmPassword)
                return RedirectToAction("ChangePassword", new { id = employeeID });

            //await HRDataService.UpdatePasswordAsync(employeeID, newPassword);
            return RedirectToAction("Index"); // Chuyển về trang danh sách
        }
        /// <summary>
        /// Phân quyền cho nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên cần phân quyền</param>
        /// <returns></returns>
        public async Task<IActionResult> ChangeRole(int id)
        {
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null) return RedirectToAction("Index");
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> SaveRole(int employeeID, string[] roles)
        {
            ViewBag.Title = "Thay đổi quyền hạn nhân viên";
            return RedirectToAction("Index"); 
        }
    }
}