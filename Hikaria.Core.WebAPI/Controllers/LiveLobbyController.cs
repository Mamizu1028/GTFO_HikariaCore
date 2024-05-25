using Hikaria.Core.Contracts;
using Hikaria.Core.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Hikaria.Core.WebAPI.Controllers
{
    [Route("api/gtfo/[controller]/[action]")]
    [ApiController]
    public class LiveLobbyController : ControllerBase
    {
        private readonly ILogger<LiveLobbyController> _logger;

        private readonly IRepositoryWrapper _repository;

        public LiveLobbyController(IRepositoryWrapper repository, ILogger<LiveLobbyController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpPut]
        public async Task<IActionResult> CreateLobby([FromBody] LiveLobby lobby)
        {
            try
            {
                await _repository.LiveLobbies.CreateOrUpdateLobby(lobby);
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

        //[UserPrivilegeAuthorize(UserPrivilege.Admin)]
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

        [HttpPatch]
        public async Task<IActionResult> KeepLobbyAlive(ulong lobbyID)
        {
            try
            {
                await _repository.LiveLobbies.KeepLobbyAlive(lobbyID);
                await _repository.Save();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateLobbyDetailedInfo(ulong lobbyID, [FromBody] DetailedLobbyInfo lobbyDetailedInfo)
        {
            try
            {
                await _repository.LiveLobbies.UpdateLobbyDetailInfo(lobbyID, lobbyDetailedInfo);
                await _repository.Save();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateLobbyPrivacySettings(ulong lobbyID, [FromBody] LobbyPrivacySettings lobbyPrivacySettings)
        {
            try
            {
                await _repository.LiveLobbies.UpdateLobbyPrivacySettings(lobbyID, lobbyPrivacySettings);
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
