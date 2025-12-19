using System.ComponentModel.DataAnnotations;

namespace WebsiteNoiThat.Models.ViewModels
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public int? VariantId { get; set; }
        public string ProductName { get; set; }
        public string VariantInfo { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Photo { get; set; }
        public int MaxQuantity { get; set; }

        public decimal TotalPrice => Price * Quantity;
    }

    public class CheckoutViewModel
    {
        [Required, StringLength(100)]
        public string ShipName { get; set; }

        [Required]
        [RegularExpression(@"^(0|\+84)[0-9]{9,10}$")]
        public string ShipPhone { get; set; }

        [Required, EmailAddress]
        public string ShipEmail { get; set; }

        [Required, StringLength(500)]
        public string ShipAddress { get; set; }

        [StringLength(1000)]
        public string Note { get; set; }

        public string PaymentMethod { get; set; }
    }
}
