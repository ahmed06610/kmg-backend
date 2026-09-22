using KMG.Api.Authorization;
using KMG.Core.DTOs.Client;
using KMG.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace KMG.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientController : ControllerBase
    {
        private readonly IClientService _clientService;

        public ClientController(IClientService clientService)
        {
            _clientService = clientService;
        }

        [HttpGet]
        [AuthorizeAbility("عرض العملاء")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _clientService.GetAllAsync());
        }

        [HttpGet("{id}")]
        [AuthorizeAbility("عرض العملاء")]
        public async Task<IActionResult> GetById(int id)
        {
            var client = await _clientService.GetByIdAsync(id);
            return client == null ? NotFound() : Ok(client);
        }

        [HttpPost]
        [AuthorizeAbility("إدارة العملاء")]
        public async Task<IActionResult> Create([FromBody] CreateClientDTO model)
        {
            var id = await _clientService.CreateAsync(model);
            return Ok(id);
        }

        [HttpPut]
        [AuthorizeAbility("إدارة العملاء")]
        public async Task<IActionResult> Update([FromBody] UpdateClientDTO model)
        {
            var result = await _clientService.UpdateAsync(model);
            return result ? Ok() : NotFound();
        }

        [HttpDelete("{id}")]
        [AuthorizeAbility("إدارة العملاء")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _clientService.DeleteAsync(id);
                return result ? Ok() : NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
