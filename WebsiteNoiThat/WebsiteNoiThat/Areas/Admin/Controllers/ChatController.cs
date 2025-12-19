using Models.EF;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Collections.Generic;

namespace WebsiteNoiThat.Areas.Admin.Controllers
{
    public class ChatController : Controller
    {
        private DBNoiThat db = new DBNoiThat();

        // 👉 Trang chính chat
        public ActionResult Index()
        {
            var users = db.Users.ToList();
            return View(users);
        }

        // 👉 Partial user-list (có search)
        public ActionResult UserList(string search = "")
        {
            var q = db.Users.Where(u => u.GroupId == "USER");

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(u =>
                    (u.Name != null && u.Name.ToLower().Contains(s)) ||
                    (u.Username != null && u.Username.ToLower().Contains(s)));
            }

            var users = q.OrderBy(u => u.Name).ToList();
            return PartialView("UserList", users);
        }



        // 👉 Partial conversation
        public ActionResult Conversation(int? userId)
        {
            if (userId == null)
                return PartialView(new List<ChatMessage>());

            var customer = db.Users.Find(userId);
            if (customer == null) return HttpNotFound();

            var messages = db.ChatMessages
                .Where(m => m.UserId == userId)
                .OrderBy(m => m.SentAt)
                .ToList();

            ViewBag.Customer = customer;
            return PartialView(messages);
        }

        // 👉 Gửi tin nhắn admin
        [HttpPost]
        public ActionResult SendMessage(int userId, string messageText)
        {
            if (string.IsNullOrWhiteSpace(messageText))
                return Json(new { success = false, error = "Tin nhắn trống" });

            try
            {
                // Kiểm tra user có tồn tại không
                var user = db.Users.Find(userId);
                if (user == null)
                    return Json(new { success = false, error = "Người dùng không tồn tại" });

                // Tạo message
                var msg = new ChatMessage
                {
                    UserId = userId,
                    MessageText = messageText,
                    IsFromAdmin = true,
                    SentAt = DateTime.Now
                };

                db.ChatMessages.Add(msg);
                db.SaveChanges(); // commit DB

                // Trả về thành công và message mới (dễ update UI)
                return Json(new
                {
                    success = true,
                    message = new
                    {
                        msg.UserId,
                        msg.MessageText,
                        msg.IsFromAdmin,
                        SentAt = msg.SentAt.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                });
            }
            catch (Exception ex)
            {
                // Trả lỗi ra để debug
                return Json(new { success = false, error = ex.Message });
            }
        }


        // 👉 Lấy tin nhắn qua AJAX (nếu cần)
        public ActionResult GetMessages(int userId)
        {
            var messages = db.ChatMessages
                .Where(m => m.UserId == userId)
                .OrderBy(m => m.SentAt)
                .Select(m => new {
                    m.MessageText,
                    m.IsFromAdmin,
                    SentAt = m.SentAt.ToString("HH:mm:ss")
                }).ToList();

            return Json(messages, JsonRequestBehavior.AllowGet);
        }

        // 👉 Lấy tin nhắn cuối (update last-message sidebar)
        // Lấy last message của tất cả user (sidebar real-time)

        public JsonResult GetLastMessageAll(string search = "")
{
    var users = db.Users.AsQueryable();

    if (!string.IsNullOrEmpty(search))
    {
        search = search.ToLower();
        users = users.Where(u => u.Name.ToLower().Contains(search) 
                              || u.Username.ToLower().Contains(search));
    }

    var userList = users
        .Select(u => new
        {
            u.UserId,
            u.Name,
            u.Username,
            LastMessage = db.ChatMessages
                            .Where(m => m.UserId == u.UserId)
                            .OrderByDescending(m => m.SentAt)
                            .Select(m => m.MessageText)
                            .FirstOrDefault() ?? "" ,
            LastMessageTime = db.ChatMessages
                            .Where(m => m.UserId == u.UserId)
                            .OrderByDescending(m => m.SentAt)
                            .Select(m => m.SentAt)
                            .FirstOrDefault(),
            IsFromAdmin = db.ChatMessages
                            .Where(m => m.UserId == u.UserId)
                            .OrderByDescending(m => m.SentAt)
                            .Select(m => m.IsFromAdmin)
                            .FirstOrDefault()

        })
        .OrderByDescending(u => u.LastMessageTime) // ← sắp xếp theo tin nhắn mới nhất
        .ToList();
    

    return Json(userList, JsonRequestBehavior.AllowGet);
}







    }
}
