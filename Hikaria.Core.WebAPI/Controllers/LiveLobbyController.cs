using Hikaria.Core.Entities;
using Hikaria.Core.WebAPI.Managers;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Hikaria.Core.WebAPI.Controllers
{
    [Route("api/gtfo/[controller]/[action]")]
    [ApiController]
    public class LiveLobbyController : ControllerBase
    {
        private readonly ILogger<LiveLobbyController> _logger;

        public LiveLobbyController(ILogger<LiveLobbyController> logger)
        {
            _logger = logger;
        }

        [HttpPut]
        public async Task<IActionResult> CreateLobby([FromBody] LiveLobby lobby)
        {
            try
            {
                await LiveLobbyManager.CreateLobby(lobby.Identifier, lobby.PrivacySettings, lobby.DetailedInfo);
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
                return Ok(await LiveLobbyManager.QueryLobby(lobbyQuery));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLobbyLookup()
        {
            try
            {
                return Ok(await LiveLobbyManager.GetLobbyLookup());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPatch]
        public async Task<IActionResult> KeepLobbyAlive(int revision, ulong lobbyID)
        {
            try
            {
                return Ok(await LiveLobbyManager.KeepLobbyAlive(revision, lobbyID));
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
                return Ok(await LiveLobbyManager.UpdateLobbyDetailInfo(lobbyID, lobbyDetailedInfo));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateLobbyPrivacySettings(int revision, ulong lobbyID, [FromBody] LobbyPrivacySettings lobbyPrivacySettings)
        {
            try
            {
                return Ok(await LiveLobbyManager.UpdateLobbyPrivacySettings(revision, lobbyID, lobbyPrivacySettings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
