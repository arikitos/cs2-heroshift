using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record JackalOptions : ISkillOptions
{
    public string ParticleName { get; init; } = "particles/ui/hud/ui_map_def_utility_trail.vpcf";
}

public static class JackalDefinition
{
    public static SkillDefinition<JackalOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Jackal,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#f542ef",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: 1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new JackalOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Jackal.LoadSkill,
            EnableSkill = Jackal.EnableSkill,
            DisableSkill = Jackal.DisableSkill,
            CheckTransmit = Jackal.CheckTransmit,
            NewRound = Jackal.NewRound,
        },
    };
}
