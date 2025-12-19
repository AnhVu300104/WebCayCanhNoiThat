namespace Models.EF
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("StockHistory")]
    public partial class StockHistory
    {
        [Key]
        public int HistoryId { get; set; }

        public int WarehouseId { get; set; }

        public int VariantId { get; set; }

        [Required]
        [StringLength(20)]
        public string Type { get; set; }

        [StringLength(20)]
        public string ReferenceType { get; set; }

        public int? ReferenceId { get; set; }

        public int QuantityBefore { get; set; }

        public int QuantityChange { get; set; }

        public int QuantityAfter { get; set; }

        [StringLength(500)]
        public string Note { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime? CreatedAt { get; set; }

        [ForeignKey("WarehouseId")]
        public virtual Warehouse Warehouse { get; set; }

        [ForeignKey("VariantId")]
        public virtual ProductVariant ProductVariant { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User UserCreated { get; set; }
    }
}