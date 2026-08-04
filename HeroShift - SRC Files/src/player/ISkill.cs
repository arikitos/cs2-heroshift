using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.UserMessages;

namespace src.player;

/*
 * ISkill - the contract every hero (skill) in the plugin implements.
 *
 * HOW THE SKILL SYSTEM WORKS
 * Every skill lives in its own file under src/player/skills/<Name>.cs and is a
 * `public class <Name> : ISkill`. The methods below are static gameplay hooks.
 * BuiltInSkillCatalog registers the hooks explicitly against a stable SkillId,
 * and SkillDispatcher invokes those typed delegates directly. That means:
 *   - A skill only implements the hooks it actually needs; an unregistered hook
 *     simply does nothing.
 *   - The method signature in a skill file must match the typed SkillHookSet
 *     delegate assigned by its canonical definition.
 *
 * LIFECYCLE OF A SKILL (typical round)
 *   LoadSkill()    - once at plugin load; registers the skill + its HUD color.
 *   EnableSkill()  - when a player is given this skill (round start / !skill).
 *                    This is where you grant weapons, set speed/gravity, etc.
 *   ...hooks fire while the round is running (damage, tick, grenades, ...)
 *   UseSkill()     - the player pressed the skill button (E / configured key).
 *   DisableSkill() - the skill is taken away (round end, death, reroll).
 *                    MUST undo everything EnableSkill did, or effects leak into
 *                    the next round.
 *
 * WHERE THE TUNABLE VALUES LIVE
 * Every built-in skill has a canonical typed options record under src/Skills/BuiltIn.
 * Code owns the defaults; configs/heroshift.json contains server-specific overrides.
 * Gameplay code reads its typed options through SkillRuntime, without reflection or
 * string property names.
 */
public interface ISkill
{
    // ---- Lifecycle -------------------------------------------------------
    // LoadSkill: once at plugin startup. Register the skill / precache models.
    // EnableSkill / DisableSkill: skill granted to / taken from one player.
    // UseSkill: the player pressed the skill activation button.
    // TypeSkill: the player passed arguments (e.g. picked a target from a menu);
    //            string[] holds the command arguments.
    public static void LoadSkill() { }
    public static void EnableSkill(CCSPlayerController _) { }
    public static void DisableSkill(CCSPlayerController _) { }
    public static void UseSkill(CCSPlayerController _) { }
    public static void TypeSkill(CCSPlayerController _, string[] __) { }

    // ---- Engine-level hooks (memory hooks, not game events) ---------------
    // OnTakeDamage: fires BEFORE damage is applied - this is where you change or
    //   cancel damage (read/write the CTakeDamageInfo out of the DynamicHook).
    //   Used by armor/reflect/one-shot type heroes.
    // OnTakeDamagePost: after damage was applied (react only, cannot change it).
    // OnEntitySpawned: any entity appeared in the world (grenade projectile,
    //   chicken, hostage...). Used to grab and modify freshly spawned entities.
    // OnTick: every server tick (64/s by default). Cheap per-frame work only.
    // CheckTransmit: decides which entities each client is ALLOWED to see -
    //   this is how invisibility / wallhack / spectator-style skills work.
    public static void OnTakeDamage(DynamicHook _) { }
    public static void OnTakeDamagePost(DynamicHook _) { }
    public static void OnEntitySpawned(CEntityInstance _) { }
    public static void OnTick() { }
    public static void CheckTransmit([CastFrom(typeof(nint))] CCheckTransmitInfoList _) { }

    // ---- Round + player game events ---------------------------------------
    // NewRound / RoundEnd: reset per-round state (clear dictionaries here!).
    // PlayerMakeSound: footstep/other sound user message - clear um.Recipients
    //   to make a player silent for everyone.
    // PlayerBlind: a player got flashed; FlashDuration on the pawn = how long.
    // PlayerHurt: damage already happened (post). PlayerHurtPre: return true to
    //   block the plugin's default handling of that hurt event.
    public static void NewRound() { }
    public static void RoundEnd() { }
    public static void PlayerMakeSound(UserMessage _) { }
    public static void PlayerBlind(EventPlayerBlind _) { }
    public static void PlayerHurt(EventPlayerHurt _) { }
    public static bool PlayerHurtPre(EventPlayerHurt _) { return false; }
    public static void PlayerDeath(EventPlayerDeath _) { }
    public static void PlayerJump(EventPlayerJump _) { }
    public static void SwitchTeam(EventSwitchTeam _, GameEventInfo __) { }
    public static void BotTakeover(EventBotTakeover _) { }

