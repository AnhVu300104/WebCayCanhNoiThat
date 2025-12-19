using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.EF
{
    [Table("ChatMessage")]
    public class ChatMessage
    {
        [Key]
        public int MessageId { get; set; }
        public int UserId { get; set; }
        public string MessageText { get; set; }
        public bool IsFromAdmin { get; set; }
        public DateTime SentAt { get; set; }
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
