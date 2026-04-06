using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SV22T1020583.BusinessLayers;
using SV22T1020583.Shop.Models;
using Newtonsoft.Json;

namespace SV22T1020583.Shop.Controllers
{
    /// <summary>
    /// Quản lý giỏ hàng
    /// </summary>
    [Authorize]
    public class CartController : Controller
    {
        private const string CART_SESSION_KEY = "MyCart";
        /// <summary>
        /// Hiển thị danh sách các sản phẩm hiện có trong giỏ hàng
        /// </summary>
        /// <returns>Trang giỏ hàng</returns>
        public IActionResult Index()
        {
            return View(GetCart());
        }
        /// <summary>
        /// Thêm một sản phẩm vào giỏ hàng
        /// </summary>
        /// <param name="productID">Mã sản phẩm cần thêm</param>
        /// <param name="quantity">Số lượng sản phẩm</param>
        /// <returns>Chuyển về trang giỏ hàng sau khi thêm</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productID, int quantity = 1)
        {
            if (productID <= 0) return RedirectToAction("Index", "Product");

            if (quantity <= 0) quantity = 1;

            var product = await CatalogDataService.GetProductAsync(productID);
            if (product == null)
                return RedirectToAction("Index", "Product");

            var cart = GetCart();
            var item = cart.FirstOrDefault(m => m.ProductID == productID);

            if (item == null)
            {
                cart.Add(new CartItem
                {
                    ProductID = product.ProductID,
                    ProductName = product.ProductName,
                    Photo = product.Photo,
                    UnitPrice = product.Price,
                    Quantity = quantity
                });
            }
            else
            {
                item.Quantity += quantity;
            }

            SaveCart(cart);
            return RedirectToAction("Index");
        }
        /// <summary>
        /// Cập nhật số lượng của một sản phẩm trong giỏ hàng
        /// </summary>
        /// <param name="productID">Mã sản phẩm</param>
        /// <param name="quantity">Số lượng mới</param>
        /// <returns>Chuyển về trang giỏ hàng</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateQuantity(int productID, int quantity)
        {
            if (productID <= 0)
                return RedirectToAction("Index");

            var cart = GetCart();
            var item = cart.FirstOrDefault(m => m.ProductID == productID);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }

                SaveCart(cart);
            }

            return RedirectToAction("Index");
        }
        /// <summary>
        /// Xóa một sản phẩm khỏi giỏ hàng
        /// </summary>
        /// <param name="id">Mã sản phẩm cần xóa</param>
        /// <returns>Chuyển về trang giỏ hàng</returns>
        [HttpPost]
        public IActionResult RemoveFromCart(int id)
        {
            if (id <= 0)
                return RedirectToAction("Index");

            var cart = GetCart();
            var item = cart.FirstOrDefault(m => m.ProductID == id);

            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }

            return RedirectToAction("Index");
        }
        /// <summary>
        /// Xóa toàn bộ sản phẩm trong giỏ hàng
        /// </summary>
        /// <returns>Chuyển về trang giỏ hàng</returns>
        [HttpPost]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(CART_SESSION_KEY);
            return RedirectToAction("Index");
        }
        /// <summary>
        /// Lấy danh sách sản phẩm trong giỏ hàng từ Session
        /// </summary>
        /// <returns>Danh sách CartItem</returns>
        private List<CartItem> GetCart()
        {
            var sessionData = HttpContext.Session.GetString(CART_SESSION_KEY);

            if (string.IsNullOrEmpty(sessionData))
                return new List<CartItem>();

            try
            {
                var cart = JsonConvert.DeserializeObject<List<CartItem>>(sessionData);
                return cart ?? new List<CartItem>();
            }
            catch
            {
                return new List<CartItem>();
            }
        }
        /// <summary>
        /// Lưu danh sách sản phẩm trong giỏ hàng vào Session
        /// </summary>
        /// <param name="cart">Danh sách sản phẩm cần lưu</param>
        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString(CART_SESSION_KEY, JsonConvert.SerializeObject(cart));
        }
    }
}