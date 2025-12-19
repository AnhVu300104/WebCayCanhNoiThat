using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.EF
{
    [Table("StockIn")]
    public partial class StockIn
    {
        [Key]
        public int StockInId { get; set; }

        [Required, StringLength(50)]
        public string Code { get; set; }

        [Required]
        public int WarehouseId { get; set; }

        [Required]
        public int ProviderId { get; set; }

        [StringLength(20)]
        public string Type { get; set; } = "purchase";

        public decimal TotalAmount { get; set; } = 0;

        [StringLength(500)]
        public string Note { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "pending";

        public int? CreatedBy { get; set; }
        public int? ApprovedBy { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ApprovedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Navigation Properties
        [ForeignKey("WarehouseId")]
        public virtual Warehouse Warehouse { get; set; }

        [ForeignKey("ProviderId")]
        public virtual Provider Provider { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User UserCreated { get; set; }

        public virtual ICollection<StockInDetail> StockInDetails { get; set; }
    }
}
