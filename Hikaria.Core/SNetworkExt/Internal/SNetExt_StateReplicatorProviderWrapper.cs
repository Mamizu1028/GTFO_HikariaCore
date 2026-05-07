using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace Hikaria.Core.SNetworkExt;

internal class SNetExt_StateReplicatorProviderWrapper : Component
{
    [HideFromIl2Cpp]
    public ISNetExt_StateReplicatorProvider Provider { get; private set; }

    [HideFromIl2Cpp]
    public void Setup(ISNetExt_StateReplicatorProvider provider)
    {
        Provider = provider;
    }
}
