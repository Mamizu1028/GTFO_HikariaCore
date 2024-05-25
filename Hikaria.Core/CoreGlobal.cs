using Hikaria.Core.Entities;
using Hikaria.Core.Managers;
using Hikaria.Core.Utility;
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

        public static bool ServerOnline { get; private set; } = true;

        public static IPLocationInfo IPLocation { get; private set; }

        public static void CheckIsServerOnline()
        {
            Task.Run(async () =>
            {
                try
                {
                    ServerOnline = true;
                    ServerOnline = await HttpHelper.GetAsync<bool>($"{ServerUrl}/alive/checkalive");
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
                            Header = "Hikaria.Core",
                            UpperText = "<color=#FF8C00><size=125%>警告 Warning</size></color>\n\n<size=150%><color=red>当前服务端不在线，某些功能无法正常工作！\nThe server is offline, some features won't work!</size></color>",
                            LowerText = string.Empty,
                            PopupType = PopupType.BoosterImplantMissed,
                            OnCloseCallback = PopupMessageManager.EmptyAction
                        });
                    }
                }
            });
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
