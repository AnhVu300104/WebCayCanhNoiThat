using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebsiteNoiThat.Controllers
{
    public class ChatbotAIController : Controller
    {
        // Thay bằng API Key từ Google Cloud Project Free Tier
        private const string GEMINI_API_KEY = "AIzaSyAbIWJFFH7Md-6MmJHeT36rBgGcKkfb0JA";
        private const string GEMINI_API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-lite:generateContent";

        [HttpPost]
        public async Task<JsonResult> SendMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return Json(new { success = false, message = "Vui lòng nhập câu hỏi!" });
            }

            // Tạo prompt hướng dẫn chatbot
            string systemPrompt = @"Bạn là trợ lý tư vấn chăm sóc cây cảnh tại cửa hàng CAYCANHANHVU.VN. 
Nhiệm vụ của bạn:
- Tư vấn cách chăm sóc, tưới nước, bón phân cho các loại cây cảnh
- Giúp khách hàng chọn cây phù hợp với không gian và điều kiện ánh sáng
- Hướng dẫn xử lý sâu bệnh, cây bị vàng lá, úa lá
- Tư vấn về chậu, đất trồng, dụng cụ chăm sóc cây
- Trả lời ngắn gọn, thân thiện, dễ hiểu (2-4 câu)
- Luôn khuyến khích khách hàng liên hệ hotline 0964 155 923 nếu cần tư vấn trực tiếp

Hãy trả lời câu hỏi sau của khách hàng:";

            string fullPrompt = systemPrompt + "\n\nKhách hàng hỏi: " + message;

            try
            {
                // Handler cho SSL localhost
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                 var httpClient = new HttpClient(handler);
                httpClient.Timeout = TimeSpan.FromSeconds(60);
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                httpClient.DefaultRequestHeaders.Add("User-Agent", "CayCanhChatbot/1.0");

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = fullPrompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 500,
                        topP = 0.8,
                        topK = 40
                    }
                };

                string jsonRequest = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                string apiUrlWithKey = $"{GEMINI_API_URL}?key={GEMINI_API_KEY}";
                HttpResponseMessage response = await httpClient.PostAsync(apiUrlWithKey, content);

                string responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine("API Status: " + response.StatusCode);
                Console.WriteLine("API Response: " + responseContent);

                if (response.IsSuccessStatusCode)
                {
                    JObject jsonResponse = JObject.Parse(responseContent);
                    string botReply = jsonResponse["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();

                    if (!string.IsNullOrEmpty(botReply))
                    {
                        return Json(new { success = true, message = botReply });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Xin lỗi, tôi không thể trả lời lúc này. Vui lòng thử lại!" });
                    }
                }
                else
                {
                    return Json(new { success = false, message = "Lỗi kết nối API. Vui lòng thử lại sau!" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                return Json(new { success = false, message = "Đã xảy ra lỗi: " + ex.Message });
            }
        }
    }
}
