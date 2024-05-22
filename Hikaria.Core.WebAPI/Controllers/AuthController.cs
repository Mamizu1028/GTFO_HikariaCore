using Hikaria.Core.Contracts;
using Hikaria.Core.Entities;
using Hikaria.Core.WebAPI.Attributes;
using Hikaria.Core.WebAPI.Entitites;
using Hikaria.Core.WebAPI.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Hikaria.Core.WebAPI.Controllers;

[Route("api/gtfo/[controller]/[action]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly IRepositoryWrapper _repository;
    private readonly JWTTokenOptions _jwtTokenOptions;

    public AuthController(IRepositoryWrapper repository, ILogger<AuthController> logger, IOptionsMonitor<JWTTokenOptions> jwtTokenOptions)
    {
        _repository = repository;
        _logger = logger;
        _jwtTokenOptions = jwtTokenOptions.CurrentValue;
    }

    public class SteamUserModel
    {
        public ulong SteamID { get; set; }
        public string Password { get; set; }
    }

    [UserPrivilegeAuthorize(UserPrivilege.Admin)]
    [HttpPost]
    public async Task<JsonResult> Register([FromBody] SteamUserModel request)
    {
        var result = _repository.SteamUsers.FindByCondition(p => p.SteamID == request.SteamID);
        if (result != null)
        {
            return await Task.FromResult(new JsonResult(new
            {
                Message = "注册失败",
                Reason = "已存在相同SteamID的用户"
            }));
        }
        _repository.SteamUsers.Create(new() { SteamID = request.SteamID, Password = request.Password});
        await _repository.Save();
        return await Task.FromResult(new JsonResult(new
        {
            Message = "注册成功"
        }));
    }

    [HttpPost]
    public async Task<JsonResult> Login([FromBody] SteamUserModel request)
    {
        var user = await _repository.SteamUsers.FindUser(request.SteamID);
        if (user == null)
        {
            return await Task.FromResult(new JsonResult(new
            {
                Message = "登录失败",
                Reason = "用户不存在或密码错误"
            }));
        }
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.Sid, user.SteamID.ToString())
        };
        foreach (var role in Enum.GetValues<UserPrivilege>())
        {
            if (user.Privilege.HasFlag(role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
            }
        }
        var token = await JwtHelper.CreateToken(claims, _jwtTokenOptions);
        return await Task.FromResult(new JsonResult(new
        {
            Message = "登陆成功",
            Token = token
        }));
    }
}