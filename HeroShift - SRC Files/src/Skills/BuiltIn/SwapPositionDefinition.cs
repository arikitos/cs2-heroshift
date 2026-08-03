using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record SwapPositionOptions : ISkillOptions
{
    public float Cooldown { get; init; } = 30f;
    public float CooldownBeforeUse { get; init; } = 10f;
}

public static class SwapPositionDefinition
{
    public static SkillDefinition<SwapPositionOptions> Create() => new()
    {
        Id = BuiltInSkillIds.SwapPosition,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#1466F5",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: true,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new SwapPositionOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = SwapPosition.LoadSkill,
            EnableSkill = SwapPosition.EnableSkill,
            DisableSkill = SwapPosition.DisableSkill,
            UseSkill = SwapPosition.UseSkill,
            OnTick = SwapPosition.OnTick,
            NewRound = SwapPosition.NewRound,
        },
    };
}
