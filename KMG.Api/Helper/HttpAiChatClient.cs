using System.Net.Http.Json;
using System.Text.Json;
using KMG.Core.DTOs.AiChat;
using KMG.Core.Interfaces.Services;

namespace KMG.Api.Helper
{
    /// <summary>
    /// بيبعت رسالة المحادثة لخدمة الـ AI الخارجية ويرجّع ردها.
    ///
    /// الإعدادات (appsettings → AiIntegration):
    ///   ChatUrl            رابط الـ Endpoint عند خدمة الـ AI (POST)
    ///   ChatApiKey         اختياري - لو مش موجود بيستخدم ApiKey (نفس المفتاح المشترك مع خدمة الـ AI)
    ///   ChatTimeoutSeconds اختياري - الافتراضي 90 ثانية
    ///
    /// الطلب: POST JSON
    ///   { "message": "...", "history": [{ "role": "user|assistant", "content": "..." }], "systemPrompt": "...", "userName": "..." }
    ///   + Header: X-Api-Key
    ///
    /// الرد المقبول (أي واحد من دول):
    ///   { "reply": "..." } | { "content": "..." } | { "message": "..." } | { "answer": "..." } | { "response": "..." }
    ///   | { "choices": [{ "message": { "content": "..." } }] } (شكل OpenAI) | نص عادي
    /// </summary>
    public class HttpAiChatClient : IAiChatClient
    {
        private static readonly string[] ReplyKeys = { "reply", "content", "message", "answer", "response", "text", "output" };

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;

        public HttpAiChatClient(HttpClient httpClient, IConfiguration configuration, IHostEnvironment environment)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _environment = environment;
        }

        public async Task<string> SendAsync(AiChatRequestDTO request, CancellationToken cancellationToken = default)
        {
            var url = _configuration["AiIntegration:ChatUrl"];
            if (string.IsNullOrWhiteSpace(url))
            {
                // في التطوير المحلي من غير خدمة AI متوصلة، بنرجع رد تجريبي عشان الشاشة تتجرب كاملة
                if (_environment.IsDevelopment())
                    return $"(رد تجريبي - خدمة الـ AI لسه مش متوصلة)\nوصلتني رسالتك: \"{request.Message}\"\nعدد الرسايل السابقة اللي اتبعتت كسياق: {request.History.Count}";

                throw new Exception("خدمة محادثة الـ AI لسه مش متوصلة - لازم يتظبط رابطها (AiIntegration:ChatUrl) في إعدادات السيرفر");
            }

            var apiKey = _configuration["AiIntegration:ChatApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey)) apiKey = _configuration["AiIntegration:ApiKey"];

            var timeoutSeconds = int.TryParse(_configuration["AiIntegration:ChatTimeoutSeconds"], out var t) && t > 0 ? t : 90;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(request, options: new JsonSerializerOptions(JsonSerializerDefaults.Web))
            };
            if (!string.IsNullOrWhiteSpace(apiKey)) httpRequest.Headers.Add("X-Api-Key", apiKey);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(httpRequest, timeout.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new Exception("خدمة الـ AI اتأخرت في الرد - جرّب تاني بعد شوية");
            }
            catch (HttpRequestException)
            {
                throw new Exception("تعذر الاتصال بخدمة الـ AI - اتأكد إنها شغالة");
            }

            using (response)
            {
                var body = await response.Content.ReadAsStringAsync(timeout.Token);
                if (!response.IsSuccessStatusCode)
                    throw new Exception($"خدمة الـ AI رجعت خطأ ({(int)response.StatusCode})");

                return ExtractReply(body);
            }
        }

        private static string ExtractReply(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return string.Empty;

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.String) return root.GetString() ?? string.Empty;
                if (root.ValueKind != JsonValueKind.Object) return body;

                foreach (var key in ReplyKeys)
                {
                    if (TryGetPropertyIgnoreCase(root, key, out var value) && value.ValueKind == JsonValueKind.String)
                        return value.GetString() ?? string.Empty;
                }

                // شكل OpenAI: choices[0].message.content
                if (TryGetPropertyIgnoreCase(root, "choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0
                    && TryGetPropertyIgnoreCase(choices[0], "message", out var msg)
                    && TryGetPropertyIgnoreCase(msg, "content", out var content) && content.ValueKind == JsonValueKind.String)
                    return content.GetString() ?? string.Empty;

                // { "data": { "reply": "..." } }
                if (TryGetPropertyIgnoreCase(root, "data", out var data) && data.ValueKind == JsonValueKind.Object)
                    return ExtractReply(data.GetRawText());

                throw new Exception("رد خدمة الـ AI مش بالشكل المتوقع (مفيش reply/content)");
            }
            catch (JsonException)
            {
                return body; // الرد نص عادي مش JSON
            }
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        value = property.Value;
                        return true;
                    }
                }
            }
            value = default;
            return false;
        }
    }
}
