using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record HealingChickenOptions : ISkillOptions
{
    public int Amount { get; init; } = 3;
    public int Heal { get; init; } = 2;
    public int TickCooldown { get; init; } = 16;
    public float HealRadius { get; init; } = 150.0f;
}

public static class HealingChickenDefinition
{
    public static SkillDefinition<HealingChickenOptions> Create() => new()
    {
        Id = BuiltInSkillIds.HealingChicken,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#b5ab8f",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: 1,
            Rarity: global::src.utils.Rarity.Legendary),
        DefaultOptions = new HealingChickenOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = HealingChicken.LoadSkill,
            EnableSkill = HealingChicken.EnableSkill,
            DisableSkill = HealingChicken.DisableSkill,
            OnTick = HealingChicken.OnTick,
            NewRound = HealingChicken.NewRound,
        },
    };
}
