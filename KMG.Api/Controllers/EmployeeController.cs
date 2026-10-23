using KMG.Api.Authorization;
using KMG.Core.DTOs.Employee;
using KMG.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace KMG.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;

        public EmployeeController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        [HttpGet]
        [AuthorizeAbility("عرض الموظفين")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _employeeService.GetAllAsync());
        }

        [HttpGet("{id}")]
        [AuthorizeAbility("عرض الموظفين")]
        public async Task<IActionResult> GetById(int id)
        {
            var employee = await _employeeService.GetByIdAsync(id);
            return employee == null ? NotFound() : Ok(employee);
        }

        [HttpPost("workers")]
        [AuthorizeAbility("إدارة الموظفين")]
        public async Task<IActionResult> CreateWorker([FromBody] CreateWorkerDTO model)
        {
            var id = await _employeeService.CreateWorkerAsync(model);
            return Ok(id);
        }

        [HttpPut("workers")]
        [AuthorizeAbility("إدارة الموظفين")]
        public async Task<IActionResult> UpdateWorker([FromBody] UpdateWorkerDTO model)
        {
            try
            {
                var result = await _employeeService.UpdateWorkerAsync(model);
                return result ? Ok() : NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
