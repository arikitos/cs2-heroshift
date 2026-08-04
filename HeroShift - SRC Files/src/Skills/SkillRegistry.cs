using src.SkillsCore.Abstractions;

namespace src.SkillsCore;

/*
 * SkillRegistry - the single lookup for every typed SkillDefinition.
 *
 * It stores the canonical built-in definitions, validates uniqueness, and
 * provides the hook-indexed lookup used by the live typed dispatcher.
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

        InvalidateHookIndexes();
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
