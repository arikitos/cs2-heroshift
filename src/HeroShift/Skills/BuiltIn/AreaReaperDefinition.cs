using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record AreaReaperOptions : ISkillOptions
{
}

public static class AreaReaperDefinition
{
    public static SkillDefinition<AreaReaperOptions> Create() => new()
    {
        Id = BuiltInSkillIds.AreaReaper,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#edf5b5",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.CounterTerrorist,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: 1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new AreaReaperOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = AreaReaper.LoadSkill,
            EnableSkill = AreaReaper.EnableSkill,
            DisableSkill = AreaReaper.DisableSkill,
            TypeSkill = AreaReaper.TypeSkill,
            OnTick = AreaReaper.OnTick,
            NewRound = AreaReaper.NewRound,
        },
    };
}
