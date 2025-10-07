namespace Hikaria.Core;

public enum SessionMemberEvent
{
    JoinSessionHub,
    LeftSessionHub
}

public enum DamageTraceFlags : uint
{
    None = 0,
    Bullet = 1 << 0,
    Melee = 1 << 1,
    Explosion = 1 << 2,
    Fire = 1 << 3,
    Freeze = 1 << 4,
    Push = 1 << 5,
    SentryGun = 1 << 6,
    Player = 1 << 7,
    Enemy = 1 << 8,
    Decoy = 1 << 9,
    Unknown = 1 << 10
}