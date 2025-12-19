using Models.EF;
using System.Collections.Generic;

namespace Models.ViewModels
{
    public class ProductVariantViewModel
    {
        public int VariantId { get; set; }
        public string SKU { get; set; }
        public decimal Price { get; set; }
        public decimal? SalePrice { get; set; }
        public int? StockQuantity { get; set; }
        public string Image { get; set; }

        // Chuỗi mô tả thuộc tính (VD: "Màu: Đỏ, Size: L")
        public string VariantName { get; set; }

        // Danh sách các thuộc tính chi tiết (nếu cần xử lý thêm)
        public List<string> AttributesInfo { get; set; }
        public List<VariantAttributeValue> VariantAttributeValues { get; set; }

    }
}