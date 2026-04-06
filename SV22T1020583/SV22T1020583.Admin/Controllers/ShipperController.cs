using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020583.Admin.AppCodes;
using SV22T1020583.BusinessLayers;
using SV22T1020583.Models.Common;
using SV22T1020583.Models.Partner;

namespace SV22T1020583.Admin.Controllers
{
    /// <summary>
    /// Các chức năng quản lý dữ liệu liên quan đến người giao hàng
    /// </summary>
    [Authorize]
    public class ShipperController : Controller
    {
        /// <summary>
        /// Tên của biến để lưu điều kiện tìm kiếm người giao hàng trong session
        /// </summary>
        private const string SHIPPER_SEARCH = "ShipperSearchInput";

        /// <summary>
        /// Nhập đầu vào tìm kiếm, hiển thị kết quả tìm kiếm trên View
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SHIPPER_SEARCH);
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
        /// Tìm kiếm và trả về kết quả dưới dạng Partial View
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await PartnerDataService.ListShippersAsync(input);
            ApplicationContext.SetSessionData(SHIPPER_SEARCH, input);
            return View(result);
        }

        /// <summary>
        /// Giao diện bổ sung một người giao hàng mới
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung người giao hàng";
            var data = new Shipper() { ShipperID = 0 };
            return View("Edit", data);
        }

        /// <summary>
        /// Giao diện cập nhật thông tin của người giao hàng
        /// </summary>
        /// <param name="id">Mã người giao hàng cần cập nhật</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id = 0)
        {
            ViewBag.Title = "Cập nhật thông tin người giao hàng";
            var data = await PartnerDataService.GetShipperAsync(id);
            if (data == null) return RedirectToAction("Index");
            return View(data);
        }

        /// <summary>
        /// Lưu dữ liệu người giao hàng (Thêm mới hoặc Cập nhật)
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Save(Shipper data)
        {
            if (string.IsNullOrWhiteSpace(data.ShipperName))
                ModelState.AddModelError(nameof(data.ShipperName), "Tên người giao hàng không được để trống");

            if (!ModelState.IsValid)
            {
                ViewBag.Title = data.ShipperID == 0 ? "Bổ sung người giao hàng" : "Cập nhật thông tin người giao hàng";
                return View("Edit", data);
            }

            if (data.ShipperID == 0)
                await PartnerDataService.AddShipperAsync(data);
            else
                await PartnerDataService.UpdateShipperAsync(data);

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Giao diện xác nhận xóa người giao hàng
        /// </summary>
        /// <param name="id">Mã người giao hàng cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id = 0)
        {
            var data = await PartnerDataService.GetShipperAsync(id);
            if (data == null) return RedirectToAction("Index");
            return View(data);
        }

        /// <summary>
        /// Thực hiện xóa người giao hàng khỏi cơ sở dữ liệu
        /// </summary>
        /// <param name="id">Mã người giao hàng</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> DeleteConfirm(int id)
        {
            await PartnerDataService.DeleteShipperAsync(id);
            return RedirectToAction("Index");
        }
    }
}