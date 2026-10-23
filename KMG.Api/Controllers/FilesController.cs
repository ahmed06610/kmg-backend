using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KMG.Api.Controllers
{
    // رفع ملفات عام لكل الموديولات (فواتير/إيصالات مرفقة على دفعات المشاريع، المخزون،
    // الموردين، المصاريف النثرية...) - نفس منطق ProjectController.UploadAttachment
    // بالظبط بس معمّم بمجلد وجهة قابل للتحديد بدل ما يفضل مربوط بالمشاريع بس.
    // مقصود إنه بس [Authorize] عادي من غير سماحية محددة: الأكشن الأصلي (تسجيل الدفعة/الشراء..)
    // هو اللي بيتفحص بسماحيته الخاصة عند الحفظ، فمش منطقي نطلب سماحية "إدارة الفواتير" هنا كمان
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FilesController : ControllerBase
    {
        private const long MaxUploadBytes = 20 * 1024 * 1024;
        private static readonly HashSet<string> AllowedFolders = new()
        {
            "invoices", "projects", "stock", "suppliers", "cashbox"
        };

        [HttpPost("upload")]
        [RequestSizeLimit(MaxUploadBytes)]
        public async Task<IActionResult> Upload(IFormFile file, [FromQuery] string folder = "invoices")
        {
            if (file == null || file.Length == 0)
                return BadRequest("لم يتم اختيار ملف");
            if (file.Length > MaxUploadBytes)
                return BadRequest("حجم الملف أكبر من الحد المسموح (20 ميجا)");
            if (!AllowedFolders.Contains(folder))
                folder = "invoices";

            var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", folder);
            Directory.CreateDirectory(uploadsRoot);

            var safeExtension = Path.GetExtension(file.FileName);
            var storedFileName = $"{Guid.NewGuid()}{safeExtension}";
            var fullPath = Path.Combine(uploadsRoot, storedFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileUrl = $"{Request.Scheme}://{Request.Host}/uploads/{folder}/{storedFileName}";
            return Ok(new { fileUrl, fileName = file.FileName });
        }
    }
}
