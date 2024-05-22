using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.Core.Features.Dev
{
    [AutomatedFeature]
    [DoNotSaveToConfig]
    public class CoreSettings : Feature
    {
        public override string Name => "Core Settings";
        public override FeatureGroup Group => EntryPoint.Groups.Dev;

        [FeatureConfig]
        public static CoreSettingsSettings Settings { get; set; }

        public class CoreSettingsSettings
        {
            public bool UseThirdPartyServer { get => CoreGlobal.UseThirdPartyServer; set => CoreGlobal.UseThirdPartyServer = value; }

            public string ThirdPartyServerUrl { get => CoreGlobal.ThirdPartyServerUrl; set => CoreGlobal.ThirdPartyServerUrl = value; }
        }

        public override void Init()
        {
            FeatureManager.EnableAutomatedFeature(typeof(CoreSettings));
        }
    }
}
