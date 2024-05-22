using Hikaria.Core.WebAPI.Entitites;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Hikaria.Core.WebAPI.Utility
{
    public class JwtHelper
    {
        public static Task<string> CreateToken(IEnumerable<Claim> claims, JWTTokenOptions options)
        {
            DateTime expires = DateTime.Now.AddMinutes(options.ExpiredMinutes);
            byte[] keyBytes = Encoding.UTF8.GetBytes(options.SecurityKey);
            var secKey = new SymmetricSecurityKey(keyBytes);
            var credentials = new SigningCredentials(secKey, SecurityAlgorithms.HmacSha256Signature);
            var tokenDescriptor = new JwtSecurityToken(
                options.Issuer,
                options.Audience,
                expires: expires,
                signingCredentials: credentials, 
                claims: claims);
            return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(tokenDescriptor));
        }
    }
}