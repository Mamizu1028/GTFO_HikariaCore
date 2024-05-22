namespace Hikaria.Core.WebAPI.Entitites
{
    public class JWTTokenOptions
    {
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public string SecurityKey { get; set; }
        public int ExpiredMinutes { get; set; }
    }
}
