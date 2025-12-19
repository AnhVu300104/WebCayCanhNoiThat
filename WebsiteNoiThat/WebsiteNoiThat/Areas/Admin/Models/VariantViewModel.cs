using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebsiteNoiThat.Areas.Admin.Models
{
    public class VariantViewModel
    {
        public int VariantId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn sản phẩm")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Mã SKU là bắt buộc")]
        [StringLength(50, ErrorMessage = "SKU không quá 50 ký tự")]
        [Display(Name = "Mã SKU")]
        public string SKU { get; set; }

        [Required(ErrorMessage = "Giá là bắt buộc")]
        [Range(0, int.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        [Display(Name = "Giá bán")]
        public decimal Price { get; set; }

        [Display(Name = "Giá khuyến mãi")]
        public decimal? SalePrice { get; set; }

        [Display(Name = "Số lượng tồn")]
        public int? StockQuantity { get; set; }

        [Display(Name = "Ảnh variant")]
        [StringLength(255)]
        public string ImageVariant { get; set; }

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; }

        // Hiển thị
        public string ProductName { get; set; }
        public List<string> Attributes { get; set; }
        public DateTime? CreatedAt { get; set; }

        public VariantViewModel()
        {
            IsActive = true;
            StockQuantity = 0;
            Attributes = new List<string>();
        }
    }
}