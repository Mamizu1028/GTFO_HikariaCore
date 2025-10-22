using Clonesoft.Json;
using Clonesoft.Json.Converters;
using Clonesoft.Json.Serialization;
using SNetwork;
using TheArchive.Core;
using TheArchive.Core.Localization;

namespace Hikaria.Core;

public static class CoreGlobal
{
    public const string GUID = "Hikaria.Core";

    public const string NAME = "HikariaCore";

    public const string VERSION = "1.0.0";

    public static int Revision => SNet.GameRevision;

    public static string RevisionString => SNet.GameRevisionString;

    internal static ILocalizationService Localization { get; private set; }

    internal static JsonSerializerSettings JsonSerializerSettings { get; private set; }

    public static void Setup(IArchiveModule module)
    {
        Localization = module.LocalizationService;

        Logs.Setup(module.Logger);

        JsonSerializerSettings = new JsonSerializerSettings()
        {
            Formatting = Formatting.Indented,
            ContractResolver = new DefaultContractResolver(),
            DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
            DateFormatString = "yyyy-MM-dd HH:mm:ss",
            NullValueHandling = NullValueHandling.Include

        };

        JsonSerializerSettings.Converters.Add(new IsoDateTimeConverter() { DateTimeFormat = "yyyy-MM-dd HH:mm:ss" });
        JsonSerializerSettings.Converters.Add(new StringEnumConverter());
    }
}
