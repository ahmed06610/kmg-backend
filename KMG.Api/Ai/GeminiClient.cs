using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace KMG.Api.Ai
{
    public class AiServiceException : Exception
    {
        public AiServiceException(string userMessage, Exception? inner = null) : base(userMessage, inner) { }
    }

    public class GeminiClient
    {
        private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";
        private const string DefaultChatModel = "gemini-3.5-flash-lite";
        private const int MaxAttempts = 3;

        private static readonly TimeSpan MaxRetryWait = TimeSpan.FromSeconds(20);
        private static readonly Regex RetryDelayPattern = new("\"retryDelay\"\\s*:\\s*\"(\\d+(?:\\.\\d+)?)s\"", RegexOptions.Compiled);

        public const string UnavailableMessage = "حصلت مشكلة مؤقتة في خدمة الـ AI. جرّب تاني بعد شوية.";

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiClient> _logger;

        public GeminiClient(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiClient> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public string ChatModel => string.IsNullOrWhiteSpace(_configuration["Ai:Gemini:ChatModel"])
            ? DefaultChatModel
            : _configuration["Ai:Gemini:ChatModel"]!;

        public string? ThinkingLevel => string.IsNullOrWhiteSpace(_configuration["Ai:Gemini:ThinkingLevel"])
            ? null
            : _configuration["Ai:Gemini:ThinkingLevel"];

        public async Task<JsonObject> GenerateContentAsync(JsonObject request, CancellationToken cancellationToken)
        {
            var apiKey = _configuration["Ai:Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogError("Gemini API key is not configured (Ai:Gemini:ApiKey)");
                throw new AiServiceException("خدمة الـ AI لسه مش متظبطة على السيرفر.");
            }

            var timeoutSeconds = int.TryParse(_configuration["Ai:Gemini:TimeoutSeconds"], out var t) && t > 0 ? t : 60;
            var url = $"{BaseUrl}{Uri.EscapeDataString(ChatModel)}:generateContent";
            var body = request.ToJsonString();

            for (var attempt = 1; ; attempt++)
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
                httpRequest.Headers.Add("x-goog-api-key", apiKey);

                HttpResponseMessage response;
                try
                {
                    response = await _httpClient.SendAsync(httpRequest, timeout.Token);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Gemini request timed out after {Seconds}s", timeoutSeconds);
                    throw new AiServiceException("خدمة الـ AI اتأخرت في الرد. جرّب تاني بعد شوية.");
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex, "Gemini request failed to connect (attempt {Attempt})", attempt);
                    if (attempt < MaxAttempts) { await DelayBeforeRetryAsync(attempt, null, cancellationToken); continue; }
                    throw new AiServiceException(UnavailableMessage, ex);
                }

                using (response)
                {
                    var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

                    if (response.IsSuccessStatusCode)
                        return ExtractCandidate(responseText);

                    var status = (int)response.StatusCode;
                    var transient = response.StatusCode == HttpStatusCode.TooManyRequests || status >= 500;
                    _logger.LogWarning("Gemini returned HTTP {Status} (attempt {Attempt}): {Body}",
                        status, attempt, Truncate(responseText, 1000));

                    var retryAfter = response.Headers.RetryAfter?.Delta ?? ParseRetryDelay(responseText);
                    if (transient && attempt < MaxAttempts && (retryAfter == null || retryAfter <= MaxRetryWait))
                    {
                        await DelayBeforeRetryAsync(attempt, retryAfter, cancellationToken);
                        continue;
                    }

                    throw new AiServiceException(response.StatusCode == HttpStatusCode.TooManyRequests
                        ? "خدمة الـ AI عليها ضغط دلوقتي. جرّب تاني بعد دقيقة."
                        : UnavailableMessage);
                }
            }
        }

        internal JsonObject ExtractCandidate(string responseText)
        {
            JsonNode? root;
            try
            {
                root = JsonNode.Parse(responseText);
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(ex, "Gemini returned malformed JSON: {Body}", Truncate(responseText, 500));
                throw new AiServiceException(UnavailableMessage, ex);
            }

            var first = root?["candidates"] is JsonArray { Count: > 0 } candidates ? candidates[0] as JsonObject : null;
            if (first?["content"] is JsonObject)
                return first;

            var blockReason = root?["promptFeedback"]?["blockReason"]?.ToString();
            _logger.LogWarning("Gemini returned no usable candidate (blockReason: {BlockReason}, finishReason: {FinishReason})",
                blockReason, first?["finishReason"]?.ToString());
            throw new AiServiceException(blockReason != null
                ? "مقدرتش أرد على الرسالة دي. جرّب تصيغها بشكل تاني."
                : UnavailableMessage);
        }

        private static Task DelayBeforeRetryAsync(int attempt, TimeSpan? retryAfter, CancellationToken cancellationToken)
        {
            var delay = retryAfter ?? TimeSpan.FromSeconds(attempt == 1 ? 1 : 3);
            return Task.Delay(delay, cancellationToken);
        }

        internal static TimeSpan? ParseRetryDelay(string errorBody)
        {
            var match = RetryDelayPattern.Match(errorBody);
            return match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
                ? TimeSpan.FromSeconds(Math.Ceiling(seconds))
                : null;
        }

        private static string Truncate(string text, int max) => text.Length <= max ? text : text[..max] + "…";
    }
}
