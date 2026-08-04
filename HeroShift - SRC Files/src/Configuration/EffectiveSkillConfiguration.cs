using src.player;
using src.SkillsCore.Abstractions;

namespace src.Configuration;

public sealed record EffectiveSkillConfiguration(
    SkillId Id,
    Skills LegacySkill,
    SkillMetadata Metadata,
    ISkillOptions Options)
{
    public bool Active => Metadata.Active;
    public string Color => Metadata.Color;
    public int OnlyTeam => (int)Metadata.OnlyTeam;
    public bool DisableOnFreezeTime => Metadata.DisableOnFreezeTime;
    public bool NeedsTeammates => Metadata.NeedsTeammates;
    public string RequiredPermission => Metadata.RequiredPermission;
    public float? HudDuration => Metadata.HudDuration;
    public float? DescriptionHudDuration => Metadata.DescriptionHudDuration;
    public int MaxPerServer => Metadata.MaxPerServer;
    public string Rarity => Metadata.Rarity.ToString();
}

public sealed class EffectiveSkillConfigurationCollection : IReadOnlyCollection<EffectiveSkillConfiguration>
{
    private readonly IReadOnlyDictionary<SkillId, EffectiveSkillConfiguration> _byId;
    private readonly IReadOnlyDictionary<Skills, EffectiveSkillConfiguration> _byLegacySkill;
    private readonly IReadOnlyList<EffectiveSkillConfiguration> _all;

    public EffectiveSkillConfigurationCollection(IEnumerable<EffectiveSkillConfiguration> skills)
    {
        ArgumentNullException.ThrowIfNull(skills);

        _all = skills.ToArray();
        _byId = _all.ToDictionary(skill => skill.Id);
        _byLegacySkill = _all.ToDictionary(skill => skill.LegacySkill);
    }

    public int Count => _all.Count;
    public IReadOnlyList<EffectiveSkillConfiguration> All => _all;

    public EffectiveSkillConfiguration Get(SkillId id) =>
        _byId.TryGetValue(id, out var skill)
            ? skill
            : throw new KeyNotFoundException($"No effective configuration exists for skill '{id}'.");

    public EffectiveSkillConfiguration Get(Skills skill) =>
        _byLegacySkill.TryGetValue(skill, out var resolved)
            ? resolved
            : throw new KeyNotFoundException($"No effective configuration exists for legacy skill '{skill}'.");

    public bool TryGet(SkillId id, out EffectiveSkillConfiguration skill) => _byId.TryGetValue(id, out skill!);
    public bool TryGet(Skills legacySkill, out EffectiveSkillConfiguration skill) => _byLegacySkill.TryGetValue(legacySkill, out skill!);

    public IEnumerator<EffectiveSkillConfiguration> GetEnumerator() => _all.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
