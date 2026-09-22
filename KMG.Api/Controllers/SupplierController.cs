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

        [HttpDelete("{id}")]
        [AuthorizeAbility("إدارة الموردين")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _supplierService.DeleteAsync(id);
                return result ? Ok() : NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("payments")]
        [AuthorizeAbility("إدارة الموردين")]
        public async Task<IActionResult> RecordPayment([FromBody] CreateSupplierPaymentDTO model)
        {
            try
            {
                return Ok(await _supplierService.RecordPaymentAsync(model, CurrentEmployeeId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("payments")]
        [AuthorizeAbility("إدارة الموردين")]
        public async Task<IActionResult> UpdatePayment([FromBody] UpdateSupplierPaymentDTO model)
        {
            try
            {
                return Ok(await _supplierService.UpdatePaymentAsync(model, CurrentEmployeeId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("payments/{id}")]
        [AuthorizeAbility("إدارة الموردين")]
        public async Task<IActionResult> DeletePayment(int id)
        {
            try
            {
                var result = await _supplierService.DeletePaymentAsync(id);
                return result ? Ok() : NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("payments/pending-checks")]
        [AuthorizeAbility("عرض الموردين")]
        public async Task<IActionResult> GetPendingChecks()
        {
            return Ok(await _supplierService.GetPendingChecksAsync());
        }

        [HttpPost("payments/checks/resolve")]
        [AuthorizeAbility("إدارة الموردين")]
        public async Task<IActionResult> ResolveCheck([FromBody] ResolveCheckDTO model)
        {
            try
            {
                return Ok(await _supplierService.ResolveCheckAsync(model, CurrentEmployeeId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
