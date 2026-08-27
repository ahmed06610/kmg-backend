using KMG.Api.Authorization;
using KMG.Core.DTOs.Auth;
using KMG.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KMG.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDTO model)
        {
            var result = await _authService.LoginAsync(model);
            if (!result.IsAuthenticated) return BadRequest(result.Message);
            return Ok(result);
        }

        [HttpPost("register-employee")]
        [AuthorizeAbility("إدارة الموظفين")]
        public async Task<IActionResult> RegisterEmployee([FromBody] RegisterEmployeeDTO model)
        {
            var result = await _authService.RegisterEmployeeAsync(model);
            if (!result.IsAuthenticated) return BadRequest(result.Message);
            return Ok(result);
        }

        [HttpPut("edit-employee")]
        [AuthorizeAbility("إدارة الموظفين")]
        public async Task<IActionResult> EditEmployee([FromBody] EditEmployeeDTO model)
        {
            var result = await _authService.EditEmployeeAsync(model);
            if (!result.IsAuthenticated) return BadRequest(result.Message);
            return Ok(result);
        }

        [HttpGet("roles")]
        [Authorize]
        public async Task<IActionResult> GetAllRoles()
        {
            return Ok(await _authService.GetAllRolesAsync());
        }

        [HttpGet("roles/{roleId}/abilities")]
        [Authorize]
        public async Task<IActionResult> GetAbilitiesForRole(string roleId)
        {
            return Ok(await _authService.GetAbilitiesForRoleAsync(roleId));
        }
    }
}