    // ---- Weapon / grenade events ------------------------------------------
    // WeaponDrop returns true to BLOCK the drop (used by "can't drop" heroes).
    // GrenadeThrown fires on throw; @event.Weapon is "flashbang" / "hegrenade" /
    //   "smokegrenade" / "molotov" / "decoy" - filter on it to target one nade.
    public static void WeaponFire(EventWeaponFire _) { }
    public static void WeaponEquip(EventItemEquip _) { }
    public static void WeaponPickup(EventItemPickup _) { }
    public static void WeaponReload(EventWeaponReload _) { }
    public static bool WeaponDrop(DynamicHook _, CCSPlayerController __) { return false; }
    public static void GrenadeThrown(EventGrenadeThrown _) { }
    public static void BulletImpact(EventBulletImpact _) { }

    // ---- Bomb (C4) events - used by plant/defuse related heroes -----------
    public static void BombBeginplant(EventBombBeginplant _) { }
    public static void BombAbortplant(EventBombAbortplant _) { }
    public static void BombPlanted(EventBombPlanted _) { }
    public static void BombBegindefuse(EventBombBegindefuse _) { }

    // ---- Decoy + smoke lifecycle (heal/toxic/freeze smoke heroes) ---------
    public static void DecoyStarted(EventDecoyStarted _) { }
    public static void DecoyDetonate(EventDecoyDetonate _) { }

    public static void SmokegrenadeDetonate(EventSmokegrenadeDetonate _) { }
    public static void SmokegrenadeExpired(EventSmokegrenadeExpired _) { }

    // ---- Map triggers + weapon pickup filter ------------------------------
    // OnWeaponCanAcquire: return true to forbid picking up / buying a weapon
    // (used by heroes restricted to knife/pistol only).
    public static void OnTriggerEnter(CBaseTrigger _, CBaseEntity __) { }
    public static void OnTriggerExit(CBaseTrigger _, CBaseEntity __) { }
    public static bool OnWeaponCanAcquire(DynamicHook _, CCSPlayerController __, CEconItemView ___, CCSWeaponBaseVData ____) { return false; }

}

// Legacy player-state identity retained for runtime compatibility. Canonical
// registration and configuration identity live in BuiltInSkillCatalog/SkillId.

public enum Skills
{
    None,
    Aimbot,
    AimLock,
    Anomaly,
    AntyFlash,
    AntyHead,
    AreaReaper,
    Armored,
    Assassin,
    Astronaut,
    Bankrupt,
    Baseball,
    Behind,
    Berserker,
    BladeMaster,
    BunnyHop,
    BlastShot,
    C4Camouflage,
    CarefulBullets,
    Catapult,
    Chicken,
    ChillOut,
    Cutter,
    Cypher,
    Darkness,
    Deactivator,
    Deaf,
    DeathBomb,
    DemonEye,
    Disarmament,
    Distancer,
    Dash,
    Dracula,
    Duplicator,
    Dwarf,
    EnemySpawn,
    ExpensiveAmmo,
    ExplodingBarrel,
    ExplosiveShot,
    FalconEye,
    FastReload,
    FireRain,
    Flash,
    Flashlight,
    Fortnite,
    FragileBomb,
    FriendlyFire,
    FrozenDecoy,
    Gambler,
    Ghost,
    Giant,
    Glaz,
    Glitch,
    Glue,
    GodMode,
    Grenadier,
    HealingChicken,
    HealingSmoke,
    Hermit,
    HolyHandGrenade,
    HomingNades,
    HotBomb,
    Iana,
    Illiterate,
    Illusionist,
    Impostor,
    InfiniteAmmo,
    Jackal,
    Jammer,
    Jester,
    JetKick,
    JumpBan,
    JumpCurse,
    JumpingJack,
    KillerFlash,
    Knockback,
    LastGasp,
    LifeSwap,
    LongKnife,
    LongZeus,
    MagneticDecoy,
    Magneto,
    Magnifier,
    Medic,
    Miner,
    MoneySwap,
    Nightmare,
    Ninja,
    NoNades,
    NoRecoil,
    Noclip,
    OneShot,
    OnlyHead,
    PawelJumper,
    Phoenix,
    PsychicDefusing,
    Pilot,
    Planter,
    Poison,
    PrimaryBan,
    Prosthesis,
    Push,
    Pyro,
    QuickShot,
    RadarHack,
    Rambo,
    RandomWeapon,
    ReZombie,
    ReactiveArmor,
    Regeneration,
    Replicator,
    Retreat,
    ReturnToSender,
    RichBoy,
    RobinHood,
    Rubber,
    Saper,
    SecondLife,
    Shade,
    ShortBomb,
    Silent,
    Smoker,
    SniperElite,
    Soldier,
    SoundMaker,
    Spectator,
    SwapPosition,
    TakeAmmo,
    TeamTeleport,
    Teleporter,
    Thief,
    ThirdEye,
    Thorns,
    ThrowingKnife,
    ToxicSmoke,
    Tripwire,
    Wallhack,
    Watchmaker,
    WeaponsSwap,
    Weightless,
    WildThrow,
    Zeus,
}
