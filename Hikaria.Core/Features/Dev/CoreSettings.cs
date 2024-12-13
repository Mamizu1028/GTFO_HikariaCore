using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Settings;

namespace Hikaria.Core.Features.Dev
{
    [EnableFeatureByDefault]
    [HideInModSettings]
    [DisallowInGameToggle]
    [DoNotSaveToConfig]
    internal class CoreSettings : Feature
    {
        public override string Name => "Core Settings";
        public override FeatureGroup Group => EntryPoint.Groups.Dev;

        public override bool RequiresRestart => true;

        [FeatureConfig]
        public static CoreSettingsSettings Settings { get; set; }

        public class CoreSettingsSettings
        {
            [FSDisplayName("使用第三方服务器")]
            public bool UseThirdPartyServer { get => CoreGlobal.UseThirdPartyServer; set => CoreGlobal.UseThirdPartyServer = value; }
            [FSDisplayName("第三方服务器链接")]
            public string ThirdPartyServerUrl { get => CoreGlobal.ThirdPartyServerUrl; set => CoreGlobal.ThirdPartyServerUrl = value; }
        }

        public override void Init()
        {
            Task.Run(() =>
            {
                while (CoreGlobal.ServerOnline)
                {
                    CoreGlobal.CheckIsServerOnline();
                    Thread.Sleep(TimeSpan.FromMinutes(5));
                }
            });
            //CoreGlobal.GetIPLocationInfo();
        }

        public override void OnFeatureSettingChanged(FeatureSetting setting)
        {
            CoreGlobal.CheckIsServerOnline();
        }
    }
}
