using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record SecondLifeOptions : ISkillOptions
{
    public int StartHealth { get; init; } = 50;
}

public static class SecondLifeDefinition
{
    public static SkillDefinition<SecondLifeOptions> Create() => new()
    {
        Id = BuiltInSkillIds.SecondLife,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#d41c1c",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new SecondLifeOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = SecondLife.LoadSkill,
            EnableSkill = SecondLife.EnableSkill,
            DisableSkill = SecondLife.DisableSkill,
            OnTakeDamage = SecondLife.OnTakeDamage,
            NewRound = SecondLife.NewRound,
        },
    };
}
