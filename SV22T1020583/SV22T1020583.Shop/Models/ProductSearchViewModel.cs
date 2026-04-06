using SV22T1020583.Models.Catalog;
using SV22T1020583.Models.Common;

namespace SV22T1020583.Shop.Models
{
    public class ProductSearchViewModel
    {
        public string SearchValue { get; set; } = "";
        public int CategoryID { get; set; } = 0;
        public decimal MinPrice { get; set; } = 0;
        public decimal MaxPrice { get; set; } = 0;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public PagedResult<Product> Result { get; set; } = new PagedResult<Product>();
    }
}