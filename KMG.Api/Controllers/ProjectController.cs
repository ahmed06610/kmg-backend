using System.Security.Claims;
using KMG.Api.Authorization;
using KMG.Core.DTOs.Project;
using KMG.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace KMG.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        private int CurrentEmployeeId => int.Parse(User.FindFirstValue("EmployeeId") ?? "0");

        [HttpGet]
        [AuthorizeAbility("عرض المشاريع")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _projectService.GetAllAsync());
        }

        [HttpGet("{id}")]
        [AuthorizeAbility("عرض المشاريع")]
        public async Task<IActionResult> GetById(int id)
        {
            var project = await _projectService.GetByIdAsync(id);
            return project == null ? NotFound() : Ok(project);
        }

        [HttpPost]
        [AuthorizeAbility("إدارة المشاريع")]
        public async Task<IActionResult> Create([FromBody] CreateProjectDTO model)
        {
            var id = await _projectService.CreateAsync(model, CurrentEmployeeId);
            return Ok(id);
        }

        [HttpPut]
        [AuthorizeAbility("إدارة المشاريع")]
        public async Task<IActionResult> Update([FromBody] UpdateProjectDTO model)
        {
            var result = await _projectService.UpdateAsync(model, CurrentEmployeeId);
            return result ? Ok() : NotFound();
        }

        [HttpDelete("{id}")]
        [AuthorizeAbility("إدارة المشاريع")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _projectService.DeleteAsync(id);
                return result ? Ok() : NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("status")]
        [AuthorizeAbility("إدارة المشاريع")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateProjectStatusDTO model)
        {
            var result = await _projectService.UpdateStatusAsync(model, CurrentEmployeeId);
            return result ? Ok() : NotFound();
        }

        [HttpPost("payments")]
        [AuthorizeAbility("إدارة المشاريع")]
        public async Task<IActionResult> RecordPayment([FromBody] CreateProjectPaymentDTO model)
        {
            try
            {
                return Ok(await _projectService.RecordPaymentAsync(model, CurrentEmployeeId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("payments")]
        [AuthorizeAbility("إدارة المشاريع")]
        public async Task<IActionResult> UpdatePayment([FromBody] UpdateProjectPaymentDTO model)
        {
            try
            {
                return Ok(await _projectService.UpdatePaymentAsync(model, CurrentEmployeeId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("payments/{id}")]
        [AuthorizeAbility("إدارة المشاريع")]
        public async Task<IActionResult> DeletePayment(int id)
        {
            try
            {
                var result = await _projectService.DeletePaymentAsync(id, CurrentEmployeeId);
                return result ? Ok() : NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("expenses")]
        [AuthorizeAbility("إدارة المصاريف")]
        public async Task<IActionResult> RecordExpense([FromBody] CreateProjectExpenseDTO model)
        {
            try
            {
                return Ok(await _projectService.RecordExpenseAsync(model, CurrentEmployeeId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("expenses")]
        [AuthorizeAbility("إدارة المصاريف")]
        public async Task<IActionResult> UpdateExpense([FromBody] UpdateProjectExpenseDTO model)
        {
            try
            {
                return Ok(await _projectService.UpdateExpenseAsync(model, CurrentEmployeeId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("expenses/{id}")]
        [AuthorizeAbility("إدارة المصاريف")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            try
            {
                var result = await _projectService.DeleteExpenseAsync(id, CurrentEmployeeId);
                return result ? Ok() : NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("attachments")]
        [AuthorizeAbility("إدارة المشاريع")]
        public async Task<IActionResult> AddAttachment([FromBody] CreateProjectAttachmentDTO model)
        {
            return Ok(await _projectService.AddAttachmentAsync(model, CurrentEmployeeId));
        }

        [HttpPut("attachments")]
        [AuthorizeAbility("إدارة المشاريع")]
        public async Task<IActionResult> UpdateAttachment([FromBody] UpdateProjectAttachmentDTO model)
        {
            var result = await _projectService.UpdateAttachmentAsync(model);
            return result ? Ok() : NotFound();
        }

        [HttpDelete("attachments/{id}")]
        [AuthorizeAbility("إدارة المشاريع")]
        public async Task<IActionResult> DeleteAttachment(int id)
        {
            var fileUrl = await _projectService.DeleteAttachmentAsync(id);
            if (fileUrl == null) return NotFound();

            // نمسح الملف الفعلي بس لو مخزن محليًا عندنا (نفس مجلد uploads/projects) - نتجاهل روابط خارجية
            var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "projects");
            var fileName = Path.GetFileName(new Uri(fileUrl, UriKind.RelativeOrAbsolute).IsAbsoluteUri
                ? new Uri(fileUrl).LocalPath
                : fileUrl);
            var localPath = Path.Combine(uploadsRoot, fileName);
            if (fileUrl.Contains("/uploads/projects/") && System.IO.File.Exists(localPath))
            {
                try { System.IO.File.Delete(localPath); } catch { /* حذف الملف الفعلي مش حرج - السجل اتمسح من الداتابيز بالفعل */ }
            }

            return Ok();
        }

        // حد أقصى 20 ميجا للملف الواحد - استضافة مشتركة ومساحة تخزين محدودة
        private const long MaxUploadBytes = 20 * 1024 * 1024;

        [HttpPost("attachments/upload")]
        [AuthorizeAbility("إدارة المشاريع")]
        [RequestSizeLimit(MaxUploadBytes)]
        public async Task<IActionResult> UploadAttachment(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("لم يتم اختيار ملف");
            if (file.Length > MaxUploadBytes)
                return BadRequest("حجم الملف أكبر من الحد المسموح (20 ميجا)");

            var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "projects");
            Directory.CreateDirectory(uploadsRoot);

            var safeExtension = Path.GetExtension(file.FileName);
            var storedFileName = $"{Guid.NewGuid()}{safeExtension}";
            var fullPath = Path.Combine(uploadsRoot, storedFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileUrl = $"{Request.Scheme}://{Request.Host}/uploads/projects/{storedFileName}";
            return Ok(new { fileUrl, fileName = file.FileName });
        }

        [HttpPost("write-offs")]
        [AuthorizeAbility("إدارة المشاريع")]
        public async Task<IActionResult> CreateWriteOff([FromBody] CreateProjectWriteOffDTO model)
        {
            try
            {
                return Ok(await _projectService.CreateWriteOffAsync(model, CurrentEmployeeId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("write-offs")]
        [AuthorizeAbility("إدارة المشاريع")]
        public async Task<IActionResult> UpdateWriteOff([FromBody] UpdateProjectWriteOffDTO model)
        {
            try
            {
                return Ok(await _projectService.UpdateWriteOffAsync(model, CurrentEmployeeId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("write-offs/{id}")]
        [AuthorizeAbility("إدارة المشاريع")]
        public async Task<IActionResult> DeleteWriteOff(int id)
        {
            try
            {
                var result = await _projectService.DeleteWriteOffAsync(id, CurrentEmployeeId);
                return result ? Ok() : NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
