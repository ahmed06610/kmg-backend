using System.Security.Claims;
using KMG.Api.Authorization;
using KMG.Core.DTOs.Mission;
using KMG.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace KMG.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MissionController : ControllerBase
    {
        private readonly IMissionService _missionService;

        public MissionController(IMissionService missionService)
        {
            _missionService = missionService;
        }

        private int CurrentEmployeeId => int.Parse(User.FindFirstValue("EmployeeId") ?? "0");

        [HttpGet("by-project/{projectId}")]
        [AuthorizeAbility("إدارة المأموريات")]
        public async Task<IActionResult> GetByProject(int projectId)
        {
            return Ok(await _missionService.GetByProjectAsync(projectId));
        }

        [HttpGet("{id}")]
        [AuthorizeAbility("إدارة المأموريات")]
        public async Task<IActionResult> GetById(int id)
        {
            var mission = await _missionService.GetByIdAsync(id);
            return mission == null ? NotFound() : Ok(mission);
        }

        [HttpPost]
        [AuthorizeAbility("إدارة المأموريات")]
        public async Task<IActionResult> Create([FromBody] CreateMissionDTO model)
        {
            try
            {
                return Ok(await _missionService.CreateAsync(model, CurrentEmployeeId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("settle")]
        [AuthorizeAbility("إدارة المأموريات")]
        public async Task<IActionResult> Settle([FromBody] SettleMissionDTO model)
        {
            try
            {
                return Ok(await _missionService.SettleAsync(model, CurrentEmployeeId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        [AuthorizeAbility("إدارة المأموريات")]
        public async Task<IActionResult> Update([FromBody] UpdateMissionDTO model)
        {
            try
            {
                return Ok(await _missionService.UpdateAsync(model, CurrentEmployeeId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [AuthorizeAbility("إدارة المأموريات")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _missionService.DeleteAsync(id, CurrentEmployeeId);
                return result ? Ok() : NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
