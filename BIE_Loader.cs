using BepInEx;
using BepInEx.Unity.IL2CPP;
using TheArchive;

namespace Hikaria.Core;

[BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(ArchiveMod.GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
public class BIE_Loader : BasePlugin
{
    public override void Load()
    {
        ArchiveMod.RegisterArchiveModule(typeof(EntryPoint));
    }
}
