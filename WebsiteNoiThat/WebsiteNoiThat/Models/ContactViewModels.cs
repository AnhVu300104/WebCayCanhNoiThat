using System.ComponentModel.DataAnnotations;

namespace WebsiteNoiThat.Models.ViewModels
{
    public class ContactViewModel
    {
        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        [RegularExpression(@"^(0|\+84)[0-9]{9,10}$")]
        public string Phone { get; set; }

        [Required, StringLength(200)]
        public string Subject { get; set; }

        [Required, StringLength(2000)]
        public string Message { get; set; }
    }
}
