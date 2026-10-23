using System.Security.Claims;
using KMG.Api.Authorization;
using KMG.Core.DTOs.Invoice;
using KMG.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace KMG.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AuthorizeAbility("إدارة الفواتير")]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoiceController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        private int CurrentEmployeeId => int.Parse(User.FindFirstValue("EmployeeId") ?? "0");

        [HttpGet]
        public async Task<IActionResult> GetInvoices([FromQuery] InvoiceFilterDTO filter)
        {
            return Ok(await _invoiceService.GetInvoicesAsync(filter));
        }

        [HttpGet("generated")]
        public async Task<IActionResult> GetGeneratedInvoices()
        {
            return Ok(await _invoiceService.GetGeneratedInvoicesAsync());
        }

        [HttpGet("generated/{id}")]
        public async Task<IActionResult> GetGeneratedInvoice(int id)
        {
            var invoice = await _invoiceService.GetGeneratedInvoiceByIdAsync(id);
            return invoice == null ? NotFound() : Ok(invoice);
        }

        [HttpPost("generated")]
        public async Task<IActionResult> CreateGeneratedInvoice([FromBody] CreateGeneratedInvoiceDTO model)
        {
            try
            {
                return Ok(await _invoiceService.CreateGeneratedInvoiceAsync(model, CurrentEmployeeId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("generated/{id}")]
        public async Task<IActionResult> DeleteGeneratedInvoice(int id)
        {
            var result = await _invoiceService.DeleteGeneratedInvoiceAsync(id);
            return result ? Ok() : NotFound();
        }
    }
}
