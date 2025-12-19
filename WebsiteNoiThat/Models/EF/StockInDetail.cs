namespace Models.EF
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("StockInDetail")]
    public partial class StockInDetail
    {
        [Key]
        public int DetailId { get; set; }

        public int StockInId { get; set; }

        public int VariantId { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

        [StringLength(255)]
        public string Note { get; set; }

        [ForeignKey("StockInId")]
        public virtual StockIn StockIn { get; set; }

        [ForeignKey("VariantId")]
        public virtual ProductVariant ProductVariant { get; set; }
    }
}