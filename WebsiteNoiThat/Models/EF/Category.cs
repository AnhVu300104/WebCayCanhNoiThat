namespace Models.EF
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Category")]
    public partial class Category
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int CategoryId { get; set; }

        [StringLength(50)]
        [Display(Name = "Tên loại sản phẩm")]
        public string Name { get; set; }

        [StringLength(50)]
        [Display(Name = "Meta Title")]
        public string MetaTitle { get; set; }

        [Display(Name = "Mã cha (ParId)")]
        public int? ParId { get; set; }

        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; }
    }
}