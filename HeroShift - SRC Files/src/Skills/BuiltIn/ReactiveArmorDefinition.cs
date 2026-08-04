using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record ReactiveArmorOptions : ISkillOptions
{
    public float Cooldown { get; init; } = 15;
}

public static class ReactiveArmorDefinition
{
    public static SkillDefinition<ReactiveArmorOptions> Create() => new()
    {
        Id = BuiltInSkillIds.ReactiveArmor,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#3cded3",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new ReactiveArmorOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = ReactiveArmor.LoadSkill,
            EnableSkill = ReactiveArmor.EnableSkill,
            DisableSkill = ReactiveArmor.DisableSkill,
            OnTick = ReactiveArmor.OnTick,
            NewRound = ReactiveArmor.NewRound,
            PlayerHurtPre = ReactiveArmor.PlayerHurtPre,
        },
    };
}
