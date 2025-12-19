using Models.EF;
using System;
using System.Linq;
using System.Web.Mvc;

namespace WebsiteNoiThat.Areas.Admin.Controllers
{
    public class ConversationController : Controller
    {
        private DBNoiThat db = new DBNoiThat();

        // 📋 Danh sách khách hàng có chat
        public ActionResult Index()
        {
            var users = db.Users
                .Where(u => db.ChatMessages.Any(m => m.UserId == u.UserId))
                .ToList();
            return View(users);
        }

        // 💬 Chi tiết hội thoại
        public ActionResult Conversation(int userId)
        {
            var customer = db.Users.Find(userId);
            var messages = db.ChatMessages
                .Where(m => m.UserId == userId)
                .OrderBy(m => m.SentAt)
                .ToList();

            ViewBag.Customer = customer;
            return View(messages);
        }

        // 🚀 Gửi tin nhắn từ admin
        [HttpPost]
        public ActionResult SendMessage(int userId, string messageText)
        {
            db.ChatMessages.Add(new ChatMessage
            {
                UserId = userId,
                MessageText = messageText,
                IsFromAdmin = true,
                SentAt = DateTime.Now
            });
            db.SaveChanges();
            return Json(new { success = true });
        }
    }
}
