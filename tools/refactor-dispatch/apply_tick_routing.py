from pathlib import Path

# Add a single-skill tick API that deliberately lets hook exceptions propagate.
dispatcher = Path("HeroShift - SRC Files/src/Skills/SkillDispatcher.cs")
text = dispatcher.read_text(encoding="utf-8")
needle = """    public void InvokeTypeSkill(SkillId skillId, CCSPlayerController player, string[] arguments) =>
        Invoke(skillId, nameof(SkillHookSet.TypeSkill), d => d.Hooks.TypeSkill?.Invoke(player, arguments));

"""
addition = """    public void InvokeTypeSkill(SkillId skillId, CCSPlayerController player, string[] arguments) =>
        Invoke(skillId, nameof(SkillHookSet.TypeSkill), d => d.Hooks.TypeSkill?.Invoke(player, arguments));

    // OnTick has legacy caller-owned failure suppression: PlayerEvents logs a
    // failing skill only once per round, then continues with later skills. This
    // single-skill API therefore must not use Invoke(), which catches exceptions.
    public void InvokeTickUnchecked(SkillId skillId)
    {
        if (!registry.TryGet(skillId, out var definition)) return;
        definition.Hooks.OnTick?.Invoke();
    }

"""
if needle not in text:
    raise SystemExit("Missing dispatcher lifecycle insertion point")
text = text.replace(needle, addition, 1)
dispatcher.write_text(text, encoding="utf-8")

# Pin the exception propagation contract.
tests = Path("HeroShift - SRC Files/tests/HeroShift.Tests/SkillDispatcherTests.cs")
test_text = tests.read_text(encoding="utf-8")
needle = """    [Fact]
    public void DispatchTick_CallsEveryActiveSkillsOnTickHook()
"""
addition = """    [Fact]
    public void InvokeTickUnchecked_PropagatesHookFailureToCaller()
    {
        var registry = new SkillRegistry();
        registry.Register(MakeDefinition(BuiltInSkillIds.Dash, new SkillHookSet
        {
            OnTick = () => throw new InvalidOperationException("tick failed"),
        }));

        var dispatcher = new SkillDispatcher(registry);

        var exception = Assert.Throws<InvalidOperationException>(
            () => dispatcher.InvokeTickUnchecked(BuiltInSkillIds.Dash));
        Assert.Equal("tick failed", exception.Message);
    }

    [Fact]
    public void DispatchTick_CallsEveryActiveSkillsOnTickHook()
"""
if needle not in test_text:
    raise SystemExit("Missing dispatcher tick-test insertion point")
test_text = test_text.replace(needle, addition, 1)
tests.write_text(test_text, encoding="utf-8")

# Preserve the existing sorting, freeze filtering and per-round catch/log loop;
# replace only the reflection/string invocation inside it.
player = Path("HeroShift - SRC Files/src/player/PlayerEvents.cs")
player_text = player.read_text(encoding="utf-8")
old_comment = """        // OnTick runs 64 times a second, so everything it needs is pre-allocated and
        // reused: cached enum->string names (ToString() would allocate per tick per
        // hero) and two scratch collections cleared and refilled in place.
        private static readonly Dictionary<Skills, string> _skillNames =
            Enum.GetValues<Skills>().ToDictionary(s => s, s => s.ToString());
"""
new_comment = """        // OnTick runs 64 times a second, so its two scratch collections are
        // allocated once and cleared/refilled in place every frame.
"""
if old_comment not in player_text:
    raise SystemExit("Missing legacy OnTick allocation comment/cache")
player_text = player_text.replace(old_comment, new_comment, 1)
old_call = '                        Instance.SkillAction(_skillNames[skill], "OnTick");'
new_call = '                        Instance.SkillDispatcher.InvokeTickUnchecked(SkillRuntime.GetId(skill));'
if old_call not in player_text:
    raise SystemExit("Missing legacy OnTick invocation")
player_text = player_text.replace(old_call, new_call, 1)
player.write_text(player_text, encoding="utf-8")

changelog = Path("CHANGELOG.md")
log = changelog.read_text(encoding="utf-8")
entry = "- Route the sorted per-frame skill loop through a non-swallowing typed tick invocation while preserving freeze-time filtering, AreaReaper/ChillOut ordering and one-log-per-skill-per-round failure suppression."
if entry not in log:
    log = log.replace("### Changed\n\n", f"### Changed\n\n{entry}\n\n", 1)
changelog.write_text(log, encoding="utf-8")
