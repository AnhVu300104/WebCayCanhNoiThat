namespace Models.EF
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("Attribute")]
    public partial class Attribute
    {
        [Key]
        public int AttributeId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; }

        [StringLength(20)]
        public string DisplayType { get; set; }

        public bool? IsRequired { get; set; }
        public int? DisplayOrder { get; set; }
        public bool IsActive { get; set; }

        public virtual ICollection<AttributeValue> AttributeValues { get; set; }
    }
}