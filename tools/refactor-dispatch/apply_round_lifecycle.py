from pathlib import Path

hero = Path("HeroShift - SRC Files/src/HeroShift.cs")
hero_text = hero.read_text(encoding="utf-8")

old_load = '''            foreach (var skill in Enum.GetValues<Skills>())
                if (SkillRuntime.GetMetadata(skill).Active)
                    SkillAction(skill.ToString()!, "LoadSkill");
'''
new_load = '''            foreach (var skill in Enum.GetValues<Skills>())
                if (SkillRuntime.GetMetadata(skill).Active)
                    InvokeLoadSkill(skill);
'''
if old_load not in hero_text:
    raise SystemExit("Missing legacy LoadAllSkills invocation")
hero_text = hero_text.replace(old_load, new_load, 1)

insertion_point = """        private static bool TryClaimCurseTarget(object[]? param)
"""
methods = """        private void InvokeLifecycle(Skills skill, string hookName, Action<SkillDefinition> invoke)
        {
            if (!SkillRegistry.TryGet(SkillRuntime.GetId(skill), out var definition)) return;

            if (!PerfLog.Enabled)
            {
                invoke(definition);
                return;
            }

            long perfStart = PerfLog.Start();
            invoke(definition);
            PerfLog.End($"SkillAction {skill}.{hookName}", perfStart, 2.0);
        }

        internal void InvokeLoadSkill(Skills skill) =>
            InvokeLifecycle(skill, nameof(SkillHookSet.LoadSkill), d => d.Hooks.LoadSkill?.Invoke());

        internal void InvokeEnableSkill(Skills skill, CCSPlayerController player)
        {
            string skillName = skill.ToString();
            ActiveSkillsThisRound.TryAdd(skillName, 0);
            SkillsUsedThisMap.TryAdd(skillName, 0);
            InvokeLifecycle(skill, nameof(SkillHookSet.EnableSkill), d => d.Hooks.EnableSkill?.Invoke(player));
        }

        internal void InvokeDisableSkill(Skills skill, CCSPlayerController player)
        {
            string skillName = skill.ToString();
            if (SkillUtils.CurseLimitEnabled && SkillUtils.IsCurseSkill(skillName) && player.IsValid)
                SkillUtils.ReleaseCurse(player.Index);

            InvokeLifecycle(skill, nameof(SkillHookSet.DisableSkill), d => d.Hooks.DisableSkill?.Invoke(player));
        }

        internal void InvokeUseSkill(Skills skill, CCSPlayerController player) =>
            InvokeLifecycle(skill, nameof(SkillHookSet.UseSkill), d => d.Hooks.UseSkill?.Invoke(player));

        internal bool InvokeTypeSkill(Skills skill, CCSPlayerController player, string[] arguments)
        {
            string skillName = skill.ToString();
            if (SkillUtils.CurseLimitEnabled && SkillUtils.IsCurseSkill(skillName) &&
                !TryClaimCurseTarget([player, arguments]))
                return false;

            InvokeLifecycle(skill, nameof(SkillHookSet.TypeSkill), d => d.Hooks.TypeSkill?.Invoke(player, arguments));
            return true;
        }

        internal void InvokeNewRoundSkill(Skills skill) =>
            InvokeLifecycle(skill, nameof(SkillHookSet.NewRound), d => d.Hooks.NewRound?.Invoke());

        internal void InvokeRoundEndSkill(Skills skill) =>
            InvokeLifecycle(skill, nameof(SkillHookSet.RoundEnd), d => d.Hooks.RoundEnd?.Invoke());

"""
if insertion_point not in hero_text:
    raise SystemExit("Missing lifecycle insertion point")
hero_text = hero_text.replace(insertion_point, methods + insertion_point, 1)
hero.write_text(hero_text, encoding="utf-8")

