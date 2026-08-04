using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record DuplicatorOptions : ISkillOptions
{
}

public static class DuplicatorDefinition
{
    public static SkillDefinition<DuplicatorOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Duplicator,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#ffb73b",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new DuplicatorOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Duplicator.LoadSkill,
            EnableSkill = Duplicator.EnableSkill,
            DisableSkill = Duplicator.DisableSkill,
            TypeSkill = Duplicator.TypeSkill,
            OnTick = Duplicator.OnTick,
            NewRound = Duplicator.NewRound,
        },
    };
}
