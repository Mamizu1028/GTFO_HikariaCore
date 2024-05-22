using Hikaria.Core.Entities;
using Hikaria.Core.WebAPI.Managers;
using Microsoft.AspNetCore.Mvc;

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

        [HttpPost]
        public async Task<IActionResult> CreateLobby(LiveLobby lobby)
        {
            try
            {
                await LiveLobbyManager.CreateLobby(lobby.Identifier, lobby.Settings, lobby.DetailedInfo);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllLobbies(int revision, LobbyType lobbyType = LobbyType.Public)
        {
            try
            {
                return Ok(await LiveLobbyManager.GetAllLobbies(revision, lobbyType));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLobbiesLookup()
        {
            try
            {
                return Ok(await LiveLobbyManager.GetLobbiesLookup());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        public async Task<IActionResult> KeepLobbyAlive(int revision, ulong lobbyID)
        {
            try
            {
                await LiveLobbyManager.KeepLobbyAlive(revision, lobbyID);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateLobbyDetailedInfo(ulong lobbyID, DetailedLobbyInfo lobbyInfo)
        {
            try
            {
                await LiveLobbyManager.UpdateLobbyDetailInfo(lobbyID, lobbyInfo);
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
