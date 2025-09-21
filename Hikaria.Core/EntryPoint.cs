using TheArchive.Core;
using TheArchive.Core.Attributes;
using TheArchive.Core.Localization;
using TheArchive.Interfaces;

namespace Hikaria.Core;

[ArchiveDependency("dev.gtfomodding.gtfo-api")]
[ArchiveModule(CoreGlobal.GUID, CoreGlobal.NAME, CoreGlobal.VERSION)]
public class EntryPoint : IArchiveModule
{
    public ILocalizationService LocalizationService { get; set; }

    public IArchiveLogger Logger { get; set; }

    public void Init()
    {
        CoreGlobal.Setup(this);
    }
}
