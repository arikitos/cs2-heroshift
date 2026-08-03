using src.SkillsCore.Abstractions;

namespace src.SkillsCore;

/*
 * SkillRegistry - the single lookup for every typed SkillDefinition.
 *
 * This is a skeleton (REFACTOR.md commit 2 / section 10): it stores and
 * looks up definitions and validates uniqueness, but is not yet wired into
 * plugin load or the event pipeline. The reflection-based legacy dispatch
 * (HeroShift.SkillAction, SkillsInfo) keeps running unchanged until every
 * skill is migrated and the dispatcher replacement lands.
 *
 * Registration happens once at startup (BuiltInSkillCatalog, added when
 * skills start migrating), after which lookups are pure dictionary reads -
 * no reflection, no assembly scanning, matching the performance requirements
 * in REFACTOR.md section 32.
 */
public sealed class SkillRegistry
{
    private readonly Dictionary<SkillId, SkillDefinition> _byId = [];

    public IReadOnlyCollection<SkillDefinition> All => _byId.Values;

    public void Register(SkillDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (!_byId.TryAdd(definition.Id, definition))
            throw new InvalidOperationException($"Duplicate skill ID '{definition.Id}': every skill must be registered exactly once.");
    }

    public bool TryGet(SkillId id, out SkillDefinition definition) => _byId.TryGetValue(id, out definition!);

    public SkillDefinition Get(SkillId id)
    {
        if (!TryGet(id, out var definition))
            throw new KeyNotFoundException($"No skill is registered with ID '{id}'.");

        return definition;
    }

    public bool Contains(SkillId id) => _byId.ContainsKey(id);

    // Hook-indexed views, built lazily and cached - avoids scanning every
    // skill for hooks only a subset implement (REFACTOR.md section 10).
    // Populated once registration is complete; call Invalidate() if the
    // registry is ever rebuilt (e.g. tests constructing a fresh instance).
    private IReadOnlyList<SkillDefinition>? _tickSkills;
    private IReadOnlyList<SkillDefinition>? _newRoundSkills;
    private IReadOnlyList<SkillDefinition>? _roundEndSkills;

    public IReadOnlyList<SkillDefinition> TickSkills => _tickSkills ??= _byId.Values.Where(d => d.Hooks.OnTick != null).ToList();
    public IReadOnlyList<SkillDefinition> NewRoundSkills => _newRoundSkills ??= _byId.Values.Where(d => d.Hooks.NewRound != null).ToList();
    public IReadOnlyList<SkillDefinition> RoundEndSkills => _roundEndSkills ??= _byId.Values.Where(d => d.Hooks.RoundEnd != null).ToList();

    public void InvalidateHookIndexes()
    {
        _tickSkills = null;
        _newRoundSkills = null;
        _roundEndSkills = null;
    }
}