rounds = Path("HeroShift - SRC Files/src/player/RoundEvents.cs")
round_text = rounds.read_text(encoding="utf-8")
replacements = {
    '                    Instance.SkillAction(playerInfo.Skill.ToString(), "DisableSkill", [player]);': '                    Instance.InvokeDisableSkill(playerInfo.Skill, player);',
    '                    Instance.SkillAction(skill.Skill.ToString(), "NewRound");': '                    Instance.InvokeNewRoundSkill(skill.Skill);',
    '            DispatchToActiveSkills("RoundEnd");': '            Instance.SkillDispatcher.DispatchRoundEnd(GetActiveSkillIds());',
    '                    Instance?.SkillAction(skillPlayer.Skill.ToString(), "DisableSkill", [player]);': '                    Instance?.InvokeDisableSkill(skillPlayer.Skill, player);',
    '                                Instance?.SkillAction(randomSkill.Skill.ToString(), "EnableSkill", [playerTarget]);': '                                Instance?.InvokeEnableSkill(randomSkill.Skill, playerTarget);',
    '                            Instance?.SkillAction(randomSkill.Skill.ToString(), "EnableSkill", [playerTarget]);': '                            Instance?.InvokeEnableSkill(randomSkill.Skill, playerTarget);',
    '                Instance?.SkillAction(skillPlayer.Skill.ToString(), "DisableSkill", [player]);': '                Instance?.InvokeDisableSkill(skillPlayer.Skill, player);',
    '                            Instance?.SkillAction(randomSkill.Skill.ToString(), "EnableSkill", [player]);': '                            Instance?.InvokeEnableSkill(randomSkill.Skill, player);',
    '                        Instance?.SkillAction(randomSkill.Skill.ToString(), "EnableSkill", [player]);': '                        Instance?.InvokeEnableSkill(randomSkill.Skill, player);',
}
for old, new in replacements.items():
    if old not in round_text:
        raise SystemExit(f"Missing expected RoundEvents lifecycle source: {old}")
    round_text = round_text.replace(old, new)

old_reset = '''                foreach (var skillName in SkillsUsedThisMap.Keys)
                    Instance.SkillAction(skillName, "NewRound");
'''
new_reset = '''                foreach (var skillName in SkillsUsedThisMap.Keys)
                    if (Enum.TryParse<Skills>(skillName, ignoreCase: true, out var skill))
                        Instance.InvokeNewRoundSkill(skill);
'''
if old_reset not in round_text:
    raise SystemExit("Missing used-skill NewRound sweep")
round_text = round_text.replace(old_reset, new_reset, 1)
rounds.write_text(round_text, encoding="utf-8")

# Event routing no longer needs the generic string helpers after RoundEnd moves.
player = Path("HeroShift - SRC Files/src/player/PlayerEvents.cs")
player_text = player.read_text(encoding="utf-8")
start = player_text.find("        // Single entry point for the reflection call")
end = player_text.find("        // Builds the distinct active typed IDs", start)
if start < 0 or end < 0:
    raise SystemExit("Missing legacy InvokeSkill helper block")
player_text = player_text[:start] + player_text[end:]
start = player_text.find("        // Core fan-out: calls methodName once per DISTINCT hero currently in play.")
end = player_text.find("        // Same fan-out as DispatchToActiveSkills, but ordered:", start)
if start < 0 or end < 0:
    raise SystemExit("Missing legacy DispatchToActiveSkills helper block")
player_text = player_text[:start] + player_text[end:]
player.write_text(player_text, encoding="utf-8")

changelog = Path("CHANGELOG.md")
log = changelog.read_text(encoding="utf-8")
entry = "- Move skill load/enable/disable/use/type/reset/round-end lifecycle into explicit typed coordinator methods that preserve PerfLog, active/map history and curse ownership side effects; migrate RoundEvents and remove the generic event string fan-out helpers."
if entry not in log:
    log = log.replace("### Changed\n\n", f"### Changed\n\n{entry}\n\n", 1)
changelog.write_text(log, encoding="utf-8")
