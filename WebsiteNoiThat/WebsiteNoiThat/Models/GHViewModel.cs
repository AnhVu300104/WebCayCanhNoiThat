using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebsiteNoiThat.Models
{
    public class GHViewModel
    {
            public int CartItemId { get; set; }       // Id của CartItem
            public int ProductId { get; set; }        // Id sản phẩm
            public string ProductName { get; set; }   // Tên sản phẩm
            public int? VariantId { get; set; }       // Id biến thể nếu có
            public string VariantInfo { get; set; }   // Thông tin biến thể (size, màu,…)
            public int Quantity { get; set; }         // Số lượng
            public int Price { get; set; }            // Giá bán hiện tại (có giảm giá nếu có biến thể)
            public int MaxQuantity { get; set; }      // Số lượng tồn kho tối đa
            public string Photo { get; set; }         // Ảnh sản phẩm/biến thể
            public DateTime? CreateDate { get; set; } // Ngày thêm vào giỏ
            public DateTime? UpdateDate { get; set; } // Ngày cập nhật giỏ
        }
    
}