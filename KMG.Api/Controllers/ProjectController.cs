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

        [HttpPost("attachments")]
        [AuthorizeAbility("إدارة المشاريع")]
        public async Task<IActionResult> AddAttachment([FromBody] CreateProjectAttachmentDTO model)
        {
            return Ok(await _projectService.AddAttachmentAsync(model, CurrentEmployeeId));
        }
    }
}
