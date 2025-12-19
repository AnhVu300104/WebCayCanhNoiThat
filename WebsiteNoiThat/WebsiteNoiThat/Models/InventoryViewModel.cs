using System;

namespace WebsiteNoiThat.Models
{
    /// <summary>
    /// ViewModel để hiển thị thông tin tồn kho của sản phẩm trong kho
    /// </summary>
    public class InventoryViewModel
    {
        /// <summary>
        /// ID của biến thể sản phẩm
        /// </summary>
        public int VariantId { get; set; }

        /// <summary>
        /// ID của kho hàng
        /// </summary>
        public int WarehouseId { get; set; }

        /// <summary>
        /// Số lượng tồn kho hiện tại
        /// </summary>
        public int CurrentQuantity { get; set; }

        /// <summary>
        /// Giá vốn trung bình (Average Cost) - Lấy từ Price của ProductVariant
        /// </summary>
        public decimal AverageCost { get; set; }

        /// <summary>
        /// Giá bán (Sale Price) - Lấy từ SalePrice của ProductVariant
        /// </summary>
        public decimal SalePrice { get; set; }

        /// <summary>
        /// Tên sản phẩm
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// Mã sản phẩm (SKU)
        /// </summary>
        public string ProductCode { get; set; }

        /// <summary>
        /// Tên biến thể (màu sắc, kích thước, v.v.)
        /// </summary>
        public string VariantName { get; set; }

        /// <summary>
        /// Tính lợi nhuận (giá bán - giá vốn)
        /// </summary>
        public decimal Profit
        {
            get { return SalePrice - AverageCost; }
        }

        /// <summary>
        /// Tính % lợi nhuận
        /// </summary>
        public decimal ProfitPercent
        {
            get
            {
                if (AverageCost == 0) return 0;
                return (Profit / AverageCost) * 100;
            }
        }

        /// <summary>
        /// Tổng giá trị tồn kho (số lượng × giá vốn)
        /// </summary>
        public decimal TotalValue
        {
            get { return CurrentQuantity * AverageCost; }
        }
    }
}