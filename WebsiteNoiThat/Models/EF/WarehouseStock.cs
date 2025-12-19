namespace Models.EF
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("WarehouseStock")]
    public partial class WarehouseStock
    {
        [Key]
        public int StockId { get; set; }

        public int WarehouseId { get; set; }

        public int VariantId { get; set; }

        public int? Quantity { get; set; }

        public int? MinStock { get; set; }

        public int? MaxStock { get; set; }

        [StringLength(100)]
        public string Location { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("WarehouseId")]
        public virtual Warehouse Warehouse { get; set; }

        [ForeignKey("VariantId")]
        public virtual ProductVariant ProductVariant { get; set; }
    }
}