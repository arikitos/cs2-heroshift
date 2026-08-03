using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * DwarfOptions - typed replacement for the legacy Dwarf.SkillConfig tunables
 * (src/player/skills/Dwarf.cs). Defaults transcribed verbatim from that
 * SkillConfig's constructor parameters.
 */
public sealed record DwarfOptions : ISkillOptions
{
    public float MinScale { get; init; } = .6f;
    public float MaxScale { get; init; } = .95f;
}

/*
 * DwarfDefinition - typed SkillDefinition for Dwarf. Hooks reference the
 * skill's existing public static methods directly as delegates (REFACTOR.md
 * section 23) - Dwarf.cs's hook bodies are unchanged except for the
 * SkillsInfo.GetValue calls, which now read SkillConfigurationResolver's
 * typed DwarfOptions snapshot instead.
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
            Rarity: utils.Rarity.Common),
        DefaultOptions = new DwarfOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Dwarf.LoadSkill,
            NewRound = Dwarf.NewRound,
            EnableSkill = Dwarf.EnableSkill,
            DisableSkill = Dwarf.DisableSkill,
        },
    };
}
