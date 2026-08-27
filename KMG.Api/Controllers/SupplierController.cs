using System.Security.Claims;
using KMG.Api.Authorization;
using KMG.Core.DTOs.Supplier;
using KMG.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace KMG.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplierController : ControllerBase
    {
        private readonly ISupplierService _supplierService;

        public SupplierController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        private int CurrentEmployeeId => int.Parse(User.FindFirstValue("EmployeeId") ?? "0");

        [HttpGet]
        [AuthorizeAbility("عرض الموردين")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _supplierService.GetAllAsync());
        }

        [HttpGet("{id}")]
        [AuthorizeAbility("عرض الموردين")]
        public async Task<IActionResult> GetById(int id)
        {
            var supplier = await _supplierService.GetByIdAsync(id);
            return supplier == null ? NotFound() : Ok(supplier);
        }

        [HttpPost]
        [AuthorizeAbility("إدارة الموردين")]
        public async Task<IActionResult> Create([FromBody] CreateSupplierDTO model)
        {
            var id = await _supplierService.CreateAsync(model);
            return Ok(id);
        }

        [HttpPut]
        [AuthorizeAbility("إدارة الموردين")]
        public async Task<IActionResult> Update([FromBody] UpdateSupplierDTO model)
        {
            var result = await _supplierService.UpdateAsync(model);
            return result ? Ok() : NotFound();
        }

        [HttpPost("payments")]
        [AuthorizeAbility("إدارة الموردين")]
        public async Task<IActionResult> RecordPayment([FromBody] CreateSupplierPaymentDTO model)
        {
            var result = await _supplierService.RecordPaymentAsync(model, CurrentEmployeeId);
            return Ok(result);
        }
    }
}
