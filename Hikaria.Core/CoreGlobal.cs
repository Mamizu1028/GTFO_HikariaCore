using SNetwork;

namespace Hikaria.Core
{
    public static class CoreGlobal
    {
        public const string OfficialServerUrl = "https://q1w2e3r4t5y6u7i8o9p0.top:50001/api/gtfo";
        public static string ThirdPartyServerUrl = string.Empty;
        public static bool UseThirdPartyServer = false;

        public static string ServerUrl => UseThirdPartyServer ? ThirdPartyServerUrl : OfficialServerUrl;

        public static int Revision => SNet.GameRevision;
        public static string RevisionString => SNet.GameRevisionString;
    }
}
