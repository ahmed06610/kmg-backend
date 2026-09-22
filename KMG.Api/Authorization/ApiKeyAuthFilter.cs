using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KMG.Api.Authorization
{
    // حماية بسيطة لأي Endpoint هيتنادى من جهة خارجية (زي خدمة الـ AI) مش من موظف مسجل دخول بالـ JWT العادي
    public class ApiKeyAuthFilter : IAsyncActionFilter
    {
        private const string HeaderName = "X-Api-Key";
        private readonly IConfiguration _configuration;

        public ApiKeyAuthFilter(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var expectedKey = _configuration["AiIntegration:ApiKey"];

            if (string.IsNullOrEmpty(expectedKey)
                || !context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var providedKey)
                || providedKey != expectedKey)
            {
                context.Result = new UnauthorizedObjectResult(new { message = "مفتاح API غير صالح" });
                return;
            }

            await next();
        }
    }
}
