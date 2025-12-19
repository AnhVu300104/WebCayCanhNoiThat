namespace Models.EF
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("StockCheck")]
    public partial class StockCheck
    {
        [Key]
        public int CheckId { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; }

        public int WarehouseId { get; set; }

        public DateTime CheckDate { get; set; }

        [StringLength(500)]
        public string Note { get; set; }

        [StringLength(20)]
        public string Status { get; set; }

        public int? CreatedBy { get; set; }

        public int? ApprovedBy { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        [ForeignKey("WarehouseId")]
        public virtual Warehouse Warehouse { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User UserCreated { get; set; }

        public virtual ICollection<StockCheckDetail> StockCheckDetails { get; set; }
    }
}