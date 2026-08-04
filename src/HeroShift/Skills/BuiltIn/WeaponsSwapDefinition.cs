using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record WeaponsSwapOptions : ISkillOptions
{
    public float Cooldown { get; init; } = 30f;
}

public static class WeaponsSwapDefinition
{
    public static SkillDefinition<WeaponsSwapOptions> Create() => new()
    {
        Id = BuiltInSkillIds.WeaponsSwap,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#c7e03a",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new WeaponsSwapOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = WeaponsSwap.LoadSkill,
            EnableSkill = WeaponsSwap.EnableSkill,
            DisableSkill = WeaponsSwap.DisableSkill,
            UseSkill = WeaponsSwap.UseSkill,
            OnTick = WeaponsSwap.OnTick,
            NewRound = WeaponsSwap.NewRound,
        },
    };
}
