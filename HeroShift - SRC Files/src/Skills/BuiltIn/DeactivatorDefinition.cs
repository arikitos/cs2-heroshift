using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record DeactivatorOptions : ISkillOptions
{
}

public static class DeactivatorDefinition
{
    public static SkillDefinition<DeactivatorOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Deactivator,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#919191",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new DeactivatorOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Deactivator.LoadSkill,
            EnableSkill = Deactivator.EnableSkill,
            DisableSkill = Deactivator.DisableSkill,
            TypeSkill = Deactivator.TypeSkill,
            OnTick = Deactivator.OnTick,
            NewRound = Deactivator.NewRound,
        },
    };
}
