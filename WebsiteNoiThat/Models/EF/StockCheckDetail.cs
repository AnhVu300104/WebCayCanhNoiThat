namespace Models.EF
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("StockCheckDetail")]
    public partial class StockCheckDetail
    {
        [Key]
        public int DetailId { get; set; }

        public int CheckId { get; set; }

        public int VariantId { get; set; }

        public int SystemQuantity { get; set; }

        public int ActualQuantity { get; set; }

        public int Difference { get; set; }

        [StringLength(255)]
        public string Note { get; set; }

        [ForeignKey("CheckId")]
        public virtual StockCheck StockCheck { get; set; }

        [ForeignKey("VariantId")]
        public virtual ProductVariant ProductVariant { get; set; }
    }
}