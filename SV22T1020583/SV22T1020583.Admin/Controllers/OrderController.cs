using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020583.Admin.AppCodes;
using SV22T1020583.BusinessLayers;
using SV22T1020583.Models.Catalog;
using SV22T1020583.Models.Sales;
using System.Globalization;

namespace SV22T1020583.Admin.Controllers
{
    /// <summary>
    /// Các chức năng liên quan đến nghiệp vụ bán hàng
    /// </summary>
    [Authorize]
    public class OrderController : Controller
    {
        private const string ORDER_SEARCH = "OrderSearch";
        private const string PRODUCT_SEARCH = "SearchProductToSale";

        /// <summary>
        /// Nhập đầu vào tìm kiếm và kết quả tìm kiếm đơn hàng
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<OrderSearchInput>(ORDER_SEARCH);
            if (input == null)
                input = new OrderSearchInput()
                {
                    Status = 0,
                    DateFrom = null,
                    DateTo = null,
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = ""
                };

            return View(input);
        }

        /// <summary>
        /// Tìm kiếm đơn hàng 
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Search(OrderSearchInput input)
        {
            // Hàm parse dùng chung (Parse date về lại)
            DateTime? ParseDate(string key)
            {
                if (!Request.HasFormContentType || !Request.Form.ContainsKey(key))
                    return null;

                var raw = Request.Form[key].ToString();

                if (string.IsNullOrWhiteSpace(raw))
                    return null;

                string[] formats = { "dd/MM/yyyy", "yyyy-MM-dd" };

                if (DateTime.TryParseExact(raw, formats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var date))
                {
                    return date;
                }

                return null;
            }

            var dateFrom = ParseDate("DateFrom");
            var dateTo = ParseDate("DateTo");

            if (dateFrom.HasValue)
                input.DateFrom = dateFrom;

            if (dateTo.HasValue)
                input.DateTo = dateTo;

            var result = await SalesDataService.ListOrdersAsync(input);
            ApplicationContext.SetSessionData(ORDER_SEARCH, input);

            return PartialView(result);
        }

        //public async Task<IActionResult> Search(OrderSearchInput input)
        //{
        //    var result = await SalesDataService.ListOrdersAsync(input); 
        //    ApplicationContext.SetSessionData(ORDER_SEARCH, input); 

        //    return PartialView(result);
        //}

