# Dispatch semantics

These rules preserve the established gameplay behavior implemented by `Runtime/PlayerEvents.cs` and
`Runtime/EntityEvents.cs`. `SkillDispatcherTests`
pins these rules with fakes so a future change to the dispatcher cannot silently drift
from them.

## PlayerHurtPre

- **Signature**: `bool PlayerHurtPre(EventPlayerHurt)`. `true` = suppress this hit (refund
  armor, zero `DmgHealth`/`DmgArmor`, so no client hit-feedback); `false`/unimplemented =
  no effect.
- **Who is asked**: the victim's current skill first. If it did **not** suppress, and the
  attacker is a different player with a **different** skill than the victim's, the
  attacker's skill is asked too. If the victim's skill suppressed, the attacker's skill is
  never asked (short-circuit).
- **Aggregation**: first `true` wins; there is no OR-across-many-skills beyond this
  victim/attacker pair. Not all active skills are consulted, only these (at most) two.
- **Order**: victim before attacker.
- **Guard**: skipped entirely if the event carries no damage (`DmgHealth <= 0 &&
  DmgArmor <= 0`), or if either player is invalid/`IsDrawing`.

## OnWeaponCanAcquire

- **Signature**: `bool OnWeaponCanAcquire(DynamicHook, CCSPlayerController, CEconItemView,
  CCSWeaponBaseVData)`. `true` = block the pickup/buy (native call blocked, `HookResult.Handled`
  returned to the engine); `false`/unimplemented = allow.
- **Who is asked**: **every distinct active skill in play** (not just the acquiring
  player's own skill) - `Instance.SkillPlayer.Where(!IsDrawing).Select(Skill).Distinct()`.
- **Aggregation**: OR - the first skill (in enumeration order, which is insertion order of
  distinct skills encountered while iterating players) to return `true` wins.
- **Order**: short-circuits on the first `true`; skills after it are never asked this call.

## WeaponDrop

- **Signature exists** (`bool WeaponDrop(DynamicHook, CCSPlayerController)`) and is
  **implemented by one skill** (`Iana`), but no runtime call site dispatches it. It remains
  intentionally unreachable because activating it would change gameplay behavior.

## Non-boolean fan-out hooks, for contrast

Most hooks (`OnTick`, `NewRound`, `RoundEnd`, `PlayerMakeSound`, `PlayerBlind`, `PlayerHurt`,
`PlayerDeath`, `PlayerJump`, `BotTakeover`, `WeaponFire`, `WeaponEquip`, `WeaponPickup`,
`WeaponReload`, `GrenadeThrown`, `BulletImpact`, `CheckTransmit`) fan out to **every distinct
active skill currently in play** (`DispatchToActiveSkills`), once per distinct skill per
event/tick, skipping `IsDrawing` players. No aggregation - every skill's hook runs
independently; a thrown exception in one is caught and logged, never aborting the others or
the engine callback.

Two hooks have extra ordering rules on top of that same "every distinct active skill" fan-out:

- **OnTick**: sorted so `ChillOut` runs after normal skills, and `AreaReaper` runs after
  `ChillOut` (both depend on other skills' tick results already being computed this frame).
  Skills flagged `disableOnFreezeTime` in configuration are skipped while the round is still
  in freeze time.
- **OnTakeDamage / OnTakeDamagePost**: skills in `[SecondLife, Phoenix, ReZombie]` ("late
  damage skills" - revive-on-lethal-damage heroes) always run **after** every other active
  skill, so they observe the final, fully-modified damage value.
