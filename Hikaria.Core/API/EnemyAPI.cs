using Enemies;
using Hikaria.Core.Features.Dev;

namespace Hikaria.Core;

public static class EnemyAPI
{
    public static event Action<Dam_EnemyDamageBase> OnEnemyHealthReceived { add => EnemyAPI_Impl.OnEnemyHealthReceived += value; remove => EnemyAPI_Impl.OnEnemyHealthReceived -= value; }
    public static event Action<Dam_EnemyDamageLimb> OnEnemyLimbHealthReceived { add => EnemyAPI_Impl.OnEnemyLimbHealthReceived += value; remove => EnemyAPI_Impl.OnEnemyLimbHealthReceived -= value; }
    public static event Action<Dam_EnemyDamageLimb> OnEnemyLimbDestroyed { add => EnemyAPI_Impl.OnEnemyLimbDestroyed += value; remove => EnemyAPI_Impl.OnEnemyLimbDestroyed -= value; }
    public static event Action<EnemyAgent> OnEnemyDead { add => EnemyAPI_Impl.OnEnemyDead += value; remove => EnemyAPI_Impl.OnEnemyDead -= value; }
}