        /// <summary>
        /// Giao diện gồm các chức năng hỗ trợ cho nghiệp vụ tạo đơn hàng mới
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(PRODUCT_SEARCH);
            if (input == null)
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = 3
                };

            return View(input);
        }

        /// <summary>
        /// Tìm kiếm và hiển thị danh sách sản phẩm để add vào giỏ
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<IActionResult> SearchProduct(ProductSearchInput input)
        {
            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(PRODUCT_SEARCH, input);

            return View(result);
        }

        /// <summary>
        /// Hiển thị giỏ hàng
        /// </summary>
        /// <returns></returns>
        public IActionResult ShowCart()
        {
            var cart = ShoppingCartService.GetShoppingCart();
            return View(cart);
        }

        /// <summary>
        /// Thêm 1 mặt hàng vào giỏ
        /// </summary>
        /// <param name="productID"></param>
        /// <param name="quantity"></param>
        /// <param name="price"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> AddCartItem(int productID, int quantity, decimal price)
        {
            //Xử lý dữ liệu đầu vào
            if (quantity <= 0) return Json(new ApiResult(0, "Số lượng không hợp lệ"));
            if (price < 0) return Json(new ApiResult(0, "Giá bán không hợp lệ"));

            var product = await CatalogDataService.GetProductAsync(productID);
            if (product == null) return Json(new ApiResult(0, "Mặt hàng không tồn tại"));

            if (!product.IsSelling) return Json(new ApiResult(0, "Mặt hàng này hiện đã ngưng bán"));

            //Add vào giỏ
            ShoppingCartService.AddItemToCart(new OrderDetailViewInfo()
            {
                ProductID = productID,
                Quantity = quantity,
                SalePrice = price,
                ProductName = product.ProductName,
                Unit = product.Unit,
                Photo = product.Photo ?? "nophoto.png"
            });

            return Json(new ApiResult(1));
        }

        /// <summary>
        /// Cập nhật thông tin (số lượng, giá bán) của một mặt hàng trong một giỏ hàng hoặc một đơn hàng
        /// </summary>
        /// <param name="id">0: Cập nhật cho giỏ hàng, khác 0: Mã đơn hàng cần xử lý</param>
        /// <param name="productId">Mã mặt hàng</param>
        /// <returns></returns>
        public IActionResult EditCartItem(int productId = 0)
        {
            var item = ShoppingCartService.GetCartItem(productId);
            return PartialView(item); //View không dùng layout
        }

        public IActionResult UpdateCartItem(int productID, int quantity, decimal salePrice)
        {
            //TODO: Kiểm tra dữ liệu 

            ShoppingCartService.UpdateCartItem(productID, quantity, salePrice);
            return Json(new ApiResult(1));
        }

        /// <summary>
        /// Xóa một mặt hàng khỏi giỏ hàng hoặc đơn hàng
        /// </summary>
        /// <param name="productId">Mã mặt hàng</param>
        /// <returns></returns>
        public IActionResult DeleteCartItem(int productId = 0)
        {
            //POST
            if (Request.Method == "POST")
            {
                ShoppingCartService.RemoveCartItem(productId);
                return Json(new ApiResult(1));
            }

            //GET
            var item = ShoppingCartService.GetCartItem(productId);
            return PartialView(item);
        }

        /// <summary>
        /// Xóa giỏ hàng
        /// </summary>
        /// <returns></returns>
        public IActionResult ClearCart()
        {
            if (Request.Method == "POST")
            {
                ShoppingCartService.ClearCart();
                return RedirectToAction("Create");
            }


            return PartialView();
        }

        public async Task<IActionResult> CreateOrder(int customerID = 0, string province = "", string address = "")
        {
            //TODO: Kiểm tra dữ liệu

            var cart = ShoppingCartService.GetShoppingCart();
            if (cart.Count == 0)
            {
                return Json(new ApiResult(0, "Giỏ hàng đang trống, không lập được đơn hàng"));
            }

            //Tạo đơn hàng mới
            var order = new Order()
            {
                CustomerID = customerID == 0 ? null : customerID,
                DeliveryProvince = province,
                DeliveryAddress = address
            };

            int orderID = await SalesDataService.AddOrderAsync(order);

            //Bổ sung chi tiết cho đơn hàng
            foreach (var item in cart)
            {
                item.OrderID = orderID;
                await SalesDataService.AddDetailAsync(item);
            }
            //Tạo đơn (thêm vào order và orderDetail rồi thì xóa giỏ)
            ShoppingCartService.ClearCart();

            return Json(new ApiResult(orderID));
        }

        /// <summary>
        /// Hiển thị thông tin chi tiết của một đơn hàng và điều hướng đến các chức năng xử lý trên đơn hàng
        /// </summary>
        /// <param name="id">Mã đơn hàng cần xem và xử lý</param>
        /// <returns></returns>
        public async Task<IActionResult> Detail(int id)
        {
            //TODO: Xử lý thêm
            var model = await SalesDataService.GetOrderAsync(id);
            var listDetails = await SalesDataService.ListDetailsAsync(id);
            ViewBag.OrderDetails = listDetails;

            return View(model);
        }

        /// <summary>
        /// Duyệt chấp nhận đơn hàng
        /// </summary>
        /// <param name="id">Mã đơn hàng cần xử lý</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Accept(int id)
        {
            // Giả định EmployeeID = 1 (Trong thực tế lấy từ User.Identity hoặc Session)
            var employeeId = 1;
            var result = await SalesDataService.AcceptOrderAsync(id, employeeId);
            if (result) return Json(new ApiResult(1, "Duyệt đơn hàng thành công"));
            return Json(new ApiResult(0, "Không thể duyệt đơn hàng này"));
        }

        /// <summary>
        /// CHuyển đơn hàng cho người giao hàng
        /// </summary>
        /// <param name="id">Mã đơn hàng cần xử lý</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Shipping(int id, int shipperID)
        {
            if (shipperID <= 0) return Json(new ApiResult(0, "Vui lòng chọn người giao hàng"));

            var result = await SalesDataService.ShipOrderAsync(id, shipperID);
            if (result) return Json(new ApiResult(1, "Đơn hàng đã được chuyển giao"));
            return Json(new ApiResult(0, "Lỗi khi cập nhật giao hàng"));
        }

        /// <summary>
        /// Ghi nhận đơn hàng kết thúc thành công
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Finish(int id)
        {
            var result = await SalesDataService.CompleteOrderAsync(id);
            if (result) return Json(new ApiResult(1, "Đơn hàng đã hoàn tất"));
            return Json(new ApiResult(0, "Lỗi khi kết thúc đơn hàng"));
        }
        /// <summary>
        /// Từ chối đơn hàng(không duyệt)
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var employeeId = 1;

            var result = await SalesDataService.RejectOrderAsync(id, employeeId);
            if (result)
                return Json(new ApiResult(1, "Đơn hàng đã bị từ chối"));

            return Json(new ApiResult(0, "Không thể thực hiện thao tác từ chối đơn hàng này"));
        }
        /// <summary>
        /// Hủy đơn hàng
        /// </summary>
        /// <param name="id">Mã đơn hàng cần xử lý</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            bool result = await SalesDataService.CancelOrderAsync(id);
            return Json(result ? new ApiResult(1) : new ApiResult(0, "Không thể hủy đơn hàng này"));
        }

        /// <summary>
        /// Xóa đơn hàng
        /// </summary>
        /// <param name="id">Mã đơn hàng cần xử lý</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await SalesDataService.DeleteOrderAsync(id);
            if (result) return Json(new ApiResult(1));
            return Json(new ApiResult(0, "Đơn hàng chỉ được xóa khi đang ở trạng thái Vừa tạo, Hủy hoặc Từ chối"));
        }
    }
}
