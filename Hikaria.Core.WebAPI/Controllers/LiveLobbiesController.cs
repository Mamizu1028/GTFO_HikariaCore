using Hikaria.Core.Contracts;
using Hikaria.Core.Entities;
using Hikaria.Core.WebAPI.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Hikaria.Core.WebAPI.Controllers
{
    [Route("api/gtfo/[controller]")]
    [ApiController]
    public class LiveLobbiesController : ControllerBase
    {
        private readonly ILogger<LiveLobbiesController> _logger;

        private readonly IRepositoryWrapper _repository;

        public LiveLobbiesController(IRepositoryWrapper repository, ILogger<LiveLobbiesController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpPut]
        public async Task<IActionResult> CreateLobby([FromBody] LiveLobby lobby)
        {
            try
            {
                if (lobby.LobbyID == 0)
                {
                    return StatusCode(StatusCodes.Status400BadRequest);
                }
                await _repository.LiveLobbies.CreateOrUpdateLobby(lobby);
                await _repository.Save();
                return StatusCode(StatusCodes.Status201Created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        public async Task<IActionResult> QueryLobby([FromBody] LiveLobbyQueryBase lobbyQuery)
        {
            try
            {
                return Ok(await _repository.LiveLobbies.QueryLobby(lobbyQuery));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [UserPrivilegeAuthorize(UserPrivilege.Admin)]
        [HttpGet]
        public async Task<IActionResult> GetLobbyLookup()
        {
            try
            {
                return Ok(await _repository.LiveLobbies.GetLobbyLookupNotTracking());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPatch("{lobbyId}/keep-alive")]
        public async Task<IActionResult> KeepLobbyAlive([FromRoute] ulong lobbyId)
        {
            try
            {
                await _repository.LiveLobbies.KeepLobbyAlive(lobbyId);
                await _repository.Save();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPatch("{lobbyId}/details")]
        public async Task<IActionResult> UpdateLobbyDetailedInfo([FromRoute] ulong lobbyId, [FromBody] DetailedLobbyInfo lobbyDetailedInfo)
        {
            try
            {
                await _repository.LiveLobbies.UpdateLobbyDetailInfo(lobbyId, lobbyDetailedInfo);
                await _repository.Save();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPatch("{lobbyId}/privacy")]
        public async Task<IActionResult> UpdateLobbyPrivacySettings([FromRoute] ulong lobbyId, [FromBody] LobbyPrivacySettings lobbyPrivacySettings)
        {
            try
            {
                await _repository.LiveLobbies.UpdateLobbyPrivacySettings(lobbyId, lobbyPrivacySettings);
                await _repository.Save();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
