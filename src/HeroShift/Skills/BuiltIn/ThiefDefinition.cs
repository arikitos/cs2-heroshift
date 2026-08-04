using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record ThiefOptions : ISkillOptions
{
}

public static class ThiefDefinition
{
    public static SkillDefinition<ThiefOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Thief,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#adaec7",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new ThiefOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Thief.LoadSkill,
            EnableSkill = Thief.EnableSkill,
            DisableSkill = Thief.DisableSkill,
            TypeSkill = Thief.TypeSkill,
            OnTick = Thief.OnTick,
            NewRound = Thief.NewRound,
        },
    };
}
