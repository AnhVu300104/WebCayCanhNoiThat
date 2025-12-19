using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.EF
{
    [Table("CartItem")]
    public partial class CartItem
    {
        [Key]
        public int CartItemId { get; set; }

        public int UserId { get; set; }

        public int ProductId { get; set; }

        // --- CÁC TRƯỜNG MỚI THÊM (Quản lý biến thể trong giỏ) ---
        public int? VariantId { get; set; }

        public int Quantity { get; set; }

        public int Price { get; set; }

        public DateTime CreateDate { get; set; }

        public DateTime? UpdateDate { get; set; }

        // --- NAVIGATION PROPERTIES ---

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        // Liên kết tới bảng biến thể mới
        [ForeignKey("VariantId")]
        public virtual ProductVariant Variant { get; set; }
    }
}