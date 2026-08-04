from pathlib import Path

path = Path("HeroShift - SRC Files/src/player/PlayerEvents.cs")
text = path.read_text(encoding="utf-8")

replacements = {
    'DispatchToActiveSkills("PlayerMakeSound", um);': 'Instance.SkillDispatcher.DispatchPlayerMakeSound(GetActiveSkillIds(), um);',
    'DispatchToActiveSkills("WeaponFire", @event);': 'Instance.SkillDispatcher.DispatchWeaponFire(GetActiveSkillIds(), @event);',
    'DispatchToActiveSkills("WeaponEquip", @event);': 'Instance.SkillDispatcher.DispatchWeaponEquip(GetActiveSkillIds(), @event);',
    'DispatchToActiveSkills("WeaponPickup", @event);': 'Instance.SkillDispatcher.DispatchWeaponPickup(GetActiveSkillIds(), @event);',
    'DispatchToActiveSkills("WeaponReload", @event);': 'Instance.SkillDispatcher.DispatchWeaponReload(GetActiveSkillIds(), @event);',
    'DispatchToActiveSkills("GrenadeThrown", @event);': 'Instance.SkillDispatcher.DispatchGrenadeThrown(GetActiveSkillIds(), @event);',
    'DispatchToActiveSkills("PlayerHurt", @event);': 'Instance.SkillDispatcher.DispatchPlayerHurt(GetActiveSkillIds(), @event);',
    'DispatchToActiveSkills("PlayerJump", @event);': 'Instance.SkillDispatcher.DispatchPlayerJump(GetActiveSkillIds(), @event);',
    'DispatchToActiveSkills("BotTakeover", @event);': 'Instance.SkillDispatcher.DispatchBotTakeover(GetActiveSkillIds(), @event);',
    'DispatchToActiveSkills("PlayerBlind", @event);': 'Instance.SkillDispatcher.DispatchPlayerBlind(GetActiveSkillIds(), @event);',
    'DispatchToActiveSkills("PlayerDeath", @event);': 'Instance.SkillDispatcher.DispatchPlayerDeath(GetActiveSkillIds(), @event);',
    'DispatchToActiveSkills("BulletImpact", @event);': 'Instance.SkillDispatcher.DispatchBulletImpact(GetActiveSkillIds(), @event);',
    'Instance.SkillAction(skillPlayer.Skill.ToString(), "DisableSkill", [player]);': 'Instance.SkillDispatcher.InvokeDisableSkill(SkillRuntime.GetId(skillPlayer.Skill), player);',
    'Instance.SkillAction(playerInfo.Skill.ToString(), "DisableSkill", [victim]);': 'Instance.SkillDispatcher.InvokeDisableSkill(SkillRuntime.GetId(playerInfo.Skill), victim);',
    'Instance.SkillAction(playerInfo.Skill.ToString(), "UseSkill", [player]);': 'Instance.SkillDispatcher.InvokeUseSkill(SkillRuntime.GetId(playerInfo.Skill), player);',
}

for old, new in replacements.items():
    if old not in text:
        raise SystemExit(f"Missing expected player-routing source: {old}")
    text = text.replace(old, new)

old_hurt = """                    bool suppressed = AskSkillSuppressesHit(victimInfo.Skill, @event);

                    if (!suppressed)
                    {
                        var attacker = PlayerManager.GetPlayerEvent(@event.Attacker);
                        if (attacker != null && attacker.IsValid && attacker.Index != victim.Index)
                        {
                            var attackerInfo = PlayerManager.GetPlayerByIndex(attacker.Index);
                            if (attackerInfo != null && !attackerInfo.IsDrawing && attackerInfo.Skill != victimInfo.Skill)
                                suppressed = AskSkillSuppressesHit(attackerInfo.Skill, @event);
                        }
                    }
"""
new_hurt = """                    SkillId? attackerSkillId = null;
                    var attacker = PlayerManager.GetPlayerEvent(@event.Attacker);
                    if (attacker != null && attacker.IsValid && attacker.Index != victim.Index)
                    {
                        var attackerInfo = PlayerManager.GetPlayerByIndex(attacker.Index);
                        if (attackerInfo != null && !attackerInfo.IsDrawing)
                            attackerSkillId = SkillRuntime.GetId(attackerInfo.Skill);
                    }

                    bool suppressed = Instance.SkillDispatcher.DispatchPlayerHurtPre(
                        SkillRuntime.GetId(victimInfo.Skill), attackerSkillId, @event);
"""
if old_hurt not in text:
    raise SystemExit("Missing legacy PlayerHurtPre routing block")
text = text.replace(old_hurt, new_hurt, 1)

old_helper = """        // Calls <Skill>.PlayerHurtPre(event) and treats a boxed true return as
        // "suppress this hit". A hero that does not declare the hook returns null.
        private static bool AskSkillSuppressesHit(Skills skill, EventPlayerHurt @event)
        {
            if (skill == Skills.None) return false;
            return (bool?)Instance.SkillAction(skill.ToString(), "PlayerHurtPre", [@event]) == true;
        }

"""
if old_helper not in text:
    raise SystemExit("Missing AskSkillSuppressesHit helper")
text = text.replace(old_helper, "", 1)

old_disconnect = """                uint leavingIndex = player.Index;
                foreach (var skill in SkillData.Skills)
                    Instance.SkillAction(skill.Skill.ToString(), "PlayerDisconnect", [leavingIndex]);
"""
new_disconnect = """                uint leavingIndex = player.Index;
                var registeredSkillIds = SkillData.Skills
                    .Select(skill => SkillRuntime.GetId(skill.Skill))
                    .ToArray();
                Instance.SkillDispatcher.DispatchPlayerDisconnect(registeredSkillIds, leavingIndex);
"""
if old_disconnect not in text:
    raise SystemExit("Missing legacy disconnect fan-out")
text = text.replace(old_disconnect, new_disconnect, 1)
path.write_text(text, encoding="utf-8")

changelog = Path("CHANGELOG.md")
log = changelog.read_text(encoding="utf-8")
entry = "- Route player, weapon, grenade, hurt-suppression, death, disconnect and skill-use callbacks through typed dispatcher methods while retaining the specialized tick and damage-order paths for separate migration."
if entry not in log:
    log = log.replace("### Changed\n\n", f"### Changed\n\n{entry}\n\n", 1)
changelog.write_text(log, encoding="utf-8")
