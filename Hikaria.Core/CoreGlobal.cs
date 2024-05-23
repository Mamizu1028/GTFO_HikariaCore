using Clonesoft.Json;
using Hikaria.Core.Entities;
using Hikaria.Core.Managers;
using Hikaria.Core.Utility;
using SNetwork;
using System.Net.NetworkInformation;

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

        public static bool ServerOnline { get; private set; }

        public static IPLocationInfo IPLocation { get; private set; }

        public static void CheckIsServerOnline()
        {
            try
            {
                using (Ping ping = new Ping())
                {
                    PingReply reply = ping.Send(new Uri(ServerUrl).Host);
                    ServerOnline = reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                ServerOnline = false;
            }
            finally
            {
                if (!ServerOnline)
                {
                    Logs.LogError($"Server: \"{ServerUrl}\" is offline!!!");
                    PopupMessageManager.ShowPopup(new()
                    {
                        BlinkInContent = true,
                        BlinkTimeInterval = 0.5f,
                        Header = "Hikaria.Core <color=red>警告</color>",
                        UpperText = "<color=red><size=200%>当前服务端不在线，依赖在线服务的功能无法工作！</size></color>",
                        LowerText = string.Empty,
                        PopupType = PopupType.BoosterImplantMissed,
                        OnCloseCallback = PopupMessageManager.EmptyAction
                    });
                }
            }
        }

        public static void GetIPLocationInfo()
        {
            Task.Run(async () =>
            {
                IPLocation = await HttpHelper.GetAsync<IPLocationInfo>("https://api.ip.sb/geoip");
            });
        }
    }
}
