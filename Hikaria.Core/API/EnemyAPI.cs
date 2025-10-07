using Enemies;
using Hikaria.Core.Features.Dev;
using SNetwork;

namespace Hikaria.Core;

public static class EnemyAPI
{
    #region Delegate
    public delegate void EnemyReceivedDamage(EnemyAgent enemy, pFullEnemyReceivedDamageData data);
    #endregion

    public static event Action<Dam_EnemyDamageLimb> OnEnemyLimbDestroyed { add => EnemyAPI_Impl.OnEnemyLimbDestroyed += value; remove => EnemyAPI_Impl.OnEnemyLimbDestroyed -= value; }
    public static event Action<EnemyAgent> OnEnemyDead { add => EnemyAPI_Impl.OnEnemyDead += value; remove => EnemyAPI_Impl.OnEnemyDead -= value; }
    public static event EnemyReceivedDamage OnEnemyReceivedDamage { add => EnemyAPI_Impl.OnEnemyReceivedDamage += value; remove => EnemyAPI_Impl.OnEnemyReceivedDamage -= value; }
    public static event Action<EnemyAgent> OnEnemySpawned { add => EnemyAPI_Impl.OnEnemySpawned += value; remove => EnemyAPI_Impl.OnEnemySpawned -= value; }
    public static event Action<EnemyAgent> OnEnemyDespawn { add => EnemyAPI_Impl.OnEnemyDespawn += value; remove => EnemyAPI_Impl.OnEnemyDespawn -= value; }
}
