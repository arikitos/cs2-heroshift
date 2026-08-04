using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * DwarfOptions - immutable typed gameplay options
 * tunables. Defaults are transcribed verbatim from the baseline snapshot.
 */
public sealed record DwarfOptions : ISkillOptions
{
    public float MinScale { get; init; } = .6f;
    public float MaxScale { get; init; } = .95f;
}

/*
 * DwarfDefinition - canonical identity, metadata, typed defaults and hooks
 * for the existing Dwarf gameplay implementation.
 */
public static class DwarfDefinition
{
    public static SkillDefinition<DwarfOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Dwarf,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#ffff00",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new DwarfOptions(),
        OptionsValidator = options => SkillOptionRules.Ordered(options.MinScale, options.MaxScale, "minScale", "maxScale"),
        Hooks = new SkillHookSet
        {
            LoadSkill = Dwarf.LoadSkill,
            EnableSkill = Dwarf.EnableSkill,
            DisableSkill = Dwarf.DisableSkill,
            NewRound = Dwarf.NewRound,
        },
    };
}
