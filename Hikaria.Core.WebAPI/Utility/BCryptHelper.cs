namespace Hikaria.Core.WebAPI.Utility
{
    public class BCryptHelper
    {
        public static Task<string> EnhancedHashPasswordAsync(string input)
        {
            return Task.FromResult(BC.EnhancedHashPassword(input, BCrypt.Net.HashType.SHA384));
        }

        public static Task<bool> EnhancedVerifyPasswordAsync(string input, string hash)
        {
            return Task.FromResult(BC.EnhancedVerify(input, hash, BCrypt.Net.HashType.SHA384));
        }

        public static string EnhancedHashPassword(string input)
        {
            return BC.EnhancedHashPassword(input, BCrypt.Net.HashType.SHA384);
        }

        public static bool EnhancedVerifyPassword(string input, string hash)
        {
            return BC.EnhancedVerify(input, hash, BCrypt.Net.HashType.SHA384);
        }
    }
}
