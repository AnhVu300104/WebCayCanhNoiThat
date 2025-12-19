namespace Models.EF
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("Order")]
    public partial class Order
    {
        [Key]
        public int OrderId { get; set; }

        public int? UserId { get; set; }

        [StringLength(50)]
        public string ShipName { get; set; }

        [StringLength(50)]
        public string ShipPhone { get; set; }

        [StringLength(200)]
        public string ShipAddress { get; set; }

        [StringLength(50)]
        public string ShipEmail { get; set; }

        public int? StatusId { get; set; }

        // --- THÊM DÒNG NÀY ĐỂ SỬA LỖI ---
        public DateTime? CreatedDate { get; set; }

        public DateTime? UpdateDate { get; set; } // Nếu chưa có thì thêm luôn

        public virtual User User { get; set; }
        public virtual Status Status { get; set; } // Nếu có relationship
    }
}