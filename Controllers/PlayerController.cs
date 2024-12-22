using FooBooRealTime_back_dotnet.Interface.Service;
using FooBooRealTime_back_dotnet.Model.Domain;
using FooBooRealTime_back_dotnet.Model.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FooBooRealTime_back_dotnet.Controllers
{
    /// <summary>
    /// This is a debug controller, the actual authentication will need to be in auth route
    /// </summary>
    [ApiController]
    [Route("Api/Players/")]
    //[Authorize]
    public class PlayerController : ControllerBase
    {
        private readonly ILogger<IPlayerService> _logger;
        private readonly IPlayerService _playerService;

        public PlayerController(ILogger<IPlayerService> logger, IPlayerService playerService)
        {
            _logger = logger;
            _playerService = playerService;
        }

        // since this is a debug class, there will be ill-documented
        [HttpPost("Player")]
        [ProducesResponseType(typeof(Player), StatusCodes.Status201Created)] // Specifies the response type for 200 OK
        public async Task<IActionResult> Register([FromBody] PlayerDTO playerDTO)
        {
            var target = await _playerService.CreateAsync(playerDTO);
            return (target == null)
                    ? BadRequest() :
                    Created(nameof(target), target);

        }

        [HttpDelete("{playerId}")]
        public async Task<IActionResult> RemovePlayer(Guid playerId)
        {
            await _playerService.DeleteAsync(playerId);
            return NoContent();
        }

        [HttpGet("")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _playerService.GetAllAsync());
        }
    }
}
