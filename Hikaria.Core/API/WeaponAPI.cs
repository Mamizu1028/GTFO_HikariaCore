using Gear;
using Hikaria.Core.Features.Dev;

namespace Hikaria.Core;

public static class WeaponAPI
{
    #region Delegates
    public delegate void PreBulletWeaponFire(BulletWeapon bulletWeapon, bool resetRecoilSimilarity);
    public delegate void PostBulletWeaponFire(BulletWeapon bulletWeapon, bool resetRecoilSimilarity);

    public delegate void PreShotgunFire(Shotgun shotgun, bool resetRecoilSimilarity);
    public delegate void PostShotgunFire(Shotgun shotgun, bool resetRecoilSimilarity);
    #endregion

    #region Events
    public static event PreBulletWeaponFire OnPreBulletWeaponFire { add => WeaponAPI_Impl.OnPreBulletWeaponFire += value; remove => WeaponAPI_Impl.OnPreBulletWeaponFire -= value; }
    public static event PostBulletWeaponFire OnPostBulletWeaponFire { add => WeaponAPI_Impl.OnPostBulletWeaponFire += value; remove => WeaponAPI_Impl.OnPostBulletWeaponFire -= value; }

    public static event PreShotgunFire OnPreShotgunFire { add => WeaponAPI_Impl.OnPreShotgunFire += value; remove => WeaponAPI_Impl.OnPreShotgunFire -= value; }
    public static event PostShotgunFire OnPostShotgunFire { add => WeaponAPI_Impl.OnPostShotgunFire += value; remove => WeaponAPI_Impl.OnPostShotgunFire -= value; }
    #endregion
}
