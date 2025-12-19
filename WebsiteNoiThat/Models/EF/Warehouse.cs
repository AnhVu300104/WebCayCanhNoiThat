namespace Models.EF
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("Warehouse")]
    public partial class Warehouse
    {
        [Key]
        public int WarehouseId { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Address { get; set; }

        [StringLength(20)]
        public string Phone { get; set; }

        [StringLength(100)]
        public string ManagerName { get; set; }

        [StringLength(20)]
        public string ManagerPhone { get; set; }

        [StringLength(20)]
        public string Type { get; set; }

        public bool IsActive { get; set; }

        public DateTime? CreatedAt { get; set; }

        public virtual ICollection<WarehouseStock> WarehouseStocks { get; set; }
    }
}