using src.SkillsCore.Abstractions;

namespace src.Configuration;

public sealed record EffectiveSkillConfiguration(
    SkillId Id,
    SkillMetadata Metadata,
    ISkillOptions Options)
{
    public string Name => Id.Value;
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
    private readonly IReadOnlyList<EffectiveSkillConfiguration> _all;

    public EffectiveSkillConfigurationCollection(IEnumerable<EffectiveSkillConfiguration> skills)
    {
        ArgumentNullException.ThrowIfNull(skills);

        _all = skills.ToArray();
        _byId = _all.ToDictionary(skill => skill.Id);
    }

    public int Count => _all.Count;
    public IReadOnlyList<EffectiveSkillConfiguration> All => _all;

    public EffectiveSkillConfiguration Get(SkillId id) =>
        _byId.TryGetValue(id, out var skill)
            ? skill
            : throw new KeyNotFoundException($"No effective configuration exists for skill '{id}'.");

    public bool TryGet(SkillId id, out EffectiveSkillConfiguration skill) => _byId.TryGetValue(id, out skill!);

    public IEnumerator<EffectiveSkillConfiguration> GetEnumerator() => _all.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
