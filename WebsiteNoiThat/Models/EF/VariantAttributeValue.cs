namespace Models.EF
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("VariantAttributeValue")]
    public partial class VariantAttributeValue
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int VariantId { get; set; }

        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int AttributeId { get; set; }

        [Key]
        [Column(Order = 2)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ValueId { get; set; }

        // Navigation Properties
        [ForeignKey("VariantId")]
        public virtual ProductVariant ProductVariant { get; set; }

        [ForeignKey("AttributeId")]
        public virtual Attribute Attribute { get; set; }

        [ForeignKey("ValueId")]
        public virtual AttributeValue AttributeValue { get; set; }
    }
}