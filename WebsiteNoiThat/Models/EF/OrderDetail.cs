namespace Models.EF
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("OrderDetail")]
    public partial class OrderDetail
    {
        [Key]
        [DisplayName("Mã chi tiết hoá đơn")]
        public int OrderDetailId { get; set; }

        [DisplayName("Mã hoá đơn")]
        public int? OrderId { get; set; }

        [DisplayName("Mã sản phẩm")]
        public int? ProductId { get; set; }

        [DisplayName("Đơn giá")]
        public int? Price { get; set; }

        [DisplayName("Số lượng")]
        public int? Quantity { get; set; }

        // --- CÁC TRƯỜNG MỚI THÊM (Quản lý biến thể) ---

        [DisplayName("Mã biến thể")]
        public int? VariantId { get; set; }

        [StringLength(200)]
        [DisplayName("Thông tin biến thể")]
        public string VariantInfo { get; set; }

        // --- NAVIGATION PROPERTIES ---

        [ForeignKey("VariantId")]
        public virtual ProductVariant Variant { get; set; }

        // Các thuộc tính điều hướng cũ (nếu cần thiết cho việc truy vấn ngược)
        
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
        
    }
}