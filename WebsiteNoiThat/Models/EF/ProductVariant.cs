namespace Models.EF
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("ProductVariant")]
    public partial class ProductVariant
    {
        [Key]
        public int VariantId { get; set; }

        public int ProductId { get; set; }

        [Required]
        [StringLength(50)]
        public string SKU { get; set; }

        public decimal Price { get; set; }

        public decimal? SalePrice { get; set; }

        public int? StockQuantity { get; set; }

        [StringLength(255)]
        public string ImageVariant { get; set; }

        public bool? IsActive { get; set; }

        public DateTime? CreatedAt { get; set; }

        // ===== THÊM MỚI =====
        public int? WarehouseId { get; set; }

        // Relationships
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        [ForeignKey("WarehouseId")]
        public virtual Warehouse Warehouse { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<VariantAttributeValue> VariantAttributeValues { get; set; }
    }
}