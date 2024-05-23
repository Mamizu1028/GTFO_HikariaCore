using Hikaria.Core.Contracts;
using Hikaria.Core.Entities;
using Hikaria.Core.WebAPI.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Hikaria.Core.WebAPI.Controllers
{
    [Route("api/gtfo/[controller]/[action]")]
    [ApiController]
    public class BannedPlayersController : ControllerBase
    {
        private readonly ILogger<BannedPlayersController> _logger;

        private readonly IRepositoryWrapper _repository;

        public BannedPlayersController(IRepositoryWrapper repository, ILogger<BannedPlayersController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [UserPrivilegeAuthorize(UserPrivilege.Admin)]
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
                return StatusCode(500);
            }
        }

        [HttpDelete]
        [UserPrivilegeAuthorize(UserPrivilege.BanPlayer)]
        public async Task<IActionResult> UnbanPlayer(BannedPlayer player)
        {
            try
            {
                _repository.BannedPlayers.UnbanPlayer(player);
                await _repository.Save();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500);
            }
        }

        [HttpPost]
        [UserPrivilegeAuthorize(UserPrivilege.BanPlayer)]
        public async Task<IActionResult> BanPlayer(BannedPlayer player)
        {
            try
            {
                _repository.BannedPlayers.BanPlayer(player);
                await _repository.Save();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500);
            }
        }
    }
}
