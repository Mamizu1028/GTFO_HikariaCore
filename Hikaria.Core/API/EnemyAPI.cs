using Hikaria.Core.Features.Dev;
using Hikaria.Core.Features.Fixes;

namespace Hikaria.Core;

public static class EnemyAPI
{
    public static event Action<Dam_EnemyDamageBase> OnEnemyHealthReceived { add => EnemyAPI_Impl.OnEnemyHealthReceived += value; remove => EnemyAPI_Impl.OnEnemyHealthReceived -= value; }
    public static event Action<Dam_EnemyDamageLimb> OnEnemyLimbHealthReceived { add => EnemyAPI_Impl.OnEnemyLimbHealthReceived += value; remove => EnemyAPI_Impl.OnEnemyLimbHealthReceived -= value; }
}
