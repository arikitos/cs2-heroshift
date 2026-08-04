using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.UserMessages;

namespace src.SkillsCore.Abstractions;

/*
 * SkillHookSet - typed replacement for reflection-based skill dispatch.
 *
 * Every delegate here matches one Runtime/ISkill static method
 * signature exactly (see that file for full hook documentation). A skill
 * only sets the hooks it actually implements; everything else stays null,
 * so SkillRegistry can pre-index skills per hook instead of probing every skill for every
 * event.
 */
public sealed class SkillHookSet
{
    // ---- Lifecycle ---------------------------------------------------
    public Action? LoadSkill { get; init; }
    public Action<CCSPlayerController>? EnableSkill { get; init; }
    public Action<CCSPlayerController>? DisableSkill { get; init; }
    public Action<CCSPlayerController>? UseSkill { get; init; }
    public Action<CCSPlayerController, string[]>? TypeSkill { get; init; }

    // ---- Engine-level hooks --------------------------------------------
    public Action<DynamicHook>? OnTakeDamage { get; init; }
    public Action<DynamicHook>? OnTakeDamagePost { get; init; }
    public Action<CEntityInstance>? OnEntitySpawned { get; init; }
    public Action? OnTick { get; init; }
    public Action<CCheckTransmitInfoList>? CheckTransmit { get; init; }

    // ---- Round + player game events ------------------------------------
    public Action? NewRound { get; init; }
    public Action? RoundEnd { get; init; }
    public Action<UserMessage>? PlayerMakeSound { get; init; }
    public Action<EventPlayerBlind>? PlayerBlind { get; init; }
    public Action<EventPlayerHurt>? PlayerHurt { get; init; }
    public Func<EventPlayerHurt, bool>? PlayerHurtPre { get; init; }
    public Action<EventPlayerDeath>? PlayerDeath { get; init; }
    public Action<EventPlayerJump>? PlayerJump { get; init; }
    public Action<EventSwitchTeam, GameEventInfo>? SwitchTeam { get; init; }
    public Action<EventBotTakeover>? BotTakeover { get; init; }
    public Action<uint>? PlayerDisconnect { get; init; }

    // ---- Weapon / grenade events ----------------------------------------
    public Action<EventWeaponFire>? WeaponFire { get; init; }
    public Action<EventItemEquip>? WeaponEquip { get; init; }
    public Action<EventItemPickup>? WeaponPickup { get; init; }
    public Action<EventWeaponReload>? WeaponReload { get; init; }
    public Func<DynamicHook, CCSPlayerController, bool>? WeaponDrop { get; init; }
    public Action<EventGrenadeThrown>? GrenadeThrown { get; init; }
    public Action<EventBulletImpact>? BulletImpact { get; init; }

    // ---- Bomb (C4) events -------------------------------------------------
    public Action<EventBombBeginplant>? BombBeginplant { get; init; }
    public Action<EventBombAbortplant>? BombAbortplant { get; init; }
    public Action<EventBombPlanted>? BombPlanted { get; init; }
    public Action<EventBombBegindefuse>? BombBegindefuse { get; init; }

    // ---- Decoy + smoke lifecycle -----------------------------------------
    public Action<EventDecoyStarted>? DecoyStarted { get; init; }
    public Action<EventDecoyDetonate>? DecoyDetonate { get; init; }
    public Action<EventSmokegrenadeDetonate>? SmokegrenadeDetonate { get; init; }
    public Action<EventSmokegrenadeExpired>? SmokegrenadeExpired { get; init; }

    // ---- Map triggers + weapon pickup filter -----------------------------
    public Action<CBaseTrigger, CBaseEntity>? OnTriggerEnter { get; init; }
    public Action<CBaseTrigger, CBaseEntity>? OnTriggerExit { get; init; }
    public Func<DynamicHook, CCSPlayerController, CEconItemView, CCSWeaponBaseVData, bool>? OnWeaponCanAcquire { get; init; }
}
