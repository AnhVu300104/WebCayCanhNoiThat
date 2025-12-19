namespace Models.EF
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("StockOutDetail")]
    public partial class StockOutDetail
    {
        [Key]
        public int DetailId { get; set; }

        public int StockOutId { get; set; }

        public int VariantId { get; set; }

        public int Quantity { get; set; }

        [StringLength(255)]
        public string Note { get; set; }

        [ForeignKey("StockOutId")]
        public virtual StockOut StockOut { get; set; }

        [ForeignKey("VariantId")]
        public virtual ProductVariant ProductVariant { get; set; }
    }
}