namespace Models.EF
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("AttributeValue")]
    public partial class AttributeValue
    {
        [Key]
        public int ValueId { get; set; }

        public int AttributeId { get; set; }

        [StringLength(250)]
        public string Value { get; set; } // VD: "Red", "XL"

        [StringLength(250)]
        public string DisplayValue { get; set; } // Tên hiển thị (nếu cần)

        // --- THÊM DÒNG NÀY ĐỂ SỬA LỖI ---
        public int? DisplayOrder { get; set; } // Thứ tự hiển thị (VD: S=1, M=2, L=3)

        public bool IsActive { get; set; }

        // Khóa ngoại trỏ về Attribute cha
        // Navigation
        [ForeignKey("AttributeId")]
        public virtual Attribute Attribute { get; set; }
        public virtual ICollection<VariantAttributeValue> VariantAttributeValues { get; set; }


    }
}