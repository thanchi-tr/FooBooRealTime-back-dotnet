using FooBooRealTime_back_dotnet.Interface.Service;
using FooBooRealTime_back_dotnet.Model.Domain;
using FooBooRealTime_back_dotnet.Model.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FooBooRealTime_back_dotnet.Controllers
{
    [ApiController]
    [Route("")]
    [Authorize]
    public class GameController : ControllerBase
    {
        private readonly ILogger<IGameService> _logger;
        private readonly IGameService _gameService;

        public GameController(ILogger<IGameService> logger, IGameService gameService)
        {
            _logger = logger;
            _gameService = gameService;
        }

        [HttpPost("Players/{playerId}/Games/Game")]
        [SwaggerOperation(
            Summary = "Create an Game by with detail @NOT-TEST@ ",
            Description = "Create the store details if the store exists, otherwise returns a 404 status. Sample Rules: '{\"RuleList\":[{\"Key\":100,\"Value\":\"a\"}]}'"
        )]
        [ProducesResponseType(typeof(Game), StatusCodes.Status200OK)] // Specifies the response type for 200 OK
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Specifies a 404 Not Found response

        public async Task<IActionResult> Create([FromBody] GameDTO gameDetail, Guid playerId)
        {
            gameDetail.AuthorId = playerId;
            var target = await _gameService.CreateAsync(gameDetail);

            return (target == null)
                    ? BadRequest() :
                    Created(nameof(gameDetail), target);

        }

        [HttpDelete("Players/{playerId}/Games/{gameId}")]
        [SwaggerOperation(
            Summary = "Delete an Game using its ID",
            Description = "Fetches the Store details if the item exists, Then delete it."
        )]
        [ProducesResponseType(typeof(Game), StatusCodes.Status201Created)] // Specifies the response type for 200 OK
        [ProducesResponseType(StatusCodes.Status401Unauthorized)] // Specifies a 404 Not Found response
        public async Task<IActionResult> Delete(string gameId, Guid playerId)
        {   
            var result = await _gameService.DeleteGameRequest(gameId, playerId);
            return (result) 
                    ? NoContent()
                    : Unauthorized("Requestor is not authorised to delete the resource");
        }

        [HttpDelete("Players/{playerId}/Games/")]
        [SwaggerOperation(
            Summary = "Delete all Game using its author ID",
            Description = "Fetches all game details if the they exists, Then delete them."
        )]
        [ProducesResponseType(typeof(Game), StatusCodes.Status204NoContent)] // Specifies the response type for 200 OK
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Specifies a 404 Not Found response
        public async Task<IActionResult> DeleteAll(Guid playerId)
        {
            await _gameService.DeleteAllGameByAuthorId(playerId);
            return NoContent();
        }


        [HttpGet("Players/Player/Games/{gameId}")]
        [SwaggerOperation(
            Summary = "Get an Game using its ID",
            Description = "Attempt to retrieve the game context."
        )]
        [ProducesResponseType(typeof(Game), StatusCodes.Status200OK)] // Specifies the response type for 200 OK
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Specifies a 404 Not Found response
        public async Task<IActionResult> Get(string gameId)
        {
            var target = await _gameService.GetByIdAsync(gameId);
            return Ok(target);
        }

        [HttpGet("Players/{playerId}/Games")]
        [SwaggerOperation(
            Summary = "Get all Games using theirs author ID",
            Description = "Attempt to retrieve all of the author (identify by his/her Id) the game context."
        )]
        [ProducesResponseType(typeof(Game), StatusCodes.Status200OK)] // Specifies the response type for 200 OK
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Specifies a 404 Not Found response
        public async Task<IActionResult> Get( Guid playerId)
        {
            var targets = await _gameService.GetAllGameByAuthorId(playerId);
            return Ok(targets);
        }

        [HttpGet("Players/player/Game")]
        [SwaggerOperation(
            Summary = "Get all created Games",
            Description = "Attempt to retrieve all of the game context."
        )]
        [ProducesResponseType(typeof(Game), StatusCodes.Status200OK)] // Specifies the response type for 200 OK
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _gameService.GetAllAsync());
        }
    }
}
