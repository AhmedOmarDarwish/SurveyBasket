using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SurveyBasket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController(IRoleService roleService) : ControllerBase
    {
        private readonly IRoleService _roleService = roleService;

        [HttpGet("")]
        [HasPermission(Permissions.GetRoles)]
        public async Task<IActionResult> GetAll([FromQuery] bool includeDisabled, CancellationToken cancellationToken)
        {
            var roles = await _roleService.GetAllAsync(includeDisabled, cancellationToken);
            return Ok(roles);
        }

        [HttpGet("{id}")]
        [HasPermission(Permissions.GetRoles)]
        public async Task<IActionResult> Get([FromRoute] string id, CancellationToken cancellationToken)
        {
            var result = await _roleService.GetAsync(id, cancellationToken);
            return result.IsSuccess ?
                Ok(result.Value)
                : result.ToProblem();
        }

        [HttpPost("")]
        [HasPermission(Permissions.AddRoles)]
        public async Task<IActionResult> Add([FromBody] RoleRequest request, CancellationToken cancellationToken)
        {
            var result = await _roleService.AddAsync(request, cancellationToken);
            return result.IsSuccess ?
                CreatedAtAction(nameof(Get), new {result.Value.Id}, result.Value)
                : result.ToProblem();
        }

        [HttpPut("{id}")]
        [HasPermission(Permissions.UpdatesRoles)]
        public async Task<IActionResult> Update([FromRoute] string id,[FromBody] RoleRequest request, CancellationToken cancellationToken)
        {
            var result = await _roleService.UpdateAsync(id, request, cancellationToken);
            return result.IsSuccess
                ? NoContent()
                : result.ToProblem();
        }

        [HttpPut("{id}/toggle-status")]
        [HasPermission(Permissions.UpdatesRoles)]
        public async Task<IActionResult> ToggleStatus([FromRoute] string id, CancellationToken cancellationToken)
        {
            var result = await _roleService.ToggleStatusAsync(id, cancellationToken);
            return result.IsSuccess
                ? NoContent()
                : result.ToProblem();
        }

    }
}
