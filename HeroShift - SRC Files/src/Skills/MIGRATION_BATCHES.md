# Skill migration batches (REFACTOR.md section 23)

Status: **complete** on the `refactor` branch. All 142 built-in skills have stable
`SkillId` entries, typed option records, canonical metadata definitions, and explicit
hook registrations in `BuiltInSkillCatalog`. The live runtime cutover and legacy removal
are tracked separately from these definition-migration batches.

Generated via `dotnet run` on `tools/refactor-baseline`'s `classify` mode
(`ClassifyBatches.cs`), which buckets each of the 142 active skills by simple text
signals (RayTrace usage, entity/timer spawning, damage hooks, OnTick, target-selection).
Reviewed against REFACTOR.md's suggested categories; adjusted where reality didn't match:

- No skill file references WASDMenu directly (it's only used by the command/UI layer in
  `src/command/Command.cs`, not by individual skills), so REFACTOR.md's "Batch F: Menu and
  targeted skills" is folded into Batch G here - both are small, target-selection-flavored,
  and land in the same commit.

## Batch A: Simple passive skills (27) - complete

AntyFlash, Astronaut, Behind, Catapult, Disarmament, Dracula, Dwarf, FastReload,
FragileBomb, Grenadier, Illiterate, Impostor, InfiniteAmmo, JumpingJack, Knockback, None,
Push, Pyro, Rambo, ReturnToSender, RichBoy, RobinHood, Saper, ShortBomb, Silent,
Teleporter, Zeus

## Batch B: Tick and movement skills (51) - complete

AimLock, Anomaly, AreaReaper, Bankrupt, BunnyHop, C4Camouflage, Chicken, ChillOut,
Darkness, Dash, Deactivator, Deaf, Distancer, Duplicator, EnemySpawn, ExpensiveAmmo,
FalconEye, Flash, FrozenDecoy, Ghost, Giant, Glitch, HealingChicken, Jammer, JetKick,
JumpBan, JumpCurse, LifeSwap, MagneticDecoy, Magnifier, Medic, MoneySwap, Ninja,
NoRecoil, PawelJumper, Pilot, Planter, PrimaryBan, PsychicDefusing, QuickShot, RadarHack,
Regeneration, Retreat, Rubber, SoundMaker, Spectator, SwapPosition, TakeAmmo, Thief,
ThirdEye, WeaponsSwap

## Batch C: Damage pipeline skills (26) - complete

Aimbot, AntyHead, Armored, Assassin, Berserker, BladeMaster, CarefulBullets, Cutter,
DemonEye, Fortnite, FriendlyFire, HotBomb, KillerFlash, LastGasp, NoNades, OneShot,
OnlyHead, Phoenix, Poison, Prosthesis, ReZombie, ReactiveArmor, Replicator, SecondLife,
Soldier, Thorns

## Batch D: Entity and grenade skills (25) - complete

Baseball, BlastShot, DeathBomb, ExplodingBarrel, ExplosiveShot, FireRain, Flashlight,
Glue, GodMode, HealingSmoke, HolyHandGrenade, HomingNades, Illusionist, Jackal, Jester,
Magneto, Miner, Nightmare, RandomWeapon, SniperElite, ThrowingKnife, ToxicSmoke,
Watchmaker, Weightless, WildThrow

## Batch E: RayTrace skills (8) - complete

Cypher, Iana, LongKnife, LongZeus, Noclip, Shade, TeamTeleport, Tripwire

## Batch G: Remaining complex / target-selection skills (5) - complete

Gambler, Glaz, Hermit, Smoker, Wallhack

REFACTOR.md's Batch F is empty for the reason above. The next implementation checkpoint is
the typed runtime/configuration/localization cutover, followed by legacy removal, packaging,
CI hardening, and CS2 Docker smoke validation.
