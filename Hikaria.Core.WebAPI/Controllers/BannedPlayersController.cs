using AutoMapper;
using Hikaria.Core.Contracts;
using Hikaria.Core.Entities;
using Hikaria.Core.WebAPI.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Hikaria.Core.WebAPI.Controllers
{
    [Route("api/gtfo/[controller]/")]
    [ApiController]
    public class BannedPlayersController : ControllerBase
    {
        private readonly ILogger<BannedPlayersController> _logger;
        private readonly IRepositoryWrapper _repository;
        private readonly IMapper _mapper;

        public BannedPlayersController(IRepositoryWrapper repository, ILogger<BannedPlayersController> logger, IMapper mapper)
        {
            _repository = repository;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllBannedPlayers()
        {
            try
            {
                var players = await _repository.BannedPlayers.GetAllBannedPlayers();
                return Ok(players);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [Route("{steamid}")]
        [HttpDelete]
        [UserPrivilegeAuthorize(UserPrivilege.BanPlayer)]
        public async Task<IActionResult> UnbanPlayer([FromRoute] ulong steamid)
        {
            try
            {
                await _repository.BannedPlayers.UnbanPlayer(steamid);
                await _repository.Save();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        [UserPrivilegeAuthorize(UserPrivilege.BanPlayer)]
        public async Task<IActionResult> BanPlayer([FromBody] BannedPlayer player)
        {
            try
            {
                await _repository.BannedPlayers.BanPlayer(player);
                await _repository.Save();
                return StatusCode(StatusCodes.Status201Created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("{steamid}")]
        public async Task<IActionResult> GetBannedPlayerBySteamID([FromRoute] ulong steamid)
        {
            try
            {
                var player = await _repository.BannedPlayers.GetBannedPlayerBySteamID(steamid);
                if (player == null)
                {
                    return NotFound();
                }
                return Ok(player);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
