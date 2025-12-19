using Models.DAO;
using Models.EF;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using WebsiteNoiThat.Common;

namespace WebsiteNoiThat.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly ProductDao _productDao = new ProductDao();
        private readonly DBNoiThat db = new DBNoiThat();

        // 🏠 Thông tin cửa hàng
        private const string STORE_NAME = "Gỗ Nội Thất Anh Vũ";
        private const string STORE_ADDRESS = "18 Trần Hưng Đạo, Thành phố Huế";
        private const string STORE_PHONE = "0386861263";
        private const string STORE_EMAIL = "supportanhvu@gmail.com"; // sửa gmail

        public ActionResult Index() => View();

        // 💡 Lời chào và gợi ý
        private string GetInitialGreeting()
        {
            return $"<p>Xin chào 👋! Mình là trợ lý ảo của <b>{STORE_NAME}</b>.<br/>Bạn cần tư vấn gì ạ?</p>";
        }

        // 🟢 Lấy toàn bộ lịch sử chat giữa user và admin
        public ActionResult GetUserMessages()
        {
            try
            {
                var userSession = (UserLogin)Session[Commoncontent.user_sesion];
                if (userSession == null)
                    return Json(new { error = "not_logged_in" }, JsonRequestBehavior.AllowGet);

                int userId = userSession.UserId;

                var messages = db.ChatMessages
                    .Where(m => m.UserId == userId)
                    .OrderBy(m => m.SentAt)
                    .ToList()
                    .Select(m => new
                    {
                        m.MessageText,
                        m.IsFromAdmin,
                        Time = m.SentAt != null ? m.SentAt.ToString("HH:mm") : ""
                    })
                    .ToList();

                return Json(messages, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult Ask(string message)
        {
            var userSession = (UserLogin)Session[Commoncontent.user_sesion];
            if (userSession == null)
            {
                string loginMessage = $@"
<p style='color:red; font-weight:bold;'>
⚠️ Vui lòng <a href='/RegisterAndLogin/Login' style='color:blue;'>ĐĂNG NHẬP</a> 
để trò chuyện với Chatbot.
</p>";
                return Content(string.IsNullOrWhiteSpace(message) ? GetInitialGreeting() + loginMessage : loginMessage, "text/html");
            }

            string reply = "";

            // 🧱 1. Lưu tin nhắn khách vào DB
            if (!string.IsNullOrWhiteSpace(message))
            {
                db.ChatMessages.Add(new ChatMessage
                {
                    UserId = userSession.UserId,
                    MessageText = message,
                    IsFromAdmin = false,
                    SentAt = DateTime.Now
                });
                db.SaveChanges();
            }

            // 🧱 2. Nếu chưa có message -> gửi lời chào
            if (string.IsNullOrWhiteSpace(message))
                return Content(GetInitialGreeting(), "text/html");

            // 🧱 3. Chuẩn hóa câu hỏi (loại dấu, lowercase)
            string question = RemoveDiacritics(message.ToLower().Trim());
            question = Regex.Replace(question, @"\b(mua|xem|muon|tim|toi muon|cho xem|toi can|muốn|tìm|tôi muốn|tôi cần)\b", " ");
            question = Regex.Replace(question, @"\s+", " ").Trim();

            // 🧱 4. Trả lời tự động
            reply = GenerateReply(question);

            // 🧱 5. Chỉ lưu và trả lời NẾU có reply (reply là string HTML)
            if (!string.IsNullOrEmpty(reply))
            {
                db.ChatMessages.Add(new ChatMessage
                {
                    UserId = userSession.UserId,
                    MessageText = reply,
                    IsFromAdmin = true,
                    SentAt = DateTime.Now
                });
                db.SaveChanges();

                return Content(reply, "text/html");
            }

            // Không trả lời gì cả (trả chuỗi rỗng)
            return Content("", "text/html");
        }

        // -----------------------
        // Trả về chuỗi HTML (string) — đồng nhất
        // -----------------------
        private string GenerateReply(string question)
        {
            if (string.IsNullOrWhiteSpace(question)) return null;

            // 1️⃣ Giờ mở cửa (kiểm tra trên phiên bản không dấu)
            if (question.Contains("gio") || question.Contains("giờ") ||
    question.Contains("mo cua") || question.Contains("mở cửa") ||
    question.Contains("hoat dong") || question.Contains("hoạt động") ||
    question.Contains("lam viec") || question.Contains("làm việc") ||
    question.Contains("dong cua") || question.Contains("đóng cửa"))

                return "🕓 Cửa hàng mở từ <b>8:00 – 21:00</b> mỗi ngày, kể cả Thứ 7 và Chủ nhật.";
            
            // 2️⃣ Địa chỉ / Liên hệ
            if (question.Contains("dia chi") || question.Contains("địa chỉ") ||
    question.Contains("o dau") || question.Contains("ở đâu") ||
    question.Contains("hotline") ||
    question.Contains("so dien thoai") || question.Contains("số điện thoại") ||
    question.Contains("dien thoai") || question.Contains("điện thoại") ||
    question.Contains("lien he") || question.Contains("liên hệ") ||
    question.Contains("zalo"))
            // TRONG GenerateReply -> trả về string (HTML)
            {
                var html = $@"
    <div class='reply-block' style='font-family:Arial,sans-serif;'>
        <h3 style='color:#007bff;'>📍 ĐỊA CHỈ & LIÊN HỆ</h3>
        <p>
            🏠 <b>{STORE_NAME}</b><br/>
            📍 <b>Địa chỉ:</b> {STORE_ADDRESS}<br/>
            ☎️ <b>Điện thoại:</b> {STORE_PHONE} (Zalo / Gọi trực tiếp)<br/>
            📧 <b>Email:</b> {STORE_EMAIL}<br/>
            ⏰ <b>Giờ mở cửa:</b> 8:00 - 21:00 mỗi ngày
        </p>
    </div>";

                var top = db.Products.Take(4).ToList();
                if (top.Any())
                {
                    var sb = new StringBuilder(html);
                    sb.AppendLine("<div class='highlight-products'>");
                    sb.AppendLine("<h4 style='margin-top:10px;'>🪑 Một vài sản phẩm nổi bật:</h4>");
                    sb.AppendLine("<ul style='padding-left:18px;'>");
                    foreach (var s in top)
                    {
                        sb.AppendLine($"<li>{s.Name} – <b>{s.Price:N0}đ</b></li>");
                    }
                    sb.AppendLine("</ul>");
                    sb.AppendLine("</div>");
                    return sb.ToString(); // <-- TRẢ VỀ STRING (KHÔNG DÙNG Content)
                }

                return html; // <-- TRẢ VỀ STRING
            }



            // 3️⃣ Giảm giá
            if (question.Contains("giam") || question.Contains("giảm") ||
    question.Contains("sale") ||
    question.Contains("uu dai") || question.Contains("ưu đãi") ||
    question.Contains("khuyen mai") || question.Contains("khuyến mãi") ||
    question.Contains("hot"))

            {
                var sp = db.Products.Where(p => p.Discount > 0).OrderByDescending(p => p.Discount).Take(6).ToList();
                return ListProductsReply("🔥 SẢN PHẨM ĐANG GIẢM GIÁ", sp, true);
            }

            // 4️⃣ Hàng mới
            if (question.Contains("moi") || question.Contains("mới") ||
                question.Contains("vua") || question.Contains("vừa") ||
                question.Contains("hang moi") || question.Contains("hàng mới") ||
                question.Contains("cap nhat") || question.Contains("cập nhật"))

            {
                var spMoi = db.Products.OrderByDescending(p => p.StartDate).Take(6).ToList();
                return ListProductsReply("🆕 SẢN PHẨM MỚI VỀ", spMoi);
            }

            // 5️⃣ Giá rẻ
            if (question.Contains("re") || question.Contains("rẻ") ||
    question.Contains("binh dan") || question.Contains("bình dân") ||
    question.Contains("gia thap") || question.Contains("giá thấp"))

            {
                var spRe = db.Products.OrderBy(p => p.Price).Take(6).ToList();
                return ListProductsReply("💸 SẢN PHẨM GIÁ RẺ", spRe);
            }

            // 6️⃣ Giao hàng
            if (question.Contains("giao hang") || question.Contains("giao hàng") ||
    question.Contains("ship") ||
    question.Contains("van chuyen") || question.Contains("vận chuyển"))

                return "🚚 Giao hàng toàn quốc. Huế giao trong 24h, tỉnh khác 3–5 ngày. Miễn phí đơn hàng trên 5 triệu.";

            // 7️⃣ Đổi trả & bảo hành
            if (question.Contains("bao hanh") || question.Contains("bảo hành") ||
    question.Contains("doi tra") || question.Contains("đổi trả") ||
    question.Contains("chinh sach") || question.Contains("chính sách") ||
    question.Contains("hu hong") || question.Contains("hư hỏng"))

                return "🔁 Bảo hành 12–36 tháng, đổi trả trong 7 ngày nếu lỗi nhà sản xuất.";

            // 8️⃣ Thanh toán
            if (question.Contains("thanh toan") || question.Contains("thanh toán") ||
    question.Contains("chuyen khoan") || question.Contains("chuyển khoản") ||
    question.Contains("tra gop") || question.Contains("trả góp") ||
    question.Contains("tien") || question.Contains("tiền"))

                return "💳 Thanh toán bằng tiền mặt, chuyển khoản, hoặc trả góp 0% cho đơn trên 5 triệu.";

            // 9️⃣ Tư vấn thiết kế
            if (question.Contains("tu van") || question.Contains("tư vấn") ||
     question.Contains("thiet ke") || question.Contains("thiết kế") ||
     question.Contains("chon") || question.Contains("chọn") ||
     question.Contains("phu hop") || question.Contains("phù hợp"))

                return "🎨 Bạn muốn thiết kế phòng khách, phòng ngủ hay phòng ăn? Mình sẽ gợi ý sản phẩm phù hợp nhé!";

            // 🔟 Tìm theo danh mục / tên sản phẩm
            var allCategories = db.Categories.ToList();
            var matchedCategory = allCategories.FirstOrDefault(c => question.Contains(RemoveDiacritics(c.Name.ToLower())));
            if (matchedCategory != null)
            {
                var productsByCate = db.Products.Where(p => p.CateId == matchedCategory.CategoryId).Take(6).ToList();
                if (productsByCate.Any())
                    return ListProductsReply($"🪑 SẢN PHẨM: {matchedCategory.Name.ToUpper()}", productsByCate);
                return $"Hiện danh mục <b>{matchedCategory.Name}</b> chưa có sản phẩm nào. Bạn thử tìm từ khóa khác nhé!";
            }

            // Tìm theo tên sản phẩm
            var matches = db.Products.ToList()
                .Where(p => question.Contains(RemoveDiacritics(p.Name.ToLower())))
                .Take(6).ToList();
            if (matches.Any())
                return ListProductsReply("🔍 SẢN PHẨM GỢI Ý (Theo từ khóa)", matches);

            // Không hiểu
            return null;
        }

        // ⚙️ Hỗ trợ hiển thị sản phẩm - trả về string HTML
        private string ListProductsReply(string title, List<Product> products, bool showDiscount = false)
        {
            if (!products.Any()) return HtmlReply(title, "Không có sản phẩm nào phù hợp 😅.");

            var sb = new StringBuilder($"<div class='reply-block'><h3>{title}</h3><ul>");
            foreach (var s in products)
            {
                var price = $"{s.Price:N0}đ";
                if (showDiscount && s.Discount > 0)
                {
                    var newPrice = (decimal)(s.Price * (100 - s.Discount) / 100);
                    price = $"<b>{newPrice:N0}đ</b> (giảm {s.Discount}%)";
                }
                sb.AppendLine($"<li>{s.Name} – {price}</li>");
            }
            sb.AppendLine("</ul></div>");
            return sb.ToString();
        }

        private string HtmlReply(string title, string content)
        {
            string html = $@"
<div class='reply-block'>
    <h3 style='margin-bottom:5px;'>{title}</h3>
    <p style='font-size:15px; line-height:1.6em'>{content}</p>
</div>";
            return html;
        }

        private bool ContainsAny(string text, params string[] keywords)
            => keywords.Any(k => text.Contains(k));

        private string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            text = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in text)
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            return sb.ToString().Normalize(NormalizationForm.FormC).ToLower();
        }
    }
}
