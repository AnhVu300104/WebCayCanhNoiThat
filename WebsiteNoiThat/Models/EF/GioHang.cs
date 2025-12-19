namespace Models.EF
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("GioHang")]
    public partial class GioHang
    {
        [Key]
        public int GioHangId { get; set; }  // Khóa chính

        [Required]
        public int UserId { get; set; }     // FK tới Users

        [Required]
        public int ProductId { get; set; }  // FK tới Products

        [Required]
        public string ProductName { get; set; }

        // Trường mới: ID của biến thể sản phẩm (nullable)
        public int? VariantId { get; set; }

        // Trường mới: Thông tin mô tả biến thể
        [StringLength(500)]
        public string VariantInfo { get; set; }

        [Required]
        public int Quantity { get; set; } = 1;  // Số lượng, mặc định 1

        [Required]
        public DateTime CreateDate { get; set; } = DateTime.Now; // Ngày thêm

        public DateTime? UpdateDate { get; set; } // Ngày cập nhật

        // Navigation properties (liên kết với bảng khác)
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        [ForeignKey("VariantId")]
        public virtual ProductVariant ProductVariant { get; set; }
    }
}