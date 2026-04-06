using Newtonsoft.Json;
using SV22T1020583.Shop.Models;

public static class CartHelper
{
    private const string CART_KEY = "ShopCart";

    public static List<CartItem> GetCart(HttpContext context)
    {
        var data = context.Session.GetString(CART_KEY);
        return data == null ? new List<CartItem>() : JsonConvert.DeserializeObject<List<CartItem>>(data);
    }

    public static void SaveCart(HttpContext context, List<CartItem> cart)
    {
        context.Session.SetString(CART_KEY, JsonConvert.SerializeObject(cart));
    }
}