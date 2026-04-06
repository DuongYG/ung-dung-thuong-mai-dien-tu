using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SV22T1020583.BusinessLayers;
using SV22T1020583.Models.Sales;
using SV22T1020583.Shop.Models;
using Newtonsoft.Json;

namespace SV22T1020583.Shop.Controllers
{
    /// <summary>
    /// Quản lý đơn hàng
    /// </summary>
    [Authorize]
    public class OrderController : Controller
    {
        private const string CART_SESSION_KEY = "MyCart";
        /// <summary>
        /// Hiển thị danh sách các đơn hàng mà khách hàng đã đặt trước đó
        /// </summary>
        /// <returns>Danh sách lịch sử đơn hàng của khách hàng</returns>
        public async Task<IActionResult> History()
        {
            // Lấy ID khách hàng từ Claim định danh khi đăng nhập
            var claim = User.FindFirst("CustomerID");
            if (claim == null) return RedirectToAction("Login", "Account");

            int customerID = int.Parse(claim.Value);

            var input = new OrderSearchInput()
            {
                Page = 1,
                PageSize = 100, // Lấy tối đa 100 đơn gần nhất
                SearchValue = customerID.ToString(), // Truyền ID vào đây
                Status = 0 // 0 thường quy ước là lấy tất cả trạng thái
            };

            var result = await SalesDataService.ListOrdersAsync(input);
            return View(result.DataItems);
        }
        /// <summary>
        /// Hiển thị thông tin chi tiết của một đơn hàng cụ thể
        /// </summary>
        /// <param name="id">Mã đơn hàng cần xem</param>
        /// <returns>Trang hiển thị chi tiết đơn hàng</returns>
        public async Task<IActionResult> Details(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return RedirectToAction("History");

            int customerID = int.Parse(User.FindFirst("CustomerID")!.Value);

            if (order.CustomerID != customerID)
                return RedirectToAction("History");

            var orderDetails = await SalesDataService.ListDetailsAsync(id);

            ViewBag.OrderDetails = orderDetails;

            return View(order);
        }
        /// <summary>
        /// Hiển thị trang thanh toán với danh sách sản phẩm trong giỏ hàng
        /// </summary>
        /// <returns>Trang Checkout</returns>
        public async Task<IActionResult> Checkout()
        {
            var cart = GetCart();
            if (cart.Count == 0) return RedirectToAction("Index", "Cart");

            // Lấy CustomerID từ người dùng đã đăng nhập
            int customerID = int.Parse(User.FindFirst("CustomerID")?.Value ?? "0");
            ViewBag.Customer = await PartnerDataService.GetCustomerAsync(customerID);

            return View(cart);
        }
        /// <summary>
        /// Xử lý việc tạo đơn hàng khi khách hàng hoàn tất đặt hàng
        /// </summary>
        /// <param name="deliveryProvince">Tỉnh/thành giao hàng</param>
        /// <param name="deliveryAddress">Địa chỉ giao hàng</param>
        /// <returns>
        /// Nếu đặt hàng thành công sẽ chuyển đến trang Success,
        /// nếu có lỗi sẽ quay lại trang Checkout
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoCheckout(string deliveryProvince, string deliveryAddress)
        {
            var cart = GetCart();
            if (cart.Count == 0) return RedirectToAction("Index", "Cart");

            // Kiểm tra tính hợp lệ của dữ liệu đầu vào
            if (string.IsNullOrEmpty(deliveryProvince) || string.IsNullOrEmpty(deliveryAddress))
            {
                ModelState.AddModelError("Error", "Vui lòng chọn Tỉnh/Thành và nhập địa chỉ giao hàng.");
                int currentUserId = int.Parse(User.FindFirst("CustomerID")?.Value ?? "0");
                ViewBag.Customer = await PartnerDataService.GetCustomerAsync(currentUserId);

                return View("Checkout", cart);
            }

            try
            {
                int customerID = int.Parse(User.FindFirst("CustomerID")?.Value ?? "0");
                int orderID = await SalesDataService.AddOrderAsync(customerID, deliveryProvince, deliveryAddress);

                if (orderID > 0)
                {
                    foreach (var item in cart)
                    {
                        await SalesDataService.AddDetailAsync(new OrderDetail()
                        {
                            OrderID = orderID,
                            ProductID = item.ProductID,
                            Quantity = item.Quantity,
                            SalePrice = item.UnitPrice
                        });
                    }
                    HttpContext.Session.Remove(CART_SESSION_KEY);
                    return RedirectToAction("Success", new { id = orderID });
                }
                else
                {
                    ModelState.AddModelError("Error", "Không thể tạo đơn hàng. Vui lòng thử lại sau.");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", "Lỗi hệ thống: " + ex.Message);
            }

            // Nếu có lỗi, quay lại trang Checkout và nạp lại thông tin khách
            int retryId = int.Parse(User.FindFirst("CustomerID")?.Value ?? "0");
            ViewBag.Customer = await PartnerDataService.GetCustomerAsync(retryId);
            return View("Checkout", cart);
        }
        /// <summary>
        /// Hiển thị thông báo đặt hàng thành công
        /// </summary>
        /// <param name="id">Mã đơn hàng vừa được tạo</param>
        /// <returns>Trang thông báo thành công</returns>
        public IActionResult Success(int id)
        {
            return View(id);
        }
        /// <summary>
        /// Lấy danh sách sản phẩm trong giỏ hàng từ Session
        /// </summary>
        /// <returns>Danh sách CartItem trong giỏ hàng</returns>
        private List<CartItem> GetCart()
        {
            var sessionData = HttpContext.Session.GetString(CART_SESSION_KEY);
            if (string.IsNullOrEmpty(sessionData)) return new List<CartItem>();
            return JsonConvert.DeserializeObject<List<CartItem>>(sessionData) ?? new List<CartItem>();
        }
    }
}